using Backend.DTOs.Common;
using Backend.DTOs.Rbac;

namespace Backend.Services.Interfaces
{
    public interface IPermissionService : IService<PermissionResponseDto, CreatePermissionRequest, UpdatePermissionRequest>
    {
        Task<PageResponse<PermissionResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, string? resource, string? action);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
