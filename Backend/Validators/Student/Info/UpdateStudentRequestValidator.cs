using Backend.DTOs.Student;
using FluentValidation;

namespace Backend.Validators.Student
{
    public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
    {
        public UpdateStudentRequestValidator()
        {
            RuleFor(x => x.StudentCode)
                .NotEmpty().WithMessage("Mã học viên không được để trống")
                .Length(3, 50).WithMessage("Mã học viên phải từ 3 đến 50 ký tự")
                .Matches(@"^[A-Za-z0-9_.-]+$").WithMessage("Mã học viên chỉ được chứa chữ cái, số, gạch dưới hoặc gạch ngang");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên học viên không được để trống")
                .Length(2, 100).WithMessage("Họ và tên phải từ 2 đến 100 ký tự");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email học viên không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(100).WithMessage("Email tối đa 100 ký tự");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^0[0-9]{9}$").WithMessage("Số điện thoại phải có đúng 10 chữ số (bắt đầu bằng 0)");

            RuleFor(x => x.University)
                .MaximumLength(100).WithMessage("Tên trường đại học tối đa 100 ký tự");

            RuleFor(x => x.Major)
                .MaximumLength(100).WithMessage("Chuyên ngành đào tạo tối đa 100 ký tự");

            RuleFor(x => x.Gpa)
                .InclusiveBetween(0.0m, 10.0m).When(x => x.Gpa.HasValue)
                .WithMessage("Điểm GPA phải nằm trong khoảng từ 0.0 đến 10.0");

            RuleFor(x => x.Status)
                .Matches("^(ACTIVE|SUSPENDED|DROPOUT|GRADUATED)$")
                .WithMessage("Trạng thái học viên phải là ACTIVE, SUSPENDED, DROPOUT hoặc GRADUATED");
        }
    }
}
