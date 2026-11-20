using Backend.DTOs.Common;
using Backend.DTOs.Rbac;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : BaseController<UserResponseDto, CreateUserRequest, UpdateUserRequest>
    {
        private readonly IUserService _userService;
        private readonly IImportExportService _importExportService;

        public UsersController(IUserService userService, IImportExportService importExportService) : base(userService)
        {
            _userService = userService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] long? roleId = null,
            [FromQuery] long? roleIds = null)
        {
            var effectiveRoleId = roleId ?? roleIds;
            var response = await _userService.GetPageAsync(page, size, search, isActive, effectiveRoleId);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _userService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Users_{timestamp}.csv");
            }
            else
            {
                var bytes = await _userService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Users_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportUsersAsync(request.File);
            return Ok(result);
        }
    }
}
