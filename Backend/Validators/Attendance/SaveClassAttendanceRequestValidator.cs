using Backend.DTOs.Attendance;
using FluentValidation;

namespace Backend.Validators.Attendance
{
    public class SaveClassAttendanceRequestValidator : AbstractValidator<SaveClassAttendanceRequest>
    {
        public SaveClassAttendanceRequestValidator()
        {
            RuleFor(x => x.ClassId)
                .GreaterThan(0).WithMessage("Mã lớp học không hợp lệ");

            RuleFor(x => x.Slot)
                .NotEmpty().WithMessage("Ca học không được để trống");

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Danh sách điểm danh không được null");
        }
    }
}
