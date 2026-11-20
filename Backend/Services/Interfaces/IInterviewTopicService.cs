using Backend.DTOs.Common;
using Backend.DTOs.Interview;

namespace Backend.Services.Interfaces
{
    public interface IInterviewTopicService : IService<InterviewTopicResponseDto, CreateInterviewTopicRequest, UpdateInterviewTopicRequest>
    {
        Task<PageResponse<InterviewTopicResponseDto>> GetPageAsync(InterviewTopicFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
