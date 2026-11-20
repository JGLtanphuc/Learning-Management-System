using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Common.Security
{
    public class AutoPermissionFilter : IAsyncAuthorizationFilter
    {
        private static readonly Dictionary<string, string> ControllerToResourceMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Classes", "CLASS" },
            { "Teachers", "TEACHER" },
            { "Students", "STUDENT" },
            { "Attendances", "ATTENDANCE" },
            { "MockProjects", "MOCK_PROJECT" },
            { "Allocations", "ALLOCATION" },
            { "InterviewTopics", "INTERVIEW_TOPIC" },
            { "InterviewQuestions", "INTERVIEW_QUESTION" },
            { "InterviewAssignments", "INTERVIEW_ASSIGNMENT" },
            { "Books", "BOOK" },
            { "StudentScores", "STUDENT_SCORE" },
            { "TopicMarks", "STUDENT_SCORE" },
            { "Roles", "ROLE" },
            { "Permissions", "PERMISSION" },
            { "Users", "USER" },
            { "Modules", "MENU" },
            { "ModuleGroups", "MENU" },
            { "Dashboard", "MENU" }
        };

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Bỏ qua nếu action hoặc controller cho phép ẩn danh (AllowAnonymous)
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is IAllowAnonymous))
            {
                return Task.CompletedTask;
            }

            var user = context.HttpContext.User;

            // Kiểm tra trạng thái đăng nhập
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Unauthorized: Please log in." });
                return Task.CompletedTask;
            }

            // Quản trị viên (ADMIN) có toàn quyền truy cập (nếu không đang giả lập role khác)
            var roles = user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value);

            bool isRealAdmin = roles.Any(r => string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase));
            string? viewRole = null;
            if (isRealAdmin && context.HttpContext.Request.Headers.TryGetValue("X-View-Role", out var vRoleVal))
            {
                var r = vRoleVal.ToString().Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(r) && !string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    viewRole = r;
                }
            }

            if (isRealAdmin && string.IsNullOrEmpty(viewRole))
            {
                return Task.CompletedTask;
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            var httpMethod = context.HttpContext.Request.Method.ToUpperInvariant();

            // Cho phép tất cả người dùng đã đăng nhập đọc Menu Navigation và Metadata RBAC (dùng để hiển thị giao diện và kiểm tra quyền động)
            if (httpMethod == "GET" && (
                string.Equals(controllerName, "Modules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controllerName, "ModuleGroups", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controllerName, "Roles", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controllerName, "Permissions", StringComparison.OrdinalIgnoreCase)))
            {
                return Task.CompletedTask;
            }

            // 1. Kiểm tra HasPermissionAttribute được gắn tường minh trên Action/Controller
            var hasPermissionAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<HasPermissionAttribute>()
                .FirstOrDefault();

            string? requiredPermission = hasPermissionAttr?.Permission;

            // 2. Nếu không có attribute tường minh, tự động suy luận từ Controller Name và Http Method
            if (string.IsNullOrEmpty(requiredPermission))
            {
                
                

                string resource = ControllerToResourceMap.TryGetValue(controllerName, out var mapped)
                    ? mapped
                    : controllerName.TrimEnd('s').ToUpperInvariant();

                string actionVerb = httpMethod switch
                {
                    "GET" => "READ",
                    "POST" => "CREATE",
                    "PUT" => "UPDATE",
                    "PATCH" => "UPDATE",
                    "DELETE" => "DELETE",
                    _ => "READ"
                };

                requiredPermission = $"{resource}_{actionVerb}";
            }

            // Truy xuất DbContext từ DI để kiểm tra quyền thời gian thực từ CSDL
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<Backend.Data.AppDbContext>();

            // 3. Nếu là Admin đang giả lập role (Preview Mode: TEACHER, STUDENT, ...)
            if (isRealAdmin && !string.IsNullOrEmpty(viewRole))
            {
                var roleEntity = dbContext.Roles
                    .Include(r => r.Permissions)
                    .AsNoTracking()
                    .FirstOrDefault(r => r.Name.ToUpper() == viewRole.ToUpper());

                if (roleEntity != null)
                {
                    bool hasPerm = roleEntity.Permissions.Any(p => string.Equals(p.Name, requiredPermission, StringComparison.OrdinalIgnoreCase));
                    if (!hasPerm)
                    {
                        context.Result = new ObjectResult(new
                        {
                            message = $"Forbidden (Preview Mode): Role '{viewRole}' does not have permission '{requiredPermission}'."
                        })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return Task.CompletedTask;
                    }
                }
                return Task.CompletedTask;
            }

            // 4. Kiểm tra quyền của người dùng thực tế từ Database để đồng bộ tức thì khi Admin sửa quyền
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.FindFirst("id")?.Value 
                ?? user.FindFirst("sub")?.Value;

            if (long.TryParse(userIdClaim, out long userId))
            {
                var userPerms = dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Roles)
                    .SelectMany(r => r.Permissions)
                    .Select(p => p.Name)
                    .ToList();

                if (!userPerms.Any(p => string.Equals(p, requiredPermission, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new ObjectResult(new
                    {
                        message = $"Forbidden: You do not have the required permission '{requiredPermission}'."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return Task.CompletedTask;
                }
            }
            else
            {
                var permissions = user.FindAll("permission").Select(c => c.Value);
                if (!permissions.Any(p => string.Equals(p, requiredPermission, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new ObjectResult(new
                    {
                        message = $"Forbidden: You do not have the required permission '{requiredPermission}'."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}
