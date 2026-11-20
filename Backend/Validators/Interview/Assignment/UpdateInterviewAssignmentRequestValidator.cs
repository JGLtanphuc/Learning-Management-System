using Backend.DTOs.Interview;
using FluentValidation;

namespace Backend.Validators.Interview
{
    public class UpdateInterviewAssignmentRequestValidator : AbstractValidator<UpdateInterviewAssignmentRequest>
    {
        public UpdateInterviewAssignmentRequestValidator()
        {
            RuleFor(x => x.TopicId)
                .GreaterThan(0).WithMessage("Vui lòng chọn mục phỏng vấn.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề bài phỏng vấn không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề tối đa 200 ký tự.");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0).WithMessage("Thời lượng phỏng vấn phải lớn hơn 0 phút.");

            RuleFor(x => x.TotalQuestions)
                .GreaterThan(0).WithMessage("Tổng số câu hỏi phải lớn hơn 0.");

            RuleFor(x => x.EasyCount)
                .GreaterThanOrEqualTo(0).WithMessage("Số câu dễ phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.MediumCount)
                .GreaterThanOrEqualTo(0).WithMessage("Số câu trung bình phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.HardCount)
                .GreaterThanOrEqualTo(0).WithMessage("Số câu khó phải lớn hơn hoặc bằng 0.");
        }
    }
}
