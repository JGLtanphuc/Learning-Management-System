using Backend.DTOs.Common;
using Backend.DTOs.MockProject;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/mock-projects")]
    public class MockProjectsController : BaseController<MockProjectResponseDto, CreateMockProjectRequest, UpdateMockProjectRequest>
    {
        private readonly IMockProjectService _mockProjectService;
        private readonly IImportExportService _importExportService;

        public MockProjectsController(IMockProjectService mockProjectService, IImportExportService importExportService) : base(mockProjectService)
        {
            _mockProjectService = mockProjectService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] MockProjectFilterDto filter)
        {
            var response = await _mockProjectService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _mockProjectService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Mock_Projects_{timestamp}.csv");
            }
            else
            {
                var bytes = await _mockProjectService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Mock_Projects_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportMockProjectsAsync(request.File);
            return Ok(result);
        }
    }
}
