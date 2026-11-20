using Backend.Common;
using Backend.DTOs.Common;
using Backend.DTOs.Book;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class BookService : BaseService<Book, BookResponseDto, CreateBookRequest, UpdateBookRequest>, IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository) : base(bookRepository)
        {
            _bookRepository = bookRepository;
        }

        protected override BookResponseDto MapToResponseDto(Book entity) => entity.ToResponseDto();
        protected override Book MapToEntity(CreateBookRequest request) => request.ToEntity();
        protected override void UpdateEntity(Book entity, UpdateBookRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<BookResponseDto>> GetPageAsync(BookFilterDto filter)
        {
            var query = _bookRepository.QueryAsNoTracking();

            if (filter.IsActive.HasValue)
            {
                query = query.Where(b => b.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(term) || (b.Description != null && b.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(b => b.Id)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<BookResponseDto>
            {
                Content = items.Select(b => b.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _bookRepository.QueryAsNoTracking().OrderBy(b => b.Id).ToListAsync();
            var headers = new[] { "ID", "Tên tài liệu / Sách", "Mô tả", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(b => new object?[]
            {
                b.Id,
                b.Name,
                b.Description,
                b.IsActive,
                b.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Danh mục sách", "DANH MỤC SÁCH VÀ TÀI LIỆU HỌC TẬP", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _bookRepository.QueryAsNoTracking().OrderBy(b => b.Id).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Description,IsActive,CreatedAt");
            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},\"{item.Name?.Replace("\"", "\"\"")}\",\"{item.Description?.Replace("\"", "\"\"")}\",{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}

