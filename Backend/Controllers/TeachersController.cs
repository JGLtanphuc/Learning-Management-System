using System;
using System.Threading.Tasks;
using Backend.DTOs.Common;
using Backend.DTOs.Teacher;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/teachers")]
    public class TeachersController : BaseController<TeacherResponseDto, CreateTeacherRequest, UpdateTeacherRequest>
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService) : base(teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] TeacherFilterDto filter)
        {
            var response = await _teacherService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _teacherService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Teachers_{timestamp}.csv");
            }
            else
            {
                var bytes = await _teacherService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Teachers_{timestamp}.xlsx");
            }
        }
    }
}
