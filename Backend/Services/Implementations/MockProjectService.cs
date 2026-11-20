using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.MockProject;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class MockProjectService : BaseService<MockProject, MockProjectResponseDto, CreateMockProjectRequest, UpdateMockProjectRequest>, IMockProjectService
    {
        private readonly IMockProjectRepository _mockProjectRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentScoreRepository _scoreRepository;
        private readonly ICurrentUserService _currentUserService;

        public MockProjectService(
            IMockProjectRepository mockProjectRepository,
            IStudentRepository studentRepository,
            IStudentScoreRepository scoreRepository,
            ICurrentUserService currentUserService) : base(mockProjectRepository)
        {
            _mockProjectRepository = mockProjectRepository;
            _studentRepository = studentRepository;
            _scoreRepository = scoreRepository;
            _currentUserService = currentUserService;
        }

        protected override MockProjectResponseDto MapToResponseDto(MockProject entity) => entity.ToResponseDto();
        protected override MockProject MapToEntity(CreateMockProjectRequest request) => request.ToEntity();
        protected override void UpdateEntity(MockProject entity, UpdateMockProjectRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<MockProjectResponseDto>> GetPageAsync(MockProjectFilterDto filter)
        {
            var query = _mockProjectRepository.QueryWithStudents().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(m => m.Students.Any(s => s.Class != null && s.Class.TeacherId == _currentUserService.UserId.Value));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(m => m.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(m => m.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var statuses = filter.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (statuses.Length > 1)
                {
                    query = query.Where(m => statuses.Contains(m.Status));
                }
                else
                {
                    query = query.Where(m => m.Status == statuses[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.MentorName))
            {
                query = query.Where(m => m.MentorName != null && m.MentorName.Contains(filter.MentorName));
            }

            if (!string.IsNullOrWhiteSpace(filter.TechStack))
            {
                query = query.Where(m => m.TechStack != null && m.TechStack.Contains(filter.TechStack));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(m => m.ProjectName.ToLower().Contains(term) ||
                                         (m.MentorName != null && m.MentorName.ToLower().Contains(term)) ||
                                         (m.TechStack != null && m.TechStack.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<MockProjectResponseDto>
            {
                Content = items.Select(m => m.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<MockProjectResponseDto?> GetByIdAsync(long id)
        {
            var item = await _mockProjectRepository.QueryWithStudents()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            return item?.ToResponseDto();
        }

        public override async Task<MockProjectResponseDto> CreateAsync(CreateMockProjectRequest request)
        {
            if (request.StudentIds != null && request.StudentIds.Count > 3)
            {
                throw new BadRequestException("Mỗi đề tài Mock Project chỉ được phân công tối đa 3 sinh viên.");
            }

            if (request.StudentIds != null && request.StudentIds.Any())
            {
                var studentsToAssign = await _studentRepository.Query()
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();

                var alreadyAssigned = studentsToAssign.Where(s => s.MockProjectId != null).ToList();
                if (alreadyAssigned.Any())
                {
                    var names = string.Join(", ", alreadyAssigned.Select(s => $"{s.FullName} ({s.StudentCode})"));
                    throw new ConflictException($"Sinh viên sau đã thuộc đề tài khác: {names}");
                }
            }

            var entity = request.ToEntity();
            await _mockProjectRepository.AddAsync(entity);
            await _mockProjectRepository.SaveChangesAsync();

            if (request.StudentIds != null && request.StudentIds.Any())
            {
                var studentsToAssign = await _studentRepository.Query()
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var st in studentsToAssign)
                {
                    st.MockProjectId = entity.Id;
                }
                await _studentRepository.SaveChangesAsync();
            }

            if (entity.FinalScore.HasValue)
            {
                await SyncMockProjectScoreToStudentsAsync(entity.Id, entity.FinalScore.Value);
            }

            var created = await _mockProjectRepository.QueryWithStudents().FirstOrDefaultAsync(m => m.Id == entity.Id);
            return created!.ToResponseDto();
        }

        public override async Task<MockProjectResponseDto?> UpdateAsync(long id, UpdateMockProjectRequest request)
        {
            if (request.StudentIds != null && request.StudentIds.Count > 3)
            {
                throw new BadRequestException("Mỗi đề tài Mock Project chỉ được phân công tối đa 3 sinh viên.");
            }

            var entity = await _mockProjectRepository.Query().FirstOrDefaultAsync(m => m.Id == id);
            if (entity == null) return null;

            if (request.StudentIds != null)
            {
                var targetStudents = await _studentRepository.Query()
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();

                var conflictStudents = targetStudents.Where(s => s.MockProjectId != null && s.MockProjectId != id).ToList();
                if (conflictStudents.Any())
                {
                    var names = string.Join(", ", conflictStudents.Select(s => $"{s.FullName} ({s.StudentCode})"));
                    throw new ConflictException($"Sinh viên sau đã thuộc đề tài khác: {names}");
                }

                var currentAssigned = await _studentRepository.Query()
                    .Where(s => s.MockProjectId == id)
                    .ToListAsync();

                foreach (var st in currentAssigned)
                {
                    if (!request.StudentIds.Contains(st.Id))
                    {
                        st.MockProjectId = null;
                    }
                }

                foreach (var st in targetStudents)
                {
                    st.MockProjectId = id;
                }
                await _studentRepository.SaveChangesAsync();
            }

            entity.UpdateEntity(request);
            await _mockProjectRepository.SaveChangesAsync();

            if (entity.FinalScore.HasValue)
            {
                await SyncMockProjectScoreToStudentsAsync(entity.Id, entity.FinalScore.Value);
            }

            var updated = await _mockProjectRepository.QueryWithStudents().FirstOrDefaultAsync(m => m.Id == id);
            return updated!.ToResponseDto();
        }

        private async Task SyncMockProjectScoreToStudentsAsync(long projectId, decimal projectScore)
        {
            var students = await _studentRepository.Query()
                .Where(s => s.MockProjectId == projectId && s.IsActive)
                .ToListAsync();

            if (!students.Any()) return;

            var studentCodes = students.Select(s => s.StudentCode).ToList();
            var existingScores = await _scoreRepository.QueryWithColumns()
                .Where(sc => studentCodes.Contains(sc.StudentCode))
                .ToListAsync();

            var existingMap = existingScores.ToDictionary(sc => sc.StudentCode);

            foreach (var student in students)
            {
                if (existingMap.TryGetValue(student.StudentCode, out var sc))
                {
                    var projCol = sc.StudentScoreColumns.FirstOrDefault(c => c.ScoreKey == "project");
                    if (projCol != null)
                    {
                        projCol.ScoreValue = projectScore;
                    }
                    else
                    {
                        sc.StudentScoreColumns.Add(new StudentScoreColumn
                        {
                            StudentScoreId = sc.Id,
                            ScoreKey = "project",
                            ScoreValue = projectScore
                        });
                    }

                    var coreKeys = new[] { "attendance", "quiz1", "quiz2", "assignment", "practice", "midterm", "final", "project" };
                    var valid = sc.StudentScoreColumns
                        .Where(c => coreKeys.Contains(c.ScoreKey) && c.ScoreValue.HasValue)
                        .Select(c => c.ScoreValue!.Value)
                        .ToList();

                    if (valid.Count > 0)
                    {
                        var bonus = sc.StudentScoreColumns.FirstOrDefault(c => c.ScoreKey == "bonus")?.ScoreValue ?? 0m;
                        sc.OverallScore = Math.Min(10m, Math.Round(valid.Average() + bonus, 2));
                    }
                    sc.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _scoreRepository.SaveChangesAsync();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _mockProjectRepository.Query().FirstOrDefaultAsync(m => m.Id == id);
            if (entity == null) return false;

            var assignedStudents = await _studentRepository.Query()
                .Where(s => s.MockProjectId == id)
                .ToListAsync();
            foreach (var st in assignedStudents)
            {
                st.MockProjectId = null;
            }
            await _studentRepository.SaveChangesAsync();

            _mockProjectRepository.Delete(entity);
            await _mockProjectRepository.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var query = _mockProjectRepository.QueryWithStudents().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(m => m.Students.Any(s => s.Class != null && s.Class.TeacherId == _currentUserService.UserId.Value));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(m => m.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)));
            }

            var items = await query.OrderBy(m => m.ProjectName).ToListAsync();
            var headers = new[] { "ID", "Tên đề tài đồ án", "Giảng viên / Mentor", "Công nghệ sử dụng", "Kho mã nguồn (Repo)", "Trạng thái", "Điểm tổng kết", "Thành viên nhóm", "Kích hoạt" };
            var rows = items.Select(m => new object?[]
            {
                m.Id,
                m.ProjectName,
                m.MentorName,
                m.TechStack,
                m.RepoUrl,
                m.Status.ToString(),
                m.FinalScore,
                string.Join("; ", m.Students.Select(s => $"{s.FullName} ({s.StudentCode})")),
                m.IsActive
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách đồ án", "DANH SÁCH ĐỒ ÁN MOCK PROJECT", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var query = _mockProjectRepository.QueryWithStudents().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(m => m.Students.Any(s => s.Class != null && s.Class.TeacherId == _currentUserService.UserId.Value));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(m => m.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)));
            }

            var items = await query
                .OrderBy(m => m.ProjectName)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,ProjectName,MentorName,TechStack,RepoUrl,Status,FinalScore,Students,IsActive");
            foreach (var m in items)
            {
                var memberNames = string.Join("; ", m.Students.Select(s => $"{s.FullName} ({s.StudentCode})"));
                csv.AppendLine($"{m.Id},\"{m.ProjectName}\",\"{m.MentorName}\",\"{m.TechStack}\",\"{m.RepoUrl}\",{m.Status},{m.FinalScore},\"{memberNames}\",{m.IsActive}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }

    }
}

