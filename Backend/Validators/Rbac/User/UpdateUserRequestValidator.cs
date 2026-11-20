using Backend.DTOs.Rbac;
using FluentValidation;

namespace Backend.Validators.User
{
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
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
                .MinimumLength(6).WithMessage("Mật khẩu phải có tối thiểu 6 ký tự")
                .When(x => !string.IsNullOrEmpty(x.Password));
        }
    }
}
