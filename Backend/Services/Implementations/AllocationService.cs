using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Allocation;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class AllocationService : BaseService<Allocation, AllocationResponseDto, CreateAllocationRequest, UpdateAllocationRequest>, IAllocationService
    {
        private readonly IAllocationRepository _allocationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICurrentUserService _currentUserService;

        public AllocationService(IAllocationRepository allocationRepository, IStudentRepository studentRepository, ICurrentUserService currentUserService) : base(allocationRepository)
        {
            _allocationRepository = allocationRepository;
            _studentRepository = studentRepository;
            _currentUserService = currentUserService;
        }

        protected override AllocationResponseDto MapToResponseDto(Allocation entity) => entity.ToResponseDto();
        protected override Allocation MapToEntity(CreateAllocationRequest request) => request.ToEntity();
        protected override void UpdateEntity(Allocation entity, UpdateAllocationRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<AllocationResponseDto>> GetPageAsync(AllocationFilterDto filter)
        {
            var query = _allocationRepository.QueryWithStudent().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Student.Class != null && a.Student.Class.TeacherId == _currentUserService.UserId.Value);
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

            if (filter.StudentId.HasValue)
            {
                query = query.Where(a => a.StudentId == filter.StudentId.Value);
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

            if (!string.IsNullOrWhiteSpace(filter.TargetUnit))
            {
                query = query.Where(a => a.TargetUnit.Contains(filter.TargetUnit));
            }

            if (!string.IsNullOrWhiteSpace(filter.RoleTitle))
            {
                query = query.Where(a => a.RoleTitle.Contains(filter.RoleTitle));
            }

            if (filter.InterviewDate.HasValue)
            {
                var targetDate = filter.InterviewDate.Value.Date;
                query = query.Where(a => a.InterviewDate.HasValue && a.InterviewDate.Value.Date == targetDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a => a.Student.StudentCode.ToLower().Contains(term) ||
                                         a.Student.FullName.ToLower().Contains(term) ||
                                         a.TargetUnit.ToLower().Contains(term) ||
                                         (a.ProjectName != null && a.ProjectName.ToLower().Contains(term)) ||
                                         a.RoleTitle.ToLower().Contains(term));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<AllocationResponseDto>
            {
                Content = items.Select(a => a.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<AllocationResponseDto?> GetByIdAsync(long id)
        {
            var item = await _allocationRepository.QueryWithStudent()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            return item?.ToResponseDto();
        }

        public override async Task<AllocationResponseDto> CreateAsync(CreateAllocationRequest request)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student == null)
            {
                throw new NotFoundException($"Học viên với ID {request.StudentId} không tồn tại.");
            }

            var entity = request.ToEntity();
            await _allocationRepository.AddAsync(entity);
            await _allocationRepository.SaveChangesAsync();

            var created = await _allocationRepository.QueryWithStudent().FirstOrDefaultAsync(a => a.Id == entity.Id);
            return created!.ToResponseDto();
        }

        public override async Task<AllocationResponseDto?> UpdateAsync(long id, UpdateAllocationRequest request)
        {
            var entity = await _allocationRepository.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return null;

            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student == null)
            {
                throw new NotFoundException($"Học viên với ID {request.StudentId} không tồn tại.");
            }

            entity.UpdateEntity(request);
            await _allocationRepository.SaveChangesAsync();

            var updated = await _allocationRepository.QueryWithStudent().FirstOrDefaultAsync(a => a.Id == id);
            return updated!.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _allocationRepository.Query().FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return false;

            _allocationRepository.Delete(entity);
            await _allocationRepository.SaveChangesAsync();
            return true;
        }

        private IQueryable<Allocation> GetScopedAllocationQuery()
        {
            var query = _allocationRepository.QueryWithStudent().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Student.Class != null && a.Student.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value));
            }

            return query;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await GetScopedAllocationQuery().OrderBy(a => a.TargetUnit).ToListAsync();
            var headers = new[] { "ID", "Mã học viên", "Họ tên học viên", "Đơn vị tiếp nhận", "Tên dự án", "Vị trí chuyên môn", "Điểm đánh giá (%)", "Trạng thái", "Ngày phỏng vấn" };
            var rows = items.Select(a => new object?[]
            {
                a.Id,
                a.Student?.StudentCode ?? "",
                a.Student?.FullName ?? "",
                a.TargetUnit,
                a.ProjectName,
                a.RoleTitle,
                a.MatchScore,
                a.Status.ToString(),
                a.InterviewDate
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách phân bổ", "DANH SÁCH PHÂN BỔ HỌC VIÊN (ALLOCATIONS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await GetScopedAllocationQuery()
                .OrderBy(a => a.TargetUnit)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,StudentCode,StudentName,TargetUnit,ProjectName,RoleTitle,MatchScore,Status,InterviewDate");
            foreach (var a in items)
            {
                csv.AppendLine($"{a.Id},\"{a.Student?.StudentCode}\",\"{a.Student?.FullName}\",\"{a.TargetUnit}\",\"{a.ProjectName}\",\"{a.RoleTitle}\",{a.MatchScore},{a.Status},{a.InterviewDate:yyyy-MM-dd}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}

