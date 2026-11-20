using Backend.DTOs.Common;
using Backend.DTOs.Menu;

namespace Backend.Services.Interfaces
{
    public interface IModuleGroupService : IService<ModuleGroupResponseDto, CreateModuleGroupRequest, UpdateModuleGroupRequest>
    {
        Task<PageResponse<ModuleGroupResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
