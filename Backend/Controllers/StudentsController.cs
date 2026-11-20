using Backend.DTOs.Common;
using Backend.DTOs.Student;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : BaseController<StudentResponseDto, CreateStudentRequest, UpdateStudentRequest>
    {
        private readonly IStudentService _studentService;
        private readonly IImportExportService _importExportService;

        public StudentsController(IStudentService studentService, IImportExportService importExportService) : base(studentService)
        {
            _studentService = studentService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] StudentFilterDto filter)
        {
            var response = await _studentService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("available-for-mock-project")]
        public async Task<IActionResult> GetAvailableForMockProject([FromQuery] long? currentProjectId, [FromQuery] long? classId)
        {
            var list = await _studentService.GetAvailableForMockProjectAsync(currentProjectId, classId);
            return Ok(list);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _studentService.ExportPdfAsync();
                return File(bytes, "application/pdf", $"FAS_Students_{timestamp}.pdf");
            }
            else if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _studentService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Students_{timestamp}.csv");
            }
            else
            {
                var bytes = await _studentService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Students_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportStudentsAsync(request.File);
            return Ok(result);
        }
    }
}
