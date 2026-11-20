using Backend.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Student;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class StudentService : BaseService<Student, StudentResponseDto, CreateStudentRequest, UpdateStudentRequest>, IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly AppDbContext _context;

        public StudentService(
            IStudentRepository studentRepository,
            IClassRepository classRepository,
            ICurrentUserService currentUserService,
            AppDbContext context) : base(studentRepository)
        {
            _studentRepository = studentRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
            _context = context;
        }

        protected override StudentResponseDto MapToResponseDto(Student entity) => entity.ToResponseDto();
        protected override Student MapToEntity(CreateStudentRequest request) => request.ToEntity();
        protected override void UpdateEntity(Student entity, UpdateStudentRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<StudentResponseDto>> GetPageAsync(StudentFilterDto filter)
        {
            var query = _studentRepository.QueryWithClass().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(s => s.Class != null && s.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            if (filter.ClassId.HasValue)
            {
                query = query.Where(s => s.ClassId == filter.ClassId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var statuses = filter.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (statuses.Length > 1)
                {
                    query = query.Where(s => statuses.Contains(s.Status));
                }
                else
                {
                    query = query.Where(s => s.Status == statuses[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.University))
            {
                query = query.Where(s => s.University != null && s.University.Contains(filter.University));
            }

            if (!string.IsNullOrWhiteSpace(filter.Major))
            {
                query = query.Where(s => s.Major != null && s.Major.Contains(filter.Major));
            }

            if (filter.MinGpa.HasValue)
            {
                query = query.Where(s => s.Gpa >= filter.MinGpa.Value);
            }

            if (filter.MaxGpa.HasValue)
            {
                query = query.Where(s => s.Gpa <= filter.MaxGpa.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(s => s.StudentCode.ToLower().Contains(term) ||
                                         s.FullName.ToLower().Contains(term) ||
                                         s.Email.ToLower().Contains(term) ||
                                         (s.Phone != null && s.Phone.Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<StudentResponseDto>
            {
                Content = items.Select(s => s.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<StudentResponseDto?> GetByIdAsync(long id)
        {
            var item = await _studentRepository.QueryWithClass()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (item == null) return null;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (item.Class == null || item.Class.TeacherId != _currentUserService.UserId.Value)
                    return null;
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                if (item.Email != email && (!uid.HasValue || item.UserId != uid.Value))
                    return null;
            }

            return item.ToResponseDto();
        }

        public override async Task<StudentResponseDto> CreateAsync(CreateStudentRequest request)
        {
            // Business rule: StudentCode uniqueness
            var existingCode = await _studentRepository.GetByCodeAsync(request.StudentCode.Trim().ToUpper());
            if (existingCode != null)
            {
                throw new ConflictException($"Mã học viên \'{request.StudentCode}\' đã tồn tại trong hệ thống.");
            }

            // Business rule: Email uniqueness
            var existingEmail = await _studentRepository.GetByEmailAsync(request.Email.Trim().ToLower());
            if (existingEmail != null)
            {
                throw new ConflictException($"Email \'{request.Email}\' đã được sử dụng bởi học viên khác.");
            }

            // Check valid class if assigned
            if (request.ClassId.HasValue)
            {
                var cls = await _classRepository.GetByIdAsync(request.ClassId.Value);
                if (cls == null)
                {
                    throw new NotFoundException($"Lớp học với ID {request.ClassId.Value} không tồn tại.");
                }
            }

            var entity = request.ToEntity();

            // Tự động kiểm tra hoặc khởi tạo tài khoản User cho học viên (Role: STUDENT, Mật khẩu mặc định: pass123)
            var studentEmail = request.Email.Trim().ToLower();
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail);
            if (existingUser != null)
            {
                entity.UserId = existingUser.Id;
            }
            else
            {
                var nameParts = (request.FullName ?? "Student").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[^1] : "Student";
                var lastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Take(nameParts.Length - 1)) : null;

                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "STUDENT");
                var newUser = new User
                {
                    Email = studentEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123"),
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = true
                };

                if (studentRole != null)
                {
                    newUser.Roles.Add(studentRole);
                }

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                entity.UserId = newUser.Id;
            }

            await _studentRepository.AddAsync(entity);
            await _studentRepository.SaveChangesAsync();

            // Reload with relations for complete response
            var created = await _studentRepository.QueryWithClass().FirstOrDefaultAsync(s => s.Id == entity.Id);
            return created!.ToResponseDto();
        }

        public override async Task<StudentResponseDto?> UpdateAsync(long id, UpdateStudentRequest request)
        {
            var entity = await _studentRepository.Query().FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return null;

            // Check code uniqueness
            var existingCode = await _studentRepository.GetByCodeAsync(request.StudentCode.Trim().ToUpper());
            if (existingCode != null && existingCode.Id != id)
            {
                throw new ConflictException($"Mã học viên \'{request.StudentCode}\' đã được sử dụng bởi học viên khác.");
            }

            // Check email uniqueness
            var existingEmail = await _studentRepository.GetByEmailAsync(request.Email.Trim().ToLower());
            if (existingEmail != null && existingEmail.Id != id)
            {
                throw new ConflictException($"Email \'{request.Email}\' đã được sử dụng bởi học viên khác.");
            }

            // Check valid class
            if (request.ClassId.HasValue)
            {
                var cls = await _classRepository.GetByIdAsync(request.ClassId.Value);
                if (cls == null)
                {
                    throw new NotFoundException($"Lớp học với ID {request.ClassId.Value} không tồn tại.");
                }
            }

            entity.UpdateEntity(request);

            if (entity.UserId.HasValue)
            {
                var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId.Value);
                if (linkedUser != null)
                {
                    linkedUser.Email = request.Email.Trim().ToLower();
                    var nameParts = (request.FullName ?? "Student").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    linkedUser.FirstName = nameParts.Length > 0 ? nameParts[^1] : "Student";
                    linkedUser.LastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Take(nameParts.Length - 1)) : null;
                }
            }

            await _studentRepository.SaveChangesAsync();

            var updated = await _studentRepository.QueryWithClass().FirstOrDefaultAsync(s => s.Id == id);
            return updated!.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _studentRepository.Query().FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return false;

            _studentRepository.Delete(entity);
            await _studentRepository.SaveChangesAsync();
            return true;
        }

        
        private IQueryable<Student> GetScopedStudentQuery()
        {
            var query = _studentRepository.QueryWithClass().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(s => s.Class != null && s.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value));
            }

            return query;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await GetScopedStudentQuery()
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            var headers = new[] { "STT", "Mã học viên", "Họ và tên", "Email", "Số điện thoại", "Lớp học", "Trường ĐH", "Chuyên ngành", "GPA", "Trạng thái", "Kích hoạt" };
            int stt = 1;
            var rows = items.Select(s => new object?[]
            {
                stt++,
                s.StudentCode,
                s.FullName,
                s.Email,
                s.Phone,
                s.Class?.ClassName ?? "",
                s.University ?? "",
                s.Major ?? "",
                s.Gpa,
                s.Status.ToString(),
                s.IsActive
            });

            return ExcelExportHelper.CreateWorkbook("Danh sách học viên", "DANH SÁCH HỌC VIÊN (STUDENTS)", headers, rows);
        }

        public async Task<byte[]> ExportPdfAsync()
        {
            var items = await GetScopedStudentQuery()
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI", "Arial", "Roboto", "Helvetica"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("FAMS - HỆ THỐNG QUẢN LÝ ĐÀO TẠO")
                                    .FontSize(11).Bold().FontColor("#1E40AF");
                                titleCol.Item().Text("DANH SÁCH HỌC VIÊN (STUDENT ROSTER)")
                                    .FontSize(16).Bold().FontColor("#0F172A");
                            });

                            row.ConstantItem(220).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                                metaCol.Item().Text($"Tổng số học viên: {items.Count}").FontSize(9).Bold().FontColor("#1E293B");
                            });
                        });

                        col.Item().PaddingTop(6).PaddingBottom(6).LineHorizontal(1).LineColor("#CBD5E1");
                    });

                    page.Content().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);   // STT
                            columns.RelativeColumn(1.3f); // Mã học viên
                            columns.RelativeColumn(2.0f); // Họ và tên
                            columns.RelativeColumn(2.2f); // Email
                            columns.RelativeColumn(1.5f); // Lớp học
                            columns.RelativeColumn(1.8f); // Trường ĐH
                            columns.RelativeColumn(1.6f); // Chuyên ngành
                            columns.ConstantColumn(36);   // GPA
                            columns.RelativeColumn(1.1f); // Trạng thái
                        });

                        table.Header(header =>
                        {
                            void StyleCell(IContainer cell, string text, bool alignCenter = false)
                            {
                                var textContainer = cell
                                    .Background("#1E40AF")
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(4);
                                var textBlock = textContainer.Text(text).FontSize(8.5f).Bold().FontColor("#FFFFFF");
                                if (alignCenter) textContainer.AlignCenter();
                            }

                            StyleCell(header.Cell(), "STT", true);
                            StyleCell(header.Cell(), "Mã HV");
                            StyleCell(header.Cell(), "Họ và tên");
                            StyleCell(header.Cell(), "Email");
                            StyleCell(header.Cell(), "Lớp học");
                            StyleCell(header.Cell(), "Trường ĐH");
                            StyleCell(header.Cell(), "Chuyên ngành");
                            StyleCell(header.Cell(), "GPA", true);
                            StyleCell(header.Cell(), "Trạng thái", true);
                        });

                        int stt = 1;
                        foreach (var s in items)
                        {
                            var bg = stt % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                            void StyleData(IContainer cell, string text, bool alignCenter = false, bool isBold = false)
                            {
                                var c = cell
                                    .Background(bg)
                                    .BorderBottom(0.5f).BorderColor("#E2E8F0")
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(4);
                                if (alignCenter) c = c.AlignCenter();
                                var t = c.Text(text).FontSize(8);
                                if (isBold) t.Bold();
                            }

                            StyleData(table.Cell(), stt.ToString(), true);
                            StyleData(table.Cell(), s.StudentCode, isBold: true);
                            StyleData(table.Cell(), s.FullName);
                            StyleData(table.Cell(), s.Email);
                            StyleData(table.Cell(), s.Class?.ClassName ?? "-");
                            StyleData(table.Cell(), s.University ?? "-");
                            StyleData(table.Cell(), s.Major ?? "-");
                            StyleData(table.Cell(), s.Gpa?.ToString("0.0") ?? "-", true, isBold: true);
                            StyleData(table.Cell(), s.Status.ToString(), true);

                            stt++;
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("FAS System - Tài liệu nội bộ đào tạo").FontSize(8).FontColor("#94A3B8");
                        row.ConstantItem(100).AlignRight().Text(x =>
                        {
                            x.Span("Trang ").FontSize(8).FontColor("#94A3B8");
                            x.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                            x.Span(" / ").FontSize(8).FontColor("#94A3B8");
                            x.TotalPages().FontSize(8).FontColor("#94A3B8");
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await GetScopedStudentQuery()
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,StudentCode,FullName,Email,Phone,ClassName,University,Major,Gpa,Status,IsActive");
            foreach (var s in items)
            {
                csv.AppendLine($"{s.Id},\"{s.StudentCode}\",\"{s.FullName}\",\"{s.Email}\",\"{s.Phone}\",\"{s.Class?.ClassName}\",\"{s.University}\",\"{s.Major}\",{s.Gpa},{s.Status},{s.IsActive}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }

        public async Task<List<StudentResponseDto>> GetAvailableForMockProjectAsync(long? currentProjectId, long? classId = null)
        {
            var query = _studentRepository.QueryWithClass()
                .Where(s => s.IsActive);

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassId == classId.Value);
            }

            if (currentProjectId.HasValue)
            {
                query = query.Where(s => s.MockProjectId == null || s.MockProjectId == currentProjectId.Value);
            }
            else
            {
                query = query.Where(s => s.MockProjectId == null);
            }

            var items = await query.OrderBy(s => s.FullName).ToListAsync();
            return items.Select(s => s.ToResponseDto()).ToList();
        }
    }
}

