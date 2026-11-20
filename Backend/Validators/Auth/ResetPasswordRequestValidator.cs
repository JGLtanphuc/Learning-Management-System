using Backend.DTOs.Auth;
using FluentValidation;

namespace Backend.Validators.Auth
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Mã xác thực token không được để trống");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có tối thiểu 6 ký tự");
        }
    }
}
