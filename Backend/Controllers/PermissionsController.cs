using Backend.DTOs.Common;
using Backend.DTOs.Rbac;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    public class PermissionsController : BaseController<PermissionResponseDto, CreatePermissionRequest, UpdatePermissionRequest>
    {
        private readonly IPermissionService _permissionService;
        private readonly IImportExportService _importExportService;

        public PermissionsController(IPermissionService permissionService, IImportExportService importExportService) : base(permissionService)
        {
            _permissionService = permissionService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage(
            [FromQuery] int page = 0,
            [FromQuery] int size = 9999,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? resource = null,
            [FromQuery] string? action = null)
        {
            var response = await _permissionService.GetPageAsync(page, size, search, isActive, resource, action);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _permissionService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Permissions_{timestamp}.csv");
            }
            else
            {
                var bytes = await _permissionService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Permissions_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportPermissionsAsync(request.File);
            return Ok(result);
        }
    }
}
