using Backend.DTOs.Common;
using Backend.DTOs.Rbac;

namespace Backend.Services.Interfaces
{
    public interface IUserService : IService<UserResponseDto, CreateUserRequest, UpdateUserRequest>
    {
        Task<PageResponse<UserResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? roleId);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
