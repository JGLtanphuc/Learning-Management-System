using System.Threading.Tasks;
using Backend.DTOs.Common;
using Backend.DTOs.Teacher;

namespace Backend.Services.Interfaces
{
    public interface ITeacherService : IService<TeacherResponseDto, CreateTeacherRequest, UpdateTeacherRequest>
    {
        Task<PageResponse<TeacherResponseDto>> GetPageAsync(TeacherFilterDto filter);
        Task<byte[]> ExportExcelAsync();
        Task<byte[]> ExportCsvAsync();
    }
}
