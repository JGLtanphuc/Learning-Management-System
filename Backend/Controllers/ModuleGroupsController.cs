using Backend.DTOs.Common;
using Backend.DTOs.Menu;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/module-groups")]
    public class ModuleGroupsController : BaseController<ModuleGroupResponseDto, CreateModuleGroupRequest, UpdateModuleGroupRequest>
    {
        private readonly IModuleGroupService _groupService;
        private readonly IImportExportService _importExportService;

        public ModuleGroupsController(IModuleGroupService groupService, IImportExportService importExportService) : base(groupService)
        {
            _groupService = groupService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage(
            [FromQuery] int page = 0,
            [FromQuery] int size = 9999,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var response = await _groupService.GetPageAsync(page, size, search, isActive);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _groupService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Module_Groups_{timestamp}.csv");
            }
            else
            {
                var bytes = await _groupService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Module_Groups_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportModuleGroupsAsync(request.File);
            return Ok(result);
        }
    }
}
