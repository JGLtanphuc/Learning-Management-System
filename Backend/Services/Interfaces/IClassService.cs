using Backend.DTOs.Common;
using Backend.DTOs.Class;

namespace Backend.Services.Interfaces
{
    public interface IClassService : IService<ClassResponseDto, CreateClassRequest, UpdateClassRequest>
    {
        Task<PageResponse<ClassResponseDto>> GetPageAsync(ClassFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
        Task<byte[]> ExportPdfAsync();
    }
}
