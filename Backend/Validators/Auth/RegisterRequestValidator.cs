using Backend.DTOs.Auth;
using FluentValidation;

namespace Backend.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(100).WithMessage("Email tối đa 100 ký tự");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Tên không được để trống")
                .MaximumLength(50).WithMessage("Tên tối đa 50 ký tự");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Họ tối đa 50 ký tự");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu phải có tối thiểu 6 ký tự");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp")
                .When(x => !string.IsNullOrEmpty(x.ConfirmPassword));
        }
    }
}
