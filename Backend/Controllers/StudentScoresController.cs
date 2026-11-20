using Backend.DTOs.Common;
using Backend.DTOs.Student.Score;
using Backend.DTOs.Class.Score;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/student-scores")]
    public class StudentScoresController : BaseController<StudentScoreResponseDto, CreateStudentScoreRequest, UpdateStudentScoreRequest>
    {
        private readonly IStudentScoreService _scoreService;
        private readonly IImportExportService _importExportService;

        public StudentScoresController(
            IStudentScoreService scoreService,
            IImportExportService importExportService) : base(scoreService)
        {
            _scoreService = scoreService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] StudentScoreFilterDto filter)
        {
            var response = await _scoreService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _scoreService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Student_Scores_{timestamp}.csv");
            }
            else
            {
                var bytes = await _scoreService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Student_Scores_{timestamp}.xlsx");
            }
        }

        [HttpGet("class-sheet")]
        public async Task<IActionResult> GetClassSheet([FromQuery] long classId)
        {
            try
            {
                var sheet = await _scoreService.GetClassScoreSheetAsync(classId);
                return Ok(sheet);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> SaveBatch([FromBody] SaveClassScoreBatchRequest request)
        {
            var result = await _scoreService.SaveClassScoreBatchAsync(request);
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportStudentScoresAsync(request.File);
            return Ok(result);
        }
    }
}
