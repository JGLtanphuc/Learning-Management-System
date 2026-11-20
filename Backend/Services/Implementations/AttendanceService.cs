using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Attendance;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ClosedXML.Excel;

namespace Backend.Services.Implementations
{
    public class AttendanceService : BaseService<Attendance, AttendanceResponseDto, CreateAttendanceRequest, UpdateAttendanceRequest>, IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;

        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            IStudentRepository studentRepository,
            IClassRepository classRepository,
            ICurrentUserService currentUserService) : base(attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
            _studentRepository = studentRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
        }

        protected override AttendanceResponseDto MapToResponseDto(Attendance entity) => entity.ToResponseDto();
        protected override Attendance MapToEntity(CreateAttendanceRequest request) => request.ToEntity();
        protected override void UpdateEntity(Attendance entity, UpdateAttendanceRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<AttendanceResponseDto>> GetPageAsync(AttendanceFilterDto filter)
        {
            var query = _attendanceRepository.QueryWithDetails().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(a => a.IsActive == filter.IsActive.Value);
            }

            if (filter.ClassId.HasValue)
            {
                query = query.Where(a => a.ClassId == filter.ClassId.Value);
            }

            if (filter.StudentId.HasValue)
            {
                query = query.Where(a => a.StudentId == filter.StudentId.Value);
            }

            if (filter.AttendanceDate.HasValue)
            {
                var targetDate = filter.AttendanceDate.Value.Date;
                query = query.Where(a => a.AttendanceDate.Date == targetDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.Slot))
            {
                var slots = filter.Slot.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (slots.Length > 1)
                {
                    query = query.Where(a => slots.Contains(a.Slot));
                }
                else
                {
                    query = query.Where(a => a.Slot == slots[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var statuses = filter.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (statuses.Length > 1)
                {
                    query = query.Where(a => statuses.Contains(a.Status));
                }
                else
                {
                    query = query.Where(a => a.Status == statuses[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a => a.Student.StudentCode.ToLower().Contains(term) ||
                                         a.Student.FullName.ToLower().Contains(term) ||
                                         a.Class.ClassCode.ToLower().Contains(term) ||
                                         (a.Note != null && a.Note.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<AttendanceResponseDto>
            {
                Content = items.Select(a => a.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<AttendanceResponseDto?> GetByIdAsync(long id)
        {
            var item = await _attendanceRepository.QueryWithDetails()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            return item?.ToResponseDto();
        }

        public override async Task<AttendanceResponseDto> CreateAsync(CreateAttendanceRequest request)
        {
            // Verify student exists
            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student == null)
            {
                throw new NotFoundException($"Học viên với ID {request.StudentId} không tồn tại.");
            }

            // Verify class exists
            var cls = await _classRepository.GetByIdAsync(request.ClassId);
            if (cls == null)
            {
                throw new NotFoundException($"Lớp học với ID {request.ClassId} không tồn tại.");
            }

            // Check if attendance already recorded for this student, class, date, slot
            var targetDate = request.AttendanceDate.Date;
            var exists = await _attendanceRepository.Query()
                .AnyAsync(a => a.StudentId == request.StudentId &&
                               a.ClassId == request.ClassId &&
                               a.AttendanceDate.Date == targetDate &&
                               a.Slot == request.Slot);

            if (exists)
            {
                throw new ConflictException($"Học viên \'{student.FullName}\' đã được điểm danh cho ca {request.Slot} ngày {targetDate:dd/MM/yyyy}.");
            }

            var entity = request.ToEntity();
            await _attendanceRepository.AddAsync(entity);
            await _attendanceRepository.SaveChangesAsync();

            var created = await _attendanceRepository.QueryWithDetails().FirstOrDefaultAsync(a => a.Id == entity.Id);
            return created!.ToResponseDto();
        }

        public override async Task<AttendanceResponseDto?> UpdateAsync(long id, UpdateAttendanceRequest request)
        {
            var entity = await _attendanceRepository.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return null;

            entity.UpdateEntity(request);
            await _attendanceRepository.SaveChangesAsync();

            var updated = await _attendanceRepository.QueryWithDetails().FirstOrDefaultAsync(a => a.Id == id);
            return updated!.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _attendanceRepository.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return false;

            _attendanceRepository.Delete(entity);
            await _attendanceRepository.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var query = _attendanceRepository.QueryWithDetails().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value));
            }

            var items = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,StudentCode,StudentName,ClassCode,AttendanceDate,Slot,Status,Note");
            foreach (var a in items)
            {
                var note = (a.Note ?? "").Replace("\"", "\"\"");
                var sName = (a.Student?.FullName ?? "").Replace("\"", "\"\"");
                csv.AppendLine($"{a.Id},\"{a.Student?.StudentCode}\",\"{sName}\",\"{a.Class?.ClassCode}\",{a.AttendanceDate:yyyy-MM-dd},{a.Slot},{a.Status},\"{note}\"");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var query = _attendanceRepository.QueryWithDetails().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value));
            }

            var items = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Điểm danh");
            ws.ShowGridLines = true;

            // Title
            ws.Cell("A1").Value = "BÁO CÁO ĐIỂM DANH HỌC VIÊN FPT ACADEMY (FAS)";
            ws.Range("A1:H1").Merge();
            ws.Row(1).Height = 30;
            var title = ws.Cell("A1").Style;
            title.Font.FontSize = 14;
            title.Font.Bold = true;
            title.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            title.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            // Subtitle
            ws.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số lượt: {items.Count}";
            ws.Range("A2:H2").Merge();
            ws.Row(2).Height = 18;
            var sub = ws.Cell("A2").Style;
            sub.Font.FontSize = 9;
            sub.Font.Italic = true;
            sub.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Header
            string[] headers = { "STT", "Mã HV", "Họ và tên", "Mã lớp", "Ngày điểm danh", "Ca học", "Trạng thái", "Ghi chú" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            for (int i = 0; i < items.Count; i++)
            {
                var a = items[i];
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = a.Student?.StudentCode ?? "";
                ws.Cell(row, 3).Value = a.Student?.FullName ?? "";
                ws.Cell(row, 4).Value = a.Class?.ClassCode ?? "";
                ws.Cell(row, 5).Value = a.AttendanceDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value = a.Slot;
                ws.Cell(row, 7).Value = a.Status;
                ws.Cell(row, 8).Value = a.Note ?? "";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<ClassAttendanceSheetDto> GetClassAttendanceSheetAsync(long classId, DateTime date, string slot)
        {
            var cls = await _classRepository.GetByIdAsync(classId);
            if (cls == null)
            {
                throw new NotFoundException($"Không tìm thấy lớp học với ID {classId}.");
            }

            var targetDate = date.Date;
            var targetSlot = string.IsNullOrWhiteSpace(slot) ? "MORNING" : slot.Trim().ToUpper();

            // Lấy danh sách học viên của lớp
            var students = await _studentRepository.Query()
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            // Lấy các bản ghi điểm danh đã có của ngày/ca này
            var attendances = await _attendanceRepository.Query()
                .Where(a => a.ClassId == classId && a.AttendanceDate.Date == targetDate && a.Slot == targetSlot)
                .ToListAsync();

            var attMap = attendances.ToDictionary(a => a.StudentId);

            var studentItems = students.Select(s =>
            {
                attMap.TryGetValue(s.Id, out var att);
                return new ClassAttendanceItemDto
                {
                    StudentId = s.Id,
                    StudentCode = s.StudentCode,
                    StudentName = s.FullName,
                    Email = s.Email,
                    AttendanceId = att?.Id,
                    Status = att?.Status ?? "PRESENT",
                    Note = att?.Note
                };
            }).ToList();

            return new ClassAttendanceSheetDto
            {
                ClassId = cls.Id,
                ClassCode = cls.ClassCode,
                ClassName = cls.ClassName,
                AttendanceDate = targetDate,
                Slot = targetSlot,
                Students = studentItems
            };
        }

        public async Task<List<AttendanceResponseDto>> SaveClassAttendanceBatchAsync(SaveClassAttendanceRequest request)
        {
            var cls = await _classRepository.GetByIdAsync(request.ClassId);
            if (cls == null)
            {
                throw new NotFoundException($"Lớp học với ID {request.ClassId} không tồn tại.");
            }

            var targetDate = request.AttendanceDate.Date;
            var targetSlot = string.IsNullOrWhiteSpace(request.Slot) ? "MORNING" : request.Slot.Trim().ToUpper();

            // Lấy các bản ghi điểm danh đã có
            var existingAttendances = await _attendanceRepository.Query()
                .Where(a => a.ClassId == request.ClassId && a.AttendanceDate.Date == targetDate && a.Slot == targetSlot)
                .ToListAsync();

            var existingMap = existingAttendances.ToDictionary(a => a.StudentId);
            var resultList = new List<Attendance>();

            foreach (var item in request.Items)
            {
                if (existingMap.TryGetValue(item.StudentId, out var existing))
                {
                    existing.Status = item.Status;
                    existing.Note = item.Note?.Trim();
                    existing.UpdatedAt = DateTime.UtcNow;
                    resultList.Add(existing);
                }
                else
                {
                    var newEntity = new Attendance
                    {
                        StudentId = item.StudentId,
                        ClassId = request.ClassId,
                        AttendanceDate = targetDate,
                        Slot = targetSlot,
                        Status = item.Status,
                        Note = item.Note?.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _attendanceRepository.AddAsync(newEntity);
                    resultList.Add(newEntity);
                }
            }

            await _attendanceRepository.SaveChangesAsync();

            var savedIds = resultList.Select(r => r.Id).ToList();
            var fullItems = await _attendanceRepository.QueryWithDetails()
                .Where(a => savedIds.Contains(a.Id))
                .ToListAsync();

            return fullItems.Select(a => a.ToResponseDto()).ToList();
        }
    }
}

