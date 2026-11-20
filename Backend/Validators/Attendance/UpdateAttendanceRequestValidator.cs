using Backend.DTOs.Attendance;
using FluentValidation;

namespace Backend.Validators.Attendance
{
    public class UpdateAttendanceRequestValidator : AbstractValidator<UpdateAttendanceRequest>
    {
        public UpdateAttendanceRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("Vui lòng chọn học viên");

            RuleFor(x => x.ClassId)
                .GreaterThan(0).WithMessage("Vui lòng chọn lớp học");

            RuleFor(x => x.AttendanceDate)
                .NotEmpty().WithMessage("Ngày điểm danh không được để trống");

            RuleFor(x => x.Slot)
                .NotEmpty().WithMessage("Ca học không được để trống")
                .Matches("^(MORNING|AFTERNOON|FULLDAY)$")
                .WithMessage("Ca học phải là MORNING, AFTERNOON hoặc FULLDAY");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Trạng thái điểm danh không được để trống")
                .Matches("^(PRESENT|ABSENT|EXCUSED|LATE)$")
                .WithMessage("Trạng thái điểm danh phải là PRESENT, ABSENT, EXCUSED hoặc LATE");

            RuleFor(x => x.Note)
                .MaximumLength(255).WithMessage("Ghi chú tối đa 255 ký tự");
        }
    }
}
