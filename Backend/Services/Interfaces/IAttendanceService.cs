using Backend.DTOs.Common;
using Backend.DTOs.Attendance;

namespace Backend.Services.Interfaces
{
    public interface IAttendanceService : IService<AttendanceResponseDto, CreateAttendanceRequest, UpdateAttendanceRequest>
    {
        Task<PageResponse<AttendanceResponseDto>> GetPageAsync(AttendanceFilterDto filter);
        Task<byte[]> ExportCsvAsync();
        Task<byte[]> ExportExcelAsync();
        Task<ClassAttendanceSheetDto> GetClassAttendanceSheetAsync(long classId, DateTime date, string slot);
        Task<List<AttendanceResponseDto>> SaveClassAttendanceBatchAsync(SaveClassAttendanceRequest request);
    }
}
