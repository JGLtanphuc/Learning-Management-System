using Backend.DTOs.Common;
using Backend.DTOs.Book;

namespace Backend.Services.Interfaces
{
    public interface IBookService : IService<BookResponseDto, CreateBookRequest, UpdateBookRequest>
    {
        Task<PageResponse<BookResponseDto>> GetPageAsync(BookFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}

