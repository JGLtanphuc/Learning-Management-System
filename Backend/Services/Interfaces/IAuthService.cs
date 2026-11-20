using Backend.DTOs.Auth;

namespace Backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(LoginResponse Response, string RefreshToken)> LoginAsync(LoginRequest request);
        Task<(LoginResponse Response, string NewRefreshToken)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string? refreshToken = null);
        Task RegisterAsync(RegisterRequest request);
        Task<string> ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}