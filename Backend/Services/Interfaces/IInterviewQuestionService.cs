using Backend.DTOs.Common;
using Backend.DTOs.Interview;

namespace Backend.Services.Interfaces
{
    public interface IInterviewQuestionService : IService<InterviewQuestionResponseDto, CreateInterviewQuestionRequest, UpdateInterviewQuestionRequest>
    {
        Task<PageResponse<InterviewQuestionResponseDto>> GetPageAsync(InterviewQuestionFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
