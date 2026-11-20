using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class
{
    public class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
    {
        public UpdateClassRequestValidator()
        {
            RuleFor(x => x.ClassCode)
                .NotEmpty().WithMessage("Mã lớp học không được để trống")
                .Length(3, 50).WithMessage("Mã lớp học phải có từ 3 đến 50 ký tự")
                .Matches(@"^[A-Za-z0-9_.-]+$").WithMessage("Mã lớp chỉ được chứa chữ cái, số, gạch dưới hoặc gạch ngang");

            RuleFor(x => x.ClassName)
                .NotEmpty().WithMessage("Tên lớp học không được để trống")
                .Length(3, 150).WithMessage("Tên lớp học phải có từ 3 đến 150 ký tự");

            RuleFor(x => x.ProgramName)
                .MaximumLength(100).WithMessage("Tên chương trình đào tạo tối đa 100 ký tự");

            RuleFor(x => x.Capacity)
                .NotNull().WithMessage("Sĩ số lớp học không được để trống")
                .InclusiveBetween(1, 150).WithMessage("Quy mô sĩ số lớp học phải từ 1 đến 150 học viên");

            RuleFor(x => x.Location)
                .MaximumLength(100).WithMessage("Địa điểm tối đa 100 ký tự");

            RuleFor(x => x.Room)
                .MaximumLength(50).WithMessage("Phòng học tối đa 50 ký tự");

            RuleFor(x => x.Status)
                .Matches("^(PLANNING|OPEN_FOR_REGISTRATION|ONGOING|COMPLETED|CANCELLED)$")
                .WithMessage("Trạng thái lớp phải là PLANNING, OPEN_FOR_REGISTRATION, ONGOING, COMPLETED hoặc CANCELLED");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
                .WithMessage("Ngày kết thúc khóa học phải sau hoặc bằng ngày bắt đầu.")
                .WithName("EndDate");
        }
    }
}
