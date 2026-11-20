using Backend.Common;
using Backend.DTOs.Common;
using Backend.DTOs.Rbac;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class RoleService : BaseService<Role, RoleResponseDto, CreateRoleRequest, UpdateRoleRequest>, IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public RoleService(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
            : base(roleRepository)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
        }

        protected override RoleResponseDto MapToResponseDto(Role entity) => entity.ToResponseDto();
        protected override Role MapToEntity(CreateRoleRequest request) => request.ToEntity();
        protected override void UpdateEntity(Role entity, UpdateRoleRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<RoleResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? permissionId = null)
        {
            var query = _roleRepository.QueryWithPermissions().AsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            if (permissionId.HasValue)
            {
                query = query.Where(r => r.Permissions.Any(p => p.Id == permissionId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r => r.Name.ToLower().Contains(term) || (r.Description != null && r.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(r => r.Id)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return new PageResponse<RoleResponseDto>
            {
                Content = items.Select(r => r.ToResponseDto()).ToList(),
                Page = page,
                Size = size,
                TotalElements = totalElements
            };
        }

        public override async Task<RoleResponseDto?> GetByIdAsync(long id)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
            return role?.ToResponseDto();
        }

        private static readonly string[] ImpliedInterviewPermissionNames = new[]
        {
            "INTERVIEW_ANSWER_READ",
            "INTERVIEW_ANSWER_CREATE",
            "INTERVIEW_ANSWER_UPDATE",
            "INTERVIEW_ANSWER_DELETE",
            "INTERVIEW_ASSIGNMENT_REVIEW",
            "EXAM_READ",
            "EXAM_CREATE",
            "EXAM_UPDATE",
            "EXAM_DELETE"
        };

        private async Task EnsureImpliedPermissionsAsync(Role role, List<Permission> perms)
        {
            if (perms.Any(p => p.Name == "INTERVIEW_ASSIGNMENT_READ"))
            {
                var impliedPerms = await _permissionRepository.Query()
                    .Where(p => ImpliedInterviewPermissionNames.Contains(p.Name))
                    .ToListAsync();

                foreach (var ip in impliedPerms)
                {
                    if (!role.Permissions.Any(rp => rp.Id == ip.Id))
                    {
                        role.Permissions.Add(ip);
                    }
                }
            }
        }

        public override async Task<RoleResponseDto> CreateAsync(CreateRoleRequest request)
        {
            var role = request.ToEntity();

            if (request.PermissionIds != null && request.PermissionIds.Any())
            {
                var perms = await _permissionRepository.Query().Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
                foreach (var p in perms) role.Permissions.Add(p);
                await EnsureImpliedPermissionsAsync(role, perms);
            }

            await _roleRepository.AddAsync(role);
            await _roleRepository.SaveChangesAsync();

            return role.ToResponseDto();
        }

        public override async Task<RoleResponseDto?> UpdateAsync(long id, UpdateRoleRequest request)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
            if (role == null) return null;

            role.UpdateEntity(request);

            if (request.PermissionIds != null)
            {
                role.Permissions.Clear();
                var perms = await _permissionRepository.Query().Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
                foreach (var p in perms) role.Permissions.Add(p);
                await EnsureImpliedPermissionsAsync(role, perms);
            }

            await _roleRepository.SaveChangesAsync();
            return role.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return false;

            _roleRepository.Delete(role);
            await _roleRepository.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _roleRepository.QueryWithPermissions().AsNoTracking().OrderBy(r => r.Id).ToListAsync();
            var headers = new[] { "ID", "Tên vai trò", "Mô tả", "Số quyền hạn", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(r => new object?[]
            {
                r.Id,
                r.Name,
                r.Description,
                r.Permissions.Count,
                r.IsActive,
                r.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách vai trò", "DANH SÁCH VAI TRÒ HỆ THỐNG (ROLES)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _roleRepository.QueryWithPermissions().AsNoTracking().OrderBy(r => r.Id).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Description,PermissionsCount,IsActive,CreatedAt");
            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},\"{item.Name}\",\"{item.Description?.Replace("\"", "\"\"")}\",{item.Permissions.Count},{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}
