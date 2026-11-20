using Backend.Common;
using Backend.DTOs.Common;
using Backend.DTOs.Rbac;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Implementations
{
    public class UserService : BaseService<User, UserResponseDto, CreateUserRequest, UpdateUserRequest>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
            : base(userRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        protected override UserResponseDto MapToResponseDto(User entity) => entity.ToResponseDto();

        protected override User MapToEntity(CreateUserRequest request)
        {
            var rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "password123" : request.Password;
            return request.ToEntity(BCrypt.Net.BCrypt.HashPassword(rawPassword));
        }

        protected override void UpdateEntity(User entity, UpdateUserRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<UserResponseDto>> GetPageAsync(int page, int size, string? search, bool? isActive, long? roleId)
        {
            var query = _userRepository.QueryWithRoles().AsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.Roles.Any(r => r.Id == roleId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.FirstName.ToLower().Contains(term) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(u => u.Id)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return new PageResponse<UserResponseDto>
            {
                Content = items.Select(u => u.ToResponseDto()).ToList(),
                Page = page,
                Size = size,
                TotalElements = totalElements
            };
        }

        public override async Task<UserResponseDto?> GetByIdAsync(long id)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(id);
            return user?.ToResponseDto();
        }

        public override async Task<UserResponseDto> CreateAsync(CreateUserRequest request)
        {
            var rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "password123" : request.Password;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            var user = request.ToEntity(passwordHash);

            if (request.RoleIds != null && request.RoleIds.Any())
            {
                var roles = await _roleRepository.Query().Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
                foreach (var r in roles) user.Roles.Add(r);
            }
            else
            {
                var defaultRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "STUDENT");
                if (defaultRole != null) user.Roles.Add(defaultRole);
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user.ToResponseDto();
        }

        public override async Task<UserResponseDto?> UpdateAsync(long id, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(id);
            if (user == null) return null;

            user.UpdateEntity(request);

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            if (request.RoleIds != null)
            {
                user.Roles.Clear();
                var roles = await _roleRepository.Query().Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
                foreach (var r in roles) user.Roles.Add(r);
            }

            await _userRepository.SaveChangesAsync();
            return user.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _userRepository.QueryWithRoles().AsNoTracking().OrderBy(u => u.Id).ToListAsync();
            var headers = new[] { "ID", "Email", "Họ và đệm", "Tên", "Vai trò (Roles)", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(u => new object?[]
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                string.Join("; ", u.Roles.Select(r => r.Name)),
                u.IsActive,
                u.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Danh sách tài khoản", "DANH SÁCH TÀI KHOẢN NGƯỜI DÙNG (USERS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _userRepository.QueryWithRoles().AsNoTracking().OrderBy(u => u.Id).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Id,Email,FirstName,LastName,Roles,IsActive,CreatedAt");
            foreach (var item in items)
            {
                var roleNames = string.Join(";", item.Roles.Select(r => r.Name));
                csv.AppendLine($"{item.Id},\"{item.Email}\",\"{item.FirstName}\",\"{item.LastName}\",\"{roleNames}\",{item.IsActive},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }
    }
}
