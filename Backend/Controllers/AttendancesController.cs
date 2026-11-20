using Backend.DTOs.Common;
using Backend.DTOs.Attendance;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/attendances")]
    public class AttendancesController : BaseController<AttendanceResponseDto, CreateAttendanceRequest, UpdateAttendanceRequest>
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IImportExportService _importExportService;

        public AttendancesController(IAttendanceService attendanceService, IImportExportService importExportService) : base(attendanceService)
        {
            _attendanceService = attendanceService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] AttendanceFilterDto filter)
        {
            var response = await _attendanceService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _attendanceService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Attendances_{timestamp}.csv");
            }
            else
            {
                var bytes = await _attendanceService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Attendances_{timestamp}.xlsx");
            }
        }

        [HttpGet("class-sheet")]
        public async Task<IActionResult> GetClassSheet([FromQuery] long classId, [FromQuery] DateTime? date, [FromQuery] string? slot)
        {
            var targetDate = date ?? DateTime.Today;
            var targetSlot = string.IsNullOrWhiteSpace(slot) ? "ALL_DAY" : slot.Trim().ToUpper();
            var sheet = await _attendanceService.GetClassAttendanceSheetAsync(classId, targetDate, targetSlot);
            return Ok(sheet);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> SaveBatch([FromBody] SaveClassAttendanceRequest request)
        {
            var result = await _attendanceService.SaveClassAttendanceBatchAsync(request);
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportAttendancesAsync(request.File);
            return Ok(result);
        }
    }
}
