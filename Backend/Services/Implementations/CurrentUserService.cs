using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Backend.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        private IEnumerable<string> RealRoles =>
            User?.FindAll(ClaimTypes.Role)
                .Concat(User?.FindAll("role") ?? Enumerable.Empty<Claim>())
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? Enumerable.Empty<string>();

        private bool IsRealAdmin => RealRoles.Any(r => string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase));

        private string? ViewRoleHeader
        {
            get
            {
                if (IsRealAdmin && _httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-View-Role", out var val) == true)
                {
                    var role = val.ToString().Trim().ToUpperInvariant();
                    return string.IsNullOrEmpty(role) ? null : role;
                }
                return null;
            }
        }

        public long? UserId
        {
            get
            {
                // Khi Admin xem ở chế độ giả lập Teacher: gán UserId mẫu của Giảng viên 1 (id = 14)
                if (IsRealAdmin && ViewRoleHeader == "TEACHER")
                {
                    return 14;
                }

                // Khi Admin xem ở chế độ giả lập Student: gán UserId mẫu của Học viên 1 (id = 17)
                if (IsRealAdmin && ViewRoleHeader == "STUDENT")
                {
                    return 17;
                }

                var idStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst("sub")?.Value
                    ?? User?.FindFirst("id")?.Value;
                return long.TryParse(idStr, out var id) ? id : null;
            }
        }

        public string? Email
        {
            get
            {
                if (IsRealAdmin && ViewRoleHeader == "TEACHER")
                {
                    return "teacher1@gmail.com";
                }

                if (IsRealAdmin && ViewRoleHeader == "STUDENT")
                {
                    return "student1@gmail.com";
                }

                return User?.FindFirst(ClaimTypes.Email)?.Value
                    ?? User?.FindFirst("email")?.Value;
            }
        }

        public IEnumerable<string> Roles
        {
            get
            {
                if (IsRealAdmin && !string.IsNullOrEmpty(ViewRoleHeader))
                {
                    return new[] { ViewRoleHeader };
                }

                return RealRoles;
            }
        }

        public bool IsAdmin => Roles.Any(r => string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase));
        public bool IsTeacher => Roles.Any(r => string.Equals(r, "TEACHER", StringComparison.OrdinalIgnoreCase));
        public bool IsStudent => Roles.Any(r => string.Equals(r, "STUDENT", StringComparison.OrdinalIgnoreCase));
    }
}
