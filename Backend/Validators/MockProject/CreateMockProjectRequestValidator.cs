using Backend.DTOs.MockProject;
using FluentValidation;

namespace Backend.Validators.MockProject
{
    public class CreateMockProjectRequestValidator : AbstractValidator<CreateMockProjectRequest>
    {
        public CreateMockProjectRequestValidator()
        {
            RuleFor(x => x.ProjectName)
                .NotEmpty().WithMessage("Tên đồ án Mock Project không được để trống")
                .Length(3, 100).WithMessage("Tên đồ án phải từ 3 đến 100 ký tự");

            RuleFor(x => x.MentorName)
                .MaximumLength(100).WithMessage("Tên Mentor tối đa 100 ký tự");

            RuleFor(x => x.TechStack)
                .MaximumLength(255).WithMessage("Tech Stack tối đa 255 ký tự");

            RuleFor(x => x.RepoUrl)
                .MaximumLength(500).WithMessage("Repo URL tối đa 500 ký tự");

            RuleFor(x => x.Status)
                .Matches("^(PLANNING|IN_PROGRESS|ACTIVE|REVIEW|COMPLETED|CANCELLED)$")
                .WithMessage("Trạng thái đồ án phải là PLANNING, IN_PROGRESS, ACTIVE, REVIEW, COMPLETED hoặc CANCELLED");

            RuleFor(x => x.FinalScore)
                .InclusiveBetween(0.0m, 10.0m).When(x => x.FinalScore.HasValue)
                .WithMessage("Điểm đánh giá đồ án phải từ 0 đến 10");
        }
    }
}
