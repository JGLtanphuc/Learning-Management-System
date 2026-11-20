using Backend.DTOs.Common;
using Backend.DTOs.Menu;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/modules")]
    public class ModulesController : BaseController<ModuleResponseDto, CreateModuleRequest, UpdateModuleRequest>
    {
        private readonly IModuleService _moduleService;
        private readonly IImportExportService _importExportService;

        public ModulesController(IModuleService moduleService, IImportExportService importExportService) : base(moduleService)
        {
            _moduleService = moduleService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage(
            [FromQuery] int page = 0,
            [FromQuery] int size = 9999,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] long? moduleGroupId = null,
            [FromQuery] string? requiredPermission = null)
        {
            var response = await _moduleService.GetPageAsync(page, size, search, isActive, moduleGroupId, requiredPermission);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _moduleService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Modules_{timestamp}.csv");
            }
            else
            {
                var bytes = await _moduleService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Modules_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportModulesAsync(request.File);
            return Ok(result);
        }
    }
}
