using Backend.DTOs.Common;
using Backend.DTOs.Rbac;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : BaseController<RoleResponseDto, CreateRoleRequest, UpdateRoleRequest>
    {
        private readonly IRoleService _roleService;
        private readonly IImportExportService _importExportService;

        public RolesController(IRoleService roleService, IImportExportService importExportService) : base(roleService)
        {
            _roleService = roleService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] long? permissionId = null,
            [FromQuery] long? permissionIds = null)
        {
            var effectivePermissionId = permissionId ?? permissionIds;
            var response = await _roleService.GetPageAsync(page, size, search, isActive, effectivePermissionId);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _roleService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Roles_{timestamp}.csv");
            }
            else
            {
                var bytes = await _roleService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Roles_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportRolesAsync(request.File);
            return Ok(result);
        }
    }
}
