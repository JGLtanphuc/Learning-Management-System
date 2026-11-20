using Backend.DTOs.Teacher;
using FluentValidation;

namespace Backend.Validators.Teacher
{
    public class UpdateTeacherRequestValidator : AbstractValidator<UpdateTeacherRequest>
    {
        public UpdateTeacherRequestValidator()
        {
            RuleFor(x => x.TeacherCode)
                .MaximumLength(50).WithMessage("Mã giảng viên tối đa 50 ký tự");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email giảng viên không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(100).WithMessage("Email tối đa 100 ký tự");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Tên giảng viên không được để trống")
                .MaximumLength(50).WithMessage("Tên tối đa 50 ký tự");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Họ tối đa 50 ký tự");

            RuleFor(x => x.Password)
                .MinimumLength(6).WithMessage("Mật khẩu phải có tối thiểu 6 ký tự")
                .When(x => !string.IsNullOrEmpty(x.Password));
        }
    }
}
