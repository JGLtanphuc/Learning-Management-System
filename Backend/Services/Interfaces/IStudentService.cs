using Backend.DTOs.Common;
using Backend.DTOs.Student;

namespace Backend.Services.Interfaces
{
    public interface IStudentService : IService<StudentResponseDto, CreateStudentRequest, UpdateStudentRequest>
    {
        Task<PageResponse<StudentResponseDto>> GetPageAsync(StudentFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
        Task<byte[]> ExportPdfAsync();
        Task<List<StudentResponseDto>> GetAvailableForMockProjectAsync(long? currentProjectId, long? classId = null);
    }
}
