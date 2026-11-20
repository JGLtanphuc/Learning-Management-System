using Backend.DTOs.Common;
using Backend.DTOs.Allocation;

namespace Backend.Services.Interfaces
{
    public interface IAllocationService : IService<AllocationResponseDto, CreateAllocationRequest, UpdateAllocationRequest>
    {
        Task<PageResponse<AllocationResponseDto>> GetPageAsync(AllocationFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
    }
}
