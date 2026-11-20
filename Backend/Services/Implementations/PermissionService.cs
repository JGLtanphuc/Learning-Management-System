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
    public class PermissionService : BaseService<Permission, PermissionResponseDto, CreatePermissionRequest, UpdatePermissionRequest>, IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
            : base(permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        protected override PermissionResponseDto MapToResponseDto(Permission entity) => entity.ToResponseDto();
        protected override Permission MapToEntity(CreatePermissionRequest request) => request.ToEntity();
        protected override void UpdateEntity(Permission entity, UpdatePermissionRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<PermissionResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, string? resource, string? action)
        {
            var query = _permissionRepository.QueryAsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(resource))
            {
                var resources = resource.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (resources.Length > 1)
                {
                    query = query.Where(p => resources.Contains(p.Resource));
                }
                else
                {
                    query = query.Where(p => p.Resource == resources[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var actions = action.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (actions.Length > 1)
                {
                    query = query.Where(p => actions.Contains(p.Action));
                }
                else
                {
                    query = query.Where(p => p.Action == actions[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Name)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return new PageResponse<PermissionResponseDto>
            {
                Content = items.Select(p => p.ToResponseDto()).ToList(),
                Page = page,
                Size = size,
                TotalElements = totalElements
            };
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _permissionRepository.QueryAsNoTracking().OrderBy(p => p.Resource).ThenBy(p => p.Name).ToListAsync();
            var headers = new[] { "ID", "Tên quyền", "Mô tả", "Tài nguyên (Resource)", "Hành động (Action)", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(p => new object?[]
            {
                p.Id,
                p.Name,
                p.Description,
                p.Resource,
                p.Action,
                p.IsActive,
                p.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách quyền hạn", "DANH SÁCH QUYỀN HẠN HỆ THỐNG (PERMISSIONS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _permissionRepository.QueryAsNoTracking().OrderBy(p => p.Resource).ThenBy(p => p.Name).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Description,Resource,Action,IsActive,CreatedAt");
            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},\"{item.Name}\",\"{item.Description?.Replace("\"", "\"\"")}\",\"{item.Resource}\",\"{item.Action}\",{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}
