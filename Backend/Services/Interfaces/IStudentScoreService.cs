using Backend.DTOs.Common;
using Backend.DTOs.Student.Score;
using Backend.DTOs.Class.Score;
using Backend.DTOs.TopicMark;

namespace Backend.Services.Interfaces
{
    public interface IStudentScoreService : IService<StudentScoreResponseDto, CreateStudentScoreRequest, UpdateStudentScoreRequest>
    {
        Task<PageResponse<StudentScoreResponseDto>> GetPageAsync(StudentScoreFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
        Task<TopicMarkGradebookSearchResponseDto> SearchGradebookAsync(int page, int size, string? sort, string? keyword);
        Task<TopicMarkGradebookRowDto?> GetStudentGradebookAsync(long id);
        Task<ClassScoreSheetDto> GetClassScoreSheetAsync(long classId);
        Task<List<StudentScoreResponseDto>> SaveClassScoreBatchAsync(SaveClassScoreBatchRequest request);
    }
}

