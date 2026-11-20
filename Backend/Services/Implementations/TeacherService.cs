using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Common;
using Backend.Common.Exceptions;
using Backend.Data;
using Backend.DTOs.Common;
using Backend.DTOs.Teacher;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Implementations
{
    public class TeacherService : BaseService<User, TeacherResponseDto, CreateTeacherRequest, UpdateTeacherRequest>, ITeacherService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly AppDbContext _context;

        public TeacherService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            AppDbContext context)
            : base(userRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _context = context;
        }

        private static TeacherResponseDto BuildTeacherResponseDto(User user, List<TrainingClass> teacherClasses)
        {
            return new TeacherResponseDto
            {
                Id = user.Id,
                TeacherCode = user.TeacherCode,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                AssignedClasses = teacherClasses.Select(c => $"{c.ClassCode} ({c.ClassName})").ToList(),
                AssignedClassIds = teacherClasses.Select(c => c.Id).ToList(),
                TotalAssignedClasses = teacherClasses.Count
            };
        }

        protected override TeacherResponseDto MapToResponseDto(User entity)
        {
            var teacherClasses = _context.Classes
                .Where(c => c.TeacherId == entity.Id)
                .AsNoTracking()
                .ToList();

            return BuildTeacherResponseDto(entity, teacherClasses);
        }

        protected override User MapToEntity(CreateTeacherRequest request)
        {
            var rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "pass123" : request.Password;
            var teacherCode = string.IsNullOrWhiteSpace(request.TeacherCode)
                ? $"GV{DateTime.UtcNow:yyMMddHHmmss}"
                : request.TeacherCode.Trim();

            return new User
            {
                TeacherCode = teacherCode,
                Email = request.Email.Trim().ToLower(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                IsActive = request.IsActive
            };
        }

        protected override void UpdateEntity(User entity, UpdateTeacherRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.TeacherCode))
            {
                entity.TeacherCode = request.TeacherCode.Trim();
            }

            entity.Email = request.Email.Trim().ToLower();
            entity.FirstName = request.FirstName.Trim();
            entity.LastName = request.LastName?.Trim();
            entity.IsActive = request.IsActive;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
        }

        private async Task SyncAssignedClassesAsync(long teacherId, List<long>? classIds)
        {
            if (classIds == null) return;

            var currentClasses = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .ToListAsync();

            foreach (var cls in currentClasses)
            {
                if (!classIds.Contains(cls.Id))
                {
                    cls.TeacherId = null;
                }
            }

            var classesToAssign = await _context.Classes
                .Where(c => classIds.Contains(c.Id))
                .ToListAsync();

            foreach (var cls in classesToAssign)
            {
                cls.TeacherId = teacherId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<PageResponse<TeacherResponseDto>> GetPageAsync(TeacherFilterDto filter)
        {
            var query = _userRepository.QueryWithRoles()
                .Where(u => u.Roles.Any(r => r.Name == "TEACHER"))
                .AsNoTracking();

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(u =>
                    (u.TeacherCode != null && u.TeacherCode.ToLower().Contains(term)) ||
                    u.Email.ToLower().Contains(term) ||
                    u.FirstName.ToLower().Contains(term) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(u => u.Id)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            var teacherIds = items.Select(u => u.Id).ToList();
            var classes = await _context.Classes
                .Where(c => c.TeacherId.HasValue && teacherIds.Contains(c.TeacherId.Value))
                .AsNoTracking()
                .ToListAsync();

            var classesByTeacher = classes
                .GroupBy(c => c.TeacherId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dtos = items.Select(u =>
            {
                var teacherClasses = classesByTeacher.GetValueOrDefault(u.Id) ?? new List<TrainingClass>();
                return BuildTeacherResponseDto(u, teacherClasses);
            }).ToList();

            return new PageResponse<TeacherResponseDto>
            {
                Content = dtos,
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<TeacherResponseDto?> GetByIdAsync(long id)
        {
            var user = await _userRepository.QueryWithRoles()
                .Where(u => u.Id == id && u.Roles.Any(r => r.Name == "TEACHER"))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user == null) return null;

            return MapToResponseDto(user);
        }

        public override async Task<TeacherResponseDto> CreateAsync(CreateTeacherRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var existing = await _userRepository.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existing != null)
            {
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(request.TeacherCode))
            {
                var normalizedCode = request.TeacherCode.Trim().ToLower();
                var existingCode = await _userRepository.FirstOrDefaultAsync(u => u.TeacherCode != null && u.TeacherCode.ToLower() == normalizedCode);
                if (existingCode != null)
                {
                    throw new ConflictException($"Mã giảng viên '{request.TeacherCode}' đã được sử dụng trong hệ thống.");
                }
            }

            var user = MapToEntity(request);

            var teacherRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "TEACHER");
            if (teacherRole != null)
            {
                user.Roles.Add(teacherRole);
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            await SyncAssignedClassesAsync(user.Id, request.ClassIds);

            return await GetByIdAsync(user.Id) ?? MapToResponseDto(user);
        }

        public override async Task<TeacherResponseDto?> UpdateAsync(long id, UpdateTeacherRequest request)
        {
            var user = await _userRepository.QueryWithRoles()
                .FirstOrDefaultAsync(u => u.Id == id && u.Roles.Any(r => r.Name == "TEACHER"));

            if (user == null) return null;

            var normalizedEmail = request.Email.Trim().ToLower();
            var existing = await _userRepository.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != id);
            if (existing != null)
            {
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng bởi người dùng khác.");
            }

            if (!string.IsNullOrWhiteSpace(request.TeacherCode))
            {
                var normalizedCode = request.TeacherCode.Trim().ToLower();
                var existingCode = await _userRepository.FirstOrDefaultAsync(u => u.TeacherCode != null && u.TeacherCode.ToLower() == normalizedCode && u.Id != id);
                if (existingCode != null)
                {
                    throw new ConflictException($"Mã giảng viên '{request.TeacherCode}' đã được sử dụng bởi người dùng khác.");
                }
            }

            UpdateEntity(user, request);
            await _userRepository.SaveChangesAsync();

            await SyncAssignedClassesAsync(user.Id, request.ClassIds);

            return await GetByIdAsync(user.Id);
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var user = await _userRepository.QueryWithRoles()
                .FirstOrDefaultAsync(u => u.Id == id && u.Roles.Any(r => r.Name == "TEACHER"));

            if (user == null) return false;

            // Unassign all classes
            var assignedClasses = await _context.Classes
                .Where(c => c.TeacherId == user.Id)
                .ToListAsync();

            foreach (var cls in assignedClasses)
            {
                cls.TeacherId = null;
            }

            // Remove roles mapping
            user.Roles.Clear();

            // Delete user entity
            _userRepository.Delete(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var page = await GetPageAsync(new TeacherFilterDto { Page = 0, Size = 1000 });
            var headers = new[] { "STT", "Mã giảng viên", "Họ và tên", "Email", "Số lớp phụ trách", "Danh sách lớp phụ trách", "Trạng thái", "Ngày tạo" };
            var rows = page.Content.Select((t, idx) => new object?[]
            {
                idx + 1,
                t.TeacherCode,
                t.FullName,
                t.Email,
                t.TotalAssignedClasses,
                string.Join("; ", t.AssignedClasses),
                t.IsActive ? "Đang hoạt động" : "Tạm dừng",
                t.CreatedAt
            });

            return ExcelExportHelper.CreateWorkbook("Giảng viên", "DANH SÁCH GIẢNG VIÊN (TEACHERS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var page = await GetPageAsync(new TeacherFilterDto { Page = 0, Size = 1000 });
            var sb = new StringBuilder();
            sb.AppendLine("STT,Mã giảng viên,Họ và tên,Email,Số lớp phụ trách,Danh sách lớp phụ trách,Trạng thái,Ngày tạo");

            int index = 1;
            foreach (var t in page.Content)
            {
                var assignedStr = $"\"{string.Join("; ", t.AssignedClasses).Replace("\"", "\"\"")}\"";
                var status = t.IsActive ? "Đang hoạt động" : "Tạm dừng";
                sb.AppendLine($"{index++},\"{t.TeacherCode}\",\"{t.FullName}\",\"{t.Email}\",{t.TotalAssignedClasses},{assignedStr},{status},{t.CreatedAt:dd/MM/yyyy HH:mm}");
            }

            return CsvExportHelper.ToUtf8BomBytes(sb.ToString());
        }
    }
}
