using Backend.Common.Exceptions;
using Backend.Common.Security;
using Backend.DTOs.Auth;
using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenHelper _jwtTokenHelper;

        public AuthService(
            AppDbContext context,
            IUserRepository userRepository,
            JwtTokenHelper jwtTokenHelper)
        {
            _context = context;
            _userRepository = userRepository;
            _jwtTokenHelper = jwtTokenHelper;
        }

        public async Task<(LoginResponse Response, string RefreshToken)> LoginAsync(LoginRequest request)
        {
            // 1. Tìm User theo email kèm các Role và Permission thông qua IUserRepository
            var user = await _userRepository.GetByEmailWithRolesAsync(request.Email.Trim());

            if (user == null)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
            }

            // 2. Kiểm tra mật khẩu bằng BCrypt
            bool isPasswordMatch = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordMatch)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
            }

            // 3. Gom danh sách Roles và Permissions
            var roles = user.Roles.Select(r => r.Name).ToList();
            var permissions = user.Roles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct().ToList();

            var rolePermissions = new Dictionary<string, List<string>>();
            foreach (var r in user.Roles)
            {
                rolePermissions[r.Name] = r.Permissions.Select(p => p.Name).ToList();
            }

            EnsureImpliedInterviewPermissions(permissions, rolePermissions);

            // 4. Sinh Token JWT (Access Token)
            string token = _jwtTokenHelper.GenerateToken(user, roles, permissions);

            // 5. Sinh Refresh Token ngẫu nhiên an toàn và lưu vào DB
            var rawRefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = rawRefreshToken,
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
                Revoked = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            // 6. Đóng gói vào DTO trả về cho Controller
            var response = new LoginResponse
            {
                AccessToken = token,
                RefreshToken = rawRefreshToken,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "STUDENT",
                Roles = roles,
                Permissions = permissions,
                RolePermissions = rolePermissions
            };

            return (response, rawRefreshToken);
        }

        public async Task<(LoginResponse Response, string NewRefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var tokenRecord = await _context.RefreshTokens
                .Include(t => t.User)
                    .ThenInclude(u => u.Roles)
                        .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(t => t.TokenHash == refreshToken);

            if (tokenRecord == null || tokenRecord.Revoked || tokenRecord.ExpiryDate <= DateTimeOffset.UtcNow)
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");
            }

            var user = tokenRecord.User;
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Tài khoản người dùng đã bị khóa.");
            }

            // Thu hồi Refresh Token cũ (Token Rotation)
            tokenRecord.Revoked = true;

            // Sinh Refresh Token mới
            var newRefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshToken,
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
                Revoked = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(newRefreshTokenEntity);

            // Gom danh sách Roles và Permissions
            var roles = user.Roles.Select(r => r.Name).ToList();
            var permissions = user.Roles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct().ToList();

            var rolePermissions = new Dictionary<string, List<string>>();
            foreach (var r in user.Roles)
            {
                rolePermissions[r.Name] = r.Permissions.Select(p => p.Name).ToList();
            }

            EnsureImpliedInterviewPermissions(permissions, rolePermissions);

            // Sinh Access Token mới
            string newAccessToken = _jwtTokenHelper.GenerateToken(user, roles, permissions);

            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "STUDENT",
                Roles = roles,
                Permissions = permissions,
                RolePermissions = rolePermissions
            };

            return (response, newRefreshToken);
        }

        public async Task LogoutAsync(string? refreshToken = null)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == refreshToken);
                if (tokenRecord != null)
                {
                    tokenRecord.Revoked = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var existingUser = await _userRepository.Query().AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser)
            {
                throw new ConflictException("Email đã được đăng ký trên hệ thống.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Mặc định gán Role STUDENT
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "STUDENT" || r.Name == "ROLE_STUDENT");
            
            var user = new User
            {
                Email = request.Email.Trim(),
                PasswordHash = passwordHash,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (studentRole != null)
            {
                user.Roles.Add(studentRole);
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            var user = await _userRepository.Query().FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                // Để bảo mật không lộ email, vẫn trả về token giả lập
                return Guid.NewGuid().ToString("N");
            }

            // Tạo token mới lưu vào bảng OneTimeTokens
            var tokenString = Guid.NewGuid().ToString("N");
            var oneTimeToken = new OneTimeToken
            {
                UserId = user.Id,
                TokenHash = tokenString,
                Type = "RESET_PASSWORD",
                ExpiryDate = DateTimeOffset.UtcNow.AddHours(24),
                Used = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.OneTimeTokens.Add(oneTimeToken);
            await _context.SaveChangesAsync();

            return tokenString;
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var tokenRecord = await _context.OneTimeTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == request.Token && !t.Used && t.ExpiryDate > DateTimeOffset.UtcNow);

            if (tokenRecord == null)
            {
                throw new BadRequestException("Mã xác thực không hợp lệ hoặc đã hết hạn.");
            }

            tokenRecord.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            tokenRecord.User.UpdatedAt = DateTime.UtcNow;
            tokenRecord.Used = true;

            await _context.SaveChangesAsync();
        }

        private static readonly string[] ImpliedInterviewPermissions = new[]
        {
            "INTERVIEW_ANSWER_READ",
            "INTERVIEW_ANSWER_CREATE",
            "INTERVIEW_ANSWER_UPDATE",
            "INTERVIEW_ANSWER_DELETE",
            "INTERVIEW_ASSIGNMENT_REVIEW",
            "EXAM_READ",
            "EXAM_CREATE",
            "EXAM_UPDATE",
            "EXAM_DELETE"
        };

        private static void EnsureImpliedInterviewPermissions(List<string> permissions, Dictionary<string, List<string>> rolePermissions)
        {
            if (permissions.Contains("INTERVIEW_ASSIGNMENT_READ"))
            {
                foreach (var p in ImpliedInterviewPermissions)
                {
                    if (!permissions.Contains(p)) permissions.Add(p);
                }

                foreach (var kvp in rolePermissions)
                {
                    if (kvp.Value.Contains("INTERVIEW_ASSIGNMENT_READ"))
                    {
                        foreach (var p in ImpliedInterviewPermissions)
                        {
                            if (!kvp.Value.Contains(p)) kvp.Value.Add(p);
                        }
                    }
                }
            }
        }
    }
}
