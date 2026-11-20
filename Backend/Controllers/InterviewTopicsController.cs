using Backend.DTOs.Common;
using Backend.DTOs.Interview;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/interview-topics")]
    public class InterviewTopicsController : BaseController<InterviewTopicResponseDto, CreateInterviewTopicRequest, UpdateInterviewTopicRequest>
    {
        private readonly IInterviewTopicService _topicService;
        private readonly IImportExportService _importExportService;

        public InterviewTopicsController(IInterviewTopicService topicService, IImportExportService importExportService) : base(topicService)
        {
            _topicService = topicService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] InterviewTopicFilterDto filter)
        {
            var response = await _topicService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _topicService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Interview_Topics_{timestamp}.csv");
            }
            else
            {
                var bytes = await _topicService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Interview_Topics_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportInterviewTopicsAsync(request.File);
            return Ok(result);
        }
    }
}
