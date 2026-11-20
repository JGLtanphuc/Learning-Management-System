using Backend.DTOs.Common;
using Backend.DTOs.Interview;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/interview-questions")]
    public class InterviewQuestionsController : BaseController<InterviewQuestionResponseDto, CreateInterviewQuestionRequest, UpdateInterviewQuestionRequest>
    {
        private readonly IInterviewQuestionService _questionService;
        private readonly IImportExportService _importExportService;

        public InterviewQuestionsController(IInterviewQuestionService questionService, IImportExportService importExportService) : base(questionService)
        {
            _questionService = questionService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] InterviewQuestionFilterDto filter)
        {
            var response = await _questionService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _questionService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Interview_Questions_{timestamp}.csv");
            }
            else
            {
                var bytes = await _questionService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Interview_Questions_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportInterviewQuestionsAsync(request.File);
            return Ok(result);
        }
    }
}
