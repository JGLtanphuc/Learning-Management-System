using Backend.DTOs.Common;
using Backend.DTOs.Class;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/classes")]
    public class ClassesController : BaseController<ClassResponseDto, CreateClassRequest, UpdateClassRequest>
    {
        private readonly IClassService _classService;
        private readonly IImportExportService _importExportService;

        public ClassesController(IClassService classService, IImportExportService importExportService) : base(classService)
        {
            _classService = classService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] ClassFilterDto filter)
        {
            var response = await _classService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _classService.ExportPdfAsync();
                return File(bytes, "application/pdf", $"FAS_Classes_{timestamp}.pdf");
            }
            else if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _classService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Classes_{timestamp}.csv");
            }
            else
            {
                var bytes = await _classService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Classes_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportClassesAsync(request.File);
            return Ok(result);
        }
    }
}
