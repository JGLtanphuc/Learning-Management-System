using Backend.DTOs.Class.Score;
using FluentValidation;

namespace Backend.Validators.StudentScore
{
    public class SaveClassScoreBatchRequestValidator : AbstractValidator<SaveClassScoreBatchRequest>
    {
        public SaveClassScoreBatchRequestValidator()
        {
            RuleFor(x => x.ClassId)
                .GreaterThan(0).WithMessage("Mã lớp học không hợp lệ");

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Danh sách điểm sinh viên không được null");
        }
    }
}
