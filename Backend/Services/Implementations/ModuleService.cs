using Backend.Common;
using Backend.DTOs.Common;
using Backend.DTOs.Menu;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class ModuleService : BaseService<Module, ModuleResponseDto, CreateModuleRequest, UpdateModuleRequest>, IModuleService
    {
        private readonly IModuleRepository _moduleRepository;

        public ModuleService(IModuleRepository moduleRepository) : base(moduleRepository)
        {
            _moduleRepository = moduleRepository;
        }

        protected override ModuleResponseDto MapToResponseDto(Module entity) => entity.ToResponseDto();
        protected override Module MapToEntity(CreateModuleRequest request) => request.ToEntity();
        protected override void UpdateEntity(Module entity, UpdateModuleRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<ModuleResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? moduleGroupId, string? requiredPermission = null)
        {
            var query = _moduleRepository.QueryAsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }

            if (moduleGroupId.HasValue)
            {
                query = query.Where(m => m.ModuleGroupId == moduleGroupId.Value);
            }

            if (!string.IsNullOrWhiteSpace(requiredPermission))
            {
                query = query.Where(m => m.RequiredPermission == requiredPermission);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(m => m.Title.ToLower().Contains(term) || (m.Description != null && m.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Title)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return new PageResponse<ModuleResponseDto>
            {
                Content = items.Select(m => m.ToResponseDto()).ToList(),
                Page = page,
                Size = size,
                TotalElements = totalElements
            };
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _moduleRepository.QueryAsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
            var headers = new[] { "ID", "Nhóm Menu ID", "Tiêu đề Menu", "Đường dẫn URL", "Biểu tượng Icon", "Thứ tự", "Quyền yêu cầu", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(m => new object?[]
            {
                m.Id,
                m.ModuleGroupId,
                m.Title,
                m.Url,
                m.Icon,
                m.DisplayOrder,
                m.RequiredPermission,
                m.IsActive,
                m.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách Menu", "DANH SÁCH CHỨC NĂNG MENU (MODULES)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _moduleRepository.QueryAsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,ModuleGroupId,Title,Url,Icon,DisplayOrder,RequiredPermission,IsActive,CreatedAt");
            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},{item.ModuleGroupId},\"{item.Title?.Replace("\"", "\"\"")}\",\"{item.Url}\",\"{item.Icon}\",{item.DisplayOrder},\"{item.RequiredPermission}\",{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}
