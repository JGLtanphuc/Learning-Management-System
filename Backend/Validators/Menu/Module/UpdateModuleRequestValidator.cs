using Backend.DTOs.Menu;
using FluentValidation;

namespace Backend.Validators.Menu
{
    public class UpdateModuleRequestValidator : AbstractValidator<UpdateModuleRequest>
    {
        public UpdateModuleRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề module không được để trống")
                .MaximumLength(100).WithMessage("Tiêu đề module tối đa 100 ký tự");

            RuleFor(x => x.Url)
                .MaximumLength(255).WithMessage("URL tối đa 255 ký tự");

            RuleFor(x => x.ModuleGroupId)
                .GreaterThan(0).WithMessage("Nhóm phân hệ không hợp lệ");
        }
    }
}
