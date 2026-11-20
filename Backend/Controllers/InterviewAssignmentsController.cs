using Backend.Common.Security;
using Backend.DTOs.Interview;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/interview-assignments")]
    [Authorize]
    public class InterviewAssignmentsController : BaseController<InterviewAssignmentResponseDto, CreateInterviewAssignmentRequest, UpdateInterviewAssignmentRequest>
    {
        private readonly IInterviewAssignmentService _assignmentService;

        public InterviewAssignmentsController(IInterviewAssignmentService assignmentService) : base(assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> GetPage([FromQuery] InterviewAssignmentFilterDto filter)
        {
            var response = await _assignmentService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", System.StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _assignmentService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Interview_Assignments_{timestamp}.csv");
            }
            else
            {
                var bytes = await _assignmentService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Interview_Assignments_{timestamp}.xlsx");
            }
        }


        [HttpGet("my-assignments")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> GetMyAssignments()
        {
            var response = await _assignmentService.GetMyAssignmentsAsync();
            return Ok(response);
        }

        [HttpPost("{id}/start-session")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> StartSession(long id)
        {
            var response = await _assignmentService.StartSessionAsync(id);
            return Ok(response);
        }

        [HttpGet("sessions/{sessionId}")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> GetSession(long sessionId)
        {
            var response = await _assignmentService.GetSessionAsync(sessionId);
            if (response == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiên làm bài #{sessionId}." });
            }
            return Ok(response);
        }

        [HttpPost("sessions/{sessionId}/save-answer")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> SaveAnswer(long sessionId, [FromBody] SaveSessionAnswerRequest request)
        {
            var success = await _assignmentService.SaveAnswerAsync(sessionId, request);
            return Ok(new { success });
        }

        [HttpPost("sessions/{sessionId}/submit")]
        [HasPermission("INTERVIEW_ASSIGNMENT_READ")]
        public async Task<IActionResult> SubmitSession(long sessionId)
        {
            var response = await _assignmentService.SubmitSessionAsync(sessionId);
            return Ok(response);
        }

   

        [HttpGet("{id}/submissions")]
        [HasPermission("INTERVIEW_ASSIGNMENT_REVIEW")]
        public async Task<IActionResult> GetSubmissions(long id)
        {
            var response = await _assignmentService.GetSubmissionsByAssignmentAsync(id);
            return Ok(response);
        }

        [HttpPost("sessions/{sessionId}/review")]
        [HasPermission("INTERVIEW_ASSIGNMENT_REVIEW")]
        public async Task<IActionResult> ReviewSession(long sessionId, [FromBody] ReviewSessionRequest request)
        {
            var response = await _assignmentService.ReviewSessionAsync(sessionId, request);
            return Ok(response);
        }
    }
}
