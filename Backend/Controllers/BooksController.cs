using Backend.DTOs.Common;
using Backend.DTOs.Book;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController : BaseController<BookResponseDto, CreateBookRequest, UpdateBookRequest>
    {
        private readonly IBookService _bookService;
        private readonly IImportExportService _importExportService;

        public BooksController(IBookService bookService, IImportExportService importExportService) : base(bookService)
        {
            _bookService = bookService;
            _importExportService = importExportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] BookFilterDto filter)
        {
            var response = await _bookService.GetPageAsync(filter);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "EXCEL")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _bookService.ExportCsvAsync();
                return File(bytes, "text/csv; charset=utf-8", $"FAS_Books_{timestamp}.csv");
            }
            else
            {
                var bytes = await _bookService.ExportExcelAsync();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FAS_Books_{timestamp}.xlsx");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] FileUploadRequest request)
        {
            var result = await _importExportService.ImportBooksAsync(request.File);
            return Ok(result);
        }
    }
}
