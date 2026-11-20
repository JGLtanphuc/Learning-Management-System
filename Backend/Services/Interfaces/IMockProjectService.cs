using Backend.DTOs.Common;
using Backend.DTOs.MockProject;

namespace Backend.Services.Interfaces
{
    public interface IMockProjectService : IService<MockProjectResponseDto, CreateMockProjectRequest, UpdateMockProjectRequest>
    {
        Task<PageResponse<MockProjectResponseDto>> GetPageAsync(MockProjectFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
