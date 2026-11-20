using System.Linq;
using Backend.DTOs.Interview;
using FluentValidation;

namespace Backend.Validators.Interview
{
    public class CreateInterviewQuestionRequestValidator : AbstractValidator<CreateInterviewQuestionRequest>
    {
        public CreateInterviewQuestionRequestValidator()
        {
            RuleFor(x => x.TopicId)
                .GreaterThan(0).WithMessage("Vui lòng chọn mục phỏng vấn.");

            RuleFor(x => x.QuestionText)
                .NotEmpty().WithMessage("Nội dung câu hỏi không được để trống.")
                .MaximumLength(1000).WithMessage("Nội dung câu hỏi tối đa 1000 ký tự.");

            RuleFor(x => x.Difficulty)
                .Must(d => new[] { "EASY", "MEDIUM", "HARD" }.Contains(d.ToUpper()))
                .WithMessage("Độ khó phải là EASY, MEDIUM hoặc HARD.");

            RuleFor(x => x.QuestionType)
                .Must(t => string.IsNullOrWhiteSpace(t) || new[] { "WRITTEN", "MCQ" }.Contains(t.ToUpper()))
                .WithMessage("Loại câu hỏi chỉ hỗ trợ Tự luận (WRITTEN) hoặc Trắc nghiệm (MCQ).");

            When(x => !string.IsNullOrWhiteSpace(x.QuestionType) && x.QuestionType.ToUpper() == "MCQ", () =>
            {
                RuleFor(x => x.OptionsJson)
                    .NotEmpty().WithMessage("Câu hỏi trắc nghiệm bắt buộc phải có danh sách đáp án (OptionsJson).");

                RuleFor(x => x.CorrectOptionId)
                    .NotEmpty().WithMessage("Câu hỏi trắc nghiệm bắt buộc phải chọn đáp án đúng (CorrectOptionId).");
            });
        }
    }
}
