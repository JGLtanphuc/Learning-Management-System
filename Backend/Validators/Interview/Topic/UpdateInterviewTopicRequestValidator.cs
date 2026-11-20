using Backend.DTOs.Interview;
using FluentValidation;

namespace Backend.Validators.Interview
{
    public class UpdateInterviewTopicRequestValidator : AbstractValidator<UpdateInterviewTopicRequest>
    {
        public UpdateInterviewTopicRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên mục phỏng vấn không được để trống.")
                .MaximumLength(150).WithMessage("Tên mục phỏng vấn tối đa 150 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.");
        }
    }
}
