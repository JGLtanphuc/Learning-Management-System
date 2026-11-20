using Backend.DTOs.Auth;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var (response, refreshToken) = await _authService.LoginAsync(request);

                // Lưu RefreshToken vào HttpOnly Cookie để FE tự động gửi qua withCredentials: true
                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Hỗ trợ cả môi trường dev localhost HTTP
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // Lấy refresh token từ Cookie hoặc từ Header X-Refresh-Token
            var token = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                token = Request.Headers["X-Refresh-Token"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { message = "Không tìm thấy refresh token trong yêu cầu." });
            }

            try
            {
                var (response, newRefreshToken) = await _authService.RefreshTokenAsync(token);

                Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                Response.Cookies.Delete("refreshToken");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi làm mới token: " + ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["refreshToken"];
            await _authService.LogoutAsync(token);
            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Đăng xuất thành công." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                await _authService.RegisterAsync(request);
                return Ok(new { message = "Đăng ký tài khoản thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đăng ký: " + ex.Message });
            }
        }

        /*[HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var token = await _authService.ForgotPasswordAsync(request.Email);
                return Ok(new
                {
                    message = "Link đặt lại mật khẩu đã được gửi tới email của bạn.",
                    token = token,
                    resetUrl = $"http://localhost:5173/forgot-password?token={token}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi xử lý quên mật khẩu: " + ex.Message });
            }
        }*/

        [HttpPatch("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Đặt lại mật khẩu thành công. Hãy đăng nhập bằng mật khẩu mới." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi đặt lại mật khẩu: " + ex.Message });
            }
        }

        [HttpPost("resend-verification")]
        public IActionResult ResendVerification([FromQuery] string email)
        {
            return Ok(new { message = $"Mã xác thực đã được gửi lại tới {email}." });
        }
    }
}