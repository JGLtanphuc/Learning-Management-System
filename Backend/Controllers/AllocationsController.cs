using Backend.DTOs.Common;
using Backend.DTOs.Allocation;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/allocations")]
    public class AllocationsController : BaseController<AllocationResponseDto, CreateAllocationRequest, UpdateAllocationRequest>
    {
        private readonly IAllocationService _allocationService;
        private readonly IImportExportService _importExportService;

        public AllocationsController(IAllocationService allocationService, IImportExportService importExportService) : base(allocationService)
        {
            _allocationService = allocationService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] AllocationFilterDto filter)
        {
            var response = await _allocationService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _allocationService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Allocations_{timestamp}.csv");
            }
            else
            {
                var bytes = await _allocationService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Allocations_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportAllocationsAsync(request.File);
            return Ok(result);
        }
    }
}
