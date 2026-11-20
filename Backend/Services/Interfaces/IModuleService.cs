using Backend.DTOs.Common;
using Backend.DTOs.Menu;

namespace Backend.Services.Interfaces
{
    public interface IModuleService : IService<ModuleResponseDto, CreateModuleRequest, UpdateModuleRequest>
    {
        Task<PageResponse<ModuleResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? moduleGroupId, string? requiredPermission = null);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
