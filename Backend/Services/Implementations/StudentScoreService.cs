using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Student.Score;
using Backend.DTOs.Class.Score;
using Backend.DTOs.TopicMark;
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
    public class StudentScoreService : BaseService<StudentScore, StudentScoreResponseDto, CreateStudentScoreRequest, UpdateStudentScoreRequest>, IStudentScoreService
    {
        private readonly IStudentScoreRepository _scoreRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IClassRepository _classRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMockProjectRepository _mockProjectRepository;
        private readonly ICurrentUserService _currentUserService;

        public StudentScoreService(
            IStudentScoreRepository scoreRepository,
            IStudentRepository studentRepository,
            IClassRepository classRepository,
            IAttendanceRepository attendanceRepository,
            IMockProjectRepository mockProjectRepository,
            ICurrentUserService currentUserService)
            : base(scoreRepository)
        {
            _scoreRepository = scoreRepository;
            _studentRepository = studentRepository;
            _classRepository = classRepository;
            _attendanceRepository = attendanceRepository;
            _mockProjectRepository = mockProjectRepository;
            _currentUserService = currentUserService;
        }

        protected override StudentScoreResponseDto MapToResponseDto(StudentScore entity) => entity.ToResponseDto();
        protected override StudentScore MapToEntity(CreateStudentScoreRequest request) => request.ToEntity();
        protected override void UpdateEntity(StudentScore entity, UpdateStudentScoreRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<StudentScoreResponseDto>> GetPageAsync(StudentScoreFilterDto filter)
        {
            var query = _scoreRepository.QueryWithColumns().AsNoTracking();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var teacherClasses = await _classRepository.Query()
                    .Where(c => c.TeacherId == _currentUserService.UserId.Value)
                    .Select(c => c.ClassCode)
                    .Concat(_classRepository.Query().Where(c => c.TeacherId == _currentUserService.UserId.Value).Select(c => c.ClassName))
                    .ToListAsync();
                query = query.Where(s => teacherClasses.Contains(s.ClassName));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                var studentCode = await _studentRepository.Query()
                    .Where(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value))
                    .Select(s => s.StudentCode)
                    .FirstOrDefaultAsync();
                query = query.Where(s => s.StudentCode == (studentCode ?? "___NONE___"));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ClassName))
            {
                query = query.Where(s => s.ClassName == filter.ClassName);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(s =>
                    s.StudentName.ToLower().Contains(term) ||
                    s.StudentCode.ToLower().Contains(term) ||
                    s.ClassName.ToLower().Contains(term));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(s => s.Id)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<StudentScoreResponseDto>
            {
                Content = items.Select(s => s.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<StudentScoreResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _scoreRepository.GetWithColumnsByIdAsync(id);
            return entity?.ToResponseDto();
        }

        public override async Task<StudentScoreResponseDto> CreateAsync(CreateStudentScoreRequest request)
        {
            var entity = request.ToEntity();
            await _scoreRepository.AddAsync(entity);
            await _scoreRepository.SaveChangesAsync();
            return entity.ToResponseDto();
        }

        public override async Task<StudentScoreResponseDto?> UpdateAsync(long id, UpdateStudentScoreRequest request)
        {
            var entity = await _scoreRepository.GetWithColumnsByIdAsync(id);
            if (entity == null) return null;

            entity.UpdateEntity(request);
            await _scoreRepository.SaveChangesAsync();
            return entity.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _scoreRepository.GetByIdAsync(id);
            if (entity == null) return false;

            _scoreRepository.Delete(entity);
            await _scoreRepository.SaveChangesAsync();
            return true;
        }

        private async Task<IQueryable<StudentScore>> GetScopedScoreQueryAsync()
        {
            var query = _scoreRepository.QueryWithColumns().AsNoTracking();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var teacherClasses = await _classRepository.Query()
                    .Where(c => c.TeacherId == _currentUserService.UserId.Value)
                    .Select(c => c.ClassCode)
                    .Concat(_classRepository.Query().Where(c => c.TeacherId == _currentUserService.UserId.Value).Select(c => c.ClassName))
                    .ToListAsync();
                query = query.Where(s => teacherClasses.Contains(s.ClassName));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                var studentCode = await _studentRepository.Query()
                    .Where(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value))
                    .Select(s => s.StudentCode)
                    .FirstOrDefaultAsync();
                query = query.Where(s => s.StudentCode == (studentCode ?? "___NONE___"));
            }

            return query;
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var query = await GetScopedScoreQueryAsync();
            var items = await query.OrderBy(s => s.Id).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,StudentCode,StudentName,ClassName,OverallScore,Scores,IsActive,CreatedAt");
            foreach (var item in items)
            {
                var sName = item.StudentName.Replace("\"", "\"\"");
                var cName = (item.ClassName ?? "").Replace("\"", "\"\"");
                var scoresStr = string.Join(";", item.StudentScoreColumns.Select(c => $"{c.ScoreKey}:{c.ScoreValue}")).Replace("\"", "\"\"");
                csv.AppendLine($"{item.Id},\"{item.StudentCode}\",\"{sName}\",\"{cName}\",{item.OverallScore},\"{scoresStr}\",{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var query = await GetScopedScoreQueryAsync();
            var items = await query.OrderBy(s => s.Id).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Bảng điểm");
            ws.ShowGridLines = true;

            // Title
            ws.Cell("A1").Value = "BẢNG ĐIỂM TỔNG HỢP HỌC VIÊN FPT ACADEMY (FAS)";
            ws.Range("A1:G1").Merge();
            ws.Row(1).Height = 30;
            var title = ws.Cell("A1").Style;
            title.Font.FontSize = 14;
            title.Font.Bold = true;
            title.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            title.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            // Subtitle
            ws.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số học viên: {items.Count}";
            ws.Range("A2:G2").Merge();
            ws.Row(2).Height = 18;
            var sub = ws.Cell("A2").Style;
            sub.Font.FontSize = 9;
            sub.Font.Italic = true;
            sub.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Header
            string[] headers = { "STT", "Mã HV", "Họ và tên", "Lớp học", "Điểm tổng kết", "Chi tiết điểm thành phần", "Trạng thái" };
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
                var item = items[i];
                var scoresStr = string.Join(", ", item.StudentScoreColumns.Select(c => $"{c.ScoreKey}: {c.ScoreValue}"));
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = item.StudentCode;
                ws.Cell(row, 3).Value = item.StudentName;
                ws.Cell(row, 4).Value = item.ClassName ?? "";
                ws.Cell(row, 5).Value = item.OverallScore;
                ws.Cell(row, 6).Value = scoresStr;
                ws.Cell(row, 7).Value = item.IsActive ? "Đang học" : "Đã thôi học";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<TopicMarkGradebookSearchResponseDto> SearchGradebookAsync(int page, int size, string? sort, string? keyword)
        {
            var query = _scoreRepository.QueryWithColumns().AsNoTracking();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var teacherClasses = await _classRepository.Query()
                    .Where(c => c.TeacherId == _currentUserService.UserId.Value)
                    .Select(c => c.ClassCode)
                    .Concat(_classRepository.Query().Where(c => c.TeacherId == _currentUserService.UserId.Value).Select(c => c.ClassName))
                    .ToListAsync();
                query = query.Where(s => teacherClasses.Contains(s.ClassName));
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                var studentCode = await _studentRepository.Query()
                    .Where(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value))
                    .Select(s => s.StudentCode)
                    .FirstOrDefaultAsync();
                query = query.Where(s => s.StudentCode == (studentCode ?? "___NONE___"));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim().ToLower();
                query = query.Where(s =>
                    s.StudentName.ToLower().Contains(term) ||
                    s.StudentCode.ToLower().Contains(term) ||
                    s.ClassName.ToLower().Contains(term));
            }

            var totalElements = await query.LongCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalElements / (size > 0 ? size : 10));

            var students = await query
                .OrderBy(s => s.Id)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            var allKeys = await _scoreRepository.GetAllScoreKeysAsync();

            var columns = new List<TopicMarkGradebookColumnDto>();
            foreach (var key in allKeys)
            {
                var label = char.ToUpper(key[0]) + key.Substring(1);
                columns.Add(new TopicMarkGradebookColumnDto
                {
                    Key = key,
                    Label = label,
                    IsGraded = true
                });
            }

            columns.Add(new TopicMarkGradebookColumnDto
            {
                Key = "FINAL_SCORE",
                Label = "Final Score",
                IsGraded = true
            });

            columns.Add(new TopicMarkGradebookColumnDto
            {
                Key = "IS_PASSED",
                Label = "Passed?",
                IsGraded = false
            });

            var rowsContent = new List<TopicMarkGradebookRowDto>();
            foreach (var s in students)
            {
                var valDict = new Dictionary<string, object?>();
                foreach (var col in s.StudentScoreColumns)
                {
                    valDict[col.ScoreKey] = col.ScoreValue;
                }

                valDict["FINAL_SCORE"] = s.OverallScore;
                valDict["IS_PASSED"] = (s.OverallScore.HasValue && s.OverallScore.Value >= 5.0m);

                rowsContent.Add(new TopicMarkGradebookRowDto
                {
                    UserId = s.Id.ToString(),
                    FullName = s.StudentName,
                    Email = $"{s.StudentCode.ToLower()}@fpt.edu.vn",
                    Values = valDict
                });
            }

            return new TopicMarkGradebookSearchResponseDto
            {
                Columns = columns,
                Rows = new TopicMarkPageResponse<TopicMarkGradebookRowDto>
                {
                    Content = rowsContent,
                    Page = page,
                    Size = size,
                    TotalElements = totalElements,
                    TotalPages = totalPages,
                    Sort = sort
                }
            };
        }

        public async Task<TopicMarkGradebookRowDto?> GetStudentGradebookAsync(long id)
        {
            var student = await _scoreRepository.GetWithColumnsByIdAsync(id);
            if (student == null) return null;

            var valDict = new Dictionary<string, object?>();
            foreach (var col in student.StudentScoreColumns)
            {
                valDict[col.ScoreKey] = col.ScoreValue;
            }
            valDict["FINAL_SCORE"] = student.OverallScore;
            valDict["IS_PASSED"] = (student.OverallScore.HasValue && student.OverallScore.Value >= 5.0m);

            return new TopicMarkGradebookRowDto
            {
                UserId = student.Id.ToString(),
                FullName = student.StudentName,
                Email = $"{student.StudentCode.ToLower()}@fpt.edu.vn",
                Values = valDict
            };
        }

        public async Task<ClassScoreSheetDto> GetClassScoreSheetAsync(long classId)
        {
            var cls = await _classRepository.GetByIdAsync(classId);
            if (cls == null)
            {
                throw new NotFoundException($"Không tìm thấy lớp học với ID {classId}.");
            }

            var students = await _studentRepository.Query()
                .Include(s => s.MockProject)
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            var studentCodes = students.Select(s => s.StudentCode).ToList();
            var studentIds = students.Select(s => s.Id).ToList();

            // 1. Tự động tính điểm Chuyên cần từ danh sách điểm danh
            var attendances = await _attendanceRepository.Query()
                .Where(a => studentIds.Contains(a.StudentId) && a.IsActive)
                .ToListAsync();
            var attByStudent = attendances.GroupBy(a => a.StudentId).ToDictionary(g => g.Key, g => g.ToList());

            // 2. Lấy các điểm hiện có từ bảng StudentScore
            var existingScores = await _scoreRepository.QueryWithColumns()
                .Where(sc => studentCodes.Contains(sc.StudentCode))
                .ToListAsync();

            var scoreMap = existingScores
                .GroupBy(sc => sc.StudentCode)
                .ToDictionary(g => g.Key, g => g.First());

            var coreKeys = new[] { "attendance", "quiz1", "quiz2", "assignment", "practice", "midterm", "final", "project" };

            var items = students.Select(s =>
            {
                scoreMap.TryGetValue(s.StudentCode, out var sc);
                var scoresDict = new Dictionary<string, decimal?>();
                if (sc?.StudentScoreColumns != null)
                {
                    foreach (var col in sc.StudentScoreColumns)
                    {
                        scoresDict[col.ScoreKey] = col.ScoreValue;
                    }
                }

                // Tự động tính điểm chuyên cần (10.0 thang điểm)
                decimal calculatedAttendance = 10.0m;
                if (attByStudent.TryGetValue(s.Id, out var attList) && attList.Count > 0)
                {
                    var total = attList.Count;
                    var present = attList.Count(a => a.Status == "PRESENT");
                    var excused = attList.Count(a => a.Status == "EXCUSED");
                    var late = attList.Count(a => a.Status == "LATE");
                    calculatedAttendance = Math.Round(((present * 1.0m + excused * 1.0m + late * 0.7m) / total) * 10m, 1);
                }

                // Nếu chưa có điểm chuyên cần -> tự động điền
                if (!scoresDict.ContainsKey("attendance") || scoresDict["attendance"] == null)
                {
                    scoresDict["attendance"] = calculatedAttendance;
                }

                // Nếu học viên có Mock Project đã chấm điểm và chưa có điểm project -> tự động điền
                var studentProjScore = s.MockProject?.FinalScore;
                if ((!scoresDict.ContainsKey("project") || scoresDict["project"] == null) && studentProjScore.HasValue)
                {
                    scoresDict["project"] = studentProjScore.Value;
                }

                // Tính OverallScore
                var validCore = coreKeys
                    .Where(k => scoresDict.ContainsKey(k) && scoresDict[k].HasValue)
                    .Select(k => scoresDict[k]!.Value)
                    .ToList();

                decimal? computedOverall = sc?.OverallScore;
                if (!computedOverall.HasValue && validCore.Count > 0)
                {
                    var avg = validCore.Average();
                    var bonus = (scoresDict.TryGetValue("bonus", out var b) && b.HasValue) ? b.Value : 0m;
                    computedOverall = Math.Min(10m, Math.Round(avg + bonus, 2));
                }

                return new ClassScoreItemDto
                {
                    StudentId = s.Id,
                    StudentCode = s.StudentCode,
                    StudentName = s.FullName,
                    Email = s.Email,
                    ScoreId = sc?.Id,
                    Scores = scoresDict,
                    OverallScore = computedOverall
                };
            }).ToList();

            return new ClassScoreSheetDto
            {
                ClassId = cls.Id,
                ClassCode = cls.ClassCode,
                ClassName = cls.ClassName,
                Students = items
            };
        }

        public async Task<List<StudentScoreResponseDto>> SaveClassScoreBatchAsync(SaveClassScoreBatchRequest request)
        {
            var cls = await _classRepository.GetByIdAsync(request.ClassId);
            if (cls == null)
            {
                throw new NotFoundException($"Không tìm thấy lớp học với ID {request.ClassId}.");
            }

            var classNameToUse = !string.IsNullOrWhiteSpace(request.ClassName) ? request.ClassName : cls.ClassCode;

            var studentCodes = request.Items.Select(i => i.StudentCode).ToList();
            var existingScores = await _scoreRepository.QueryWithColumns()
                .Where(sc => studentCodes.Contains(sc.StudentCode))
                .ToListAsync();

            var existingMap = existingScores
                .GroupBy(sc => sc.StudentCode)
                .ToDictionary(g => g.Key, g => g.First());

            var coreKeys = new[] { "attendance", "quiz1", "quiz2", "assignment", "practice", "midterm", "final", "project" };
            var resultEntities = new List<StudentScore>();

            foreach (var item in request.Items)
            {
                decimal? overall = item.OverallScore;
                if (item.Scores != null && item.Scores.Count > 0)
                {
                    var validCore = coreKeys
                        .Where(k => item.Scores.ContainsKey(k) && item.Scores[k].HasValue)
                        .Select(k => item.Scores[k]!.Value)
                        .ToList();

                    if (validCore.Count > 0)
                    {
                        var avg = validCore.Average();
                        var bonus = (item.Scores.TryGetValue("bonus", out var b) && b.HasValue) ? b.Value : 0m;
                        overall = Math.Min(10m, Math.Round(avg + bonus, 2));
                    }
                }

                if (existingMap.TryGetValue(item.StudentCode ?? string.Empty, out var existing))
                {
                    existing.StudentName = item.StudentName ?? string.Empty;
                    existing.ClassName = classNameToUse;
                    existing.OverallScore = overall;
                    existing.UpdatedAt = DateTime.UtcNow;

                    existing.StudentScoreColumns.Clear();
                    if (item.Scores != null)
                    {
                        foreach (var kvp in item.Scores)
                        {
                            if (kvp.Value.HasValue)
                            {
                                existing.StudentScoreColumns.Add(new StudentScoreColumn
                                {
                                    StudentScoreId = existing.Id,
                                    ScoreKey = kvp.Key,
                                    ScoreValue = kvp.Value
                                });
                            }
                        }
                    }
                    resultEntities.Add(existing);
                }
                else
                {
                    var newEntity = new StudentScore
                    {
                        StudentCode = item.StudentCode ?? string.Empty,
                        StudentName = item.StudentName ?? string.Empty,
                        ClassName = classNameToUse,
                        OverallScore = overall,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (item.Scores != null)
                    {
                        foreach (var kvp in item.Scores)
                        {
                            if (kvp.Value.HasValue)
                            {
                                newEntity.StudentScoreColumns.Add(new StudentScoreColumn
                                {
                                    ScoreKey = kvp.Key,
                                    ScoreValue = kvp.Value
                                });
                            }
                        }
                    }

                    await _scoreRepository.AddAsync(newEntity);
                    resultEntities.Add(newEntity);
                }
            }

            await _scoreRepository.SaveChangesAsync();
            return resultEntities.Select(e => e.ToResponseDto()).ToList();
        }
    }
}


