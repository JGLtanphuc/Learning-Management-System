using Backend.DTOs.Common;
using Backend.DTOs.Rbac;

namespace Backend.Services.Interfaces
{
    public interface IRoleService : IService<RoleResponseDto, CreateRoleRequest, UpdateRoleRequest>
    {
        Task<PageResponse<RoleResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? permissionId = null);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
