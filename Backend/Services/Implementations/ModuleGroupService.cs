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
    public class ModuleGroupService : BaseService<ModuleGroup, ModuleGroupResponseDto, CreateModuleGroupRequest, UpdateModuleGroupRequest>, IModuleGroupService
    {
        private readonly IModuleGroupRepository _groupRepository;

        public ModuleGroupService(IModuleGroupRepository groupRepository) : base(groupRepository)
        {
            _groupRepository = groupRepository;
        }

        protected override ModuleGroupResponseDto MapToResponseDto(ModuleGroup entity) => entity.ToResponseDto();
        protected override ModuleGroup MapToEntity(CreateModuleGroupRequest request) => request.ToEntity();
        protected override void UpdateEntity(ModuleGroup entity, UpdateModuleGroupRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<ModuleGroupResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive)
        {
            var query = _groupRepository.QueryAsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(g => g.Name.ToLower().Contains(term) || (g.Description != null && g.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Name)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return new PageResponse<ModuleGroupResponseDto>
            {
                Content = items.Select(g => g.ToResponseDto()).ToList(),
                Page = page,
                Size = size,
                TotalElements = totalElements
            };
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _groupRepository.QueryAsNoTracking().OrderBy(g => g.DisplayOrder).ToListAsync();
            var headers = new[] { "ID", "Tên nhóm Menu", "Mô tả", "Thứ tự hiển thị", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(g => new object?[]
            {
                g.Id,
                g.Name,
                g.Description,
                g.DisplayOrder,
                g.IsActive,
                g.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Nhóm Menu", "DANH SÁCH NHÓM MENU (MODULE GROUPS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _groupRepository.QueryAsNoTracking().OrderBy(g => g.DisplayOrder).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Description,DisplayOrder,IsActive,CreatedAt");
            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},\"{item.Name?.Replace("\"", "\"\"")}\",\"{item.Description?.Replace("\"", "\"\"")}\",{item.DisplayOrder},{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}
