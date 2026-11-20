using Backend.DTOs.Student.Score;
using FluentValidation;

namespace Backend.Validators.StudentScore
{
    public class UpdateStudentScoreRequestValidator : AbstractValidator<UpdateStudentScoreRequest>
    {
        public UpdateStudentScoreRequestValidator()
        {
            RuleFor(x => x.StudentCode)
                .NotEmpty().WithMessage("Mã sinh viên không được để trống")
                .MaximumLength(50).WithMessage("Mã sinh viên tối đa 50 ký tự");

            RuleFor(x => x.StudentName)
                .NotEmpty().WithMessage("Tên sinh viên không được để trống")
                .MaximumLength(100).WithMessage("Tên sinh viên tối đa 100 ký tự");

            RuleFor(x => x.ClassName)
                .NotEmpty().WithMessage("Tên lớp học không được để trống")
                .MaximumLength(100).WithMessage("Tên lớp học tối đa 100 ký tự");
        }
    }
}
