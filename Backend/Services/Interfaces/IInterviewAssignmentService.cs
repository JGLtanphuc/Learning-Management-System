using Backend.DTOs.Common;
using Backend.DTOs.Interview;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces
{
    public interface IInterviewAssignmentService : IService<InterviewAssignmentResponseDto, CreateInterviewAssignmentRequest, UpdateInterviewAssignmentRequest>
    {
        Task<PageResponse<InterviewAssignmentResponseDto>> GetPageAsync(InterviewAssignmentFilterDto filter);

        // Student workflows
        Task<List<InterviewAssignmentResponseDto>> GetMyAssignmentsAsync();
        Task<InterviewSessionResponseDto> StartSessionAsync(long assignmentId);
        Task<InterviewSessionResponseDto?> GetSessionAsync(long sessionId);
        Task<bool> SaveAnswerAsync(long sessionId, SaveSessionAnswerRequest request);
        Task<InterviewSessionResponseDto> SubmitSessionAsync(long sessionId);

        // Teacher & Admin review workflows
        Task<List<InterviewSessionSubmissionSummaryDto>> GetSubmissionsByAssignmentAsync(long assignmentId);
        Task<InterviewSessionResponseDto> ReviewSessionAsync(long sessionId, ReviewSessionRequest request);

        // Export workflows
        Task<byte[]> ExportExcelAsync();
        Task<byte[]> ExportCsvAsync();
    }
}
