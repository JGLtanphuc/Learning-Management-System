using Backend.DTOs.Menu;
using FluentValidation;

namespace Backend.Validators.Menu
{
    public class UpdateModuleGroupRequestValidator : AbstractValidator<UpdateModuleGroupRequest>
    {
        public UpdateModuleGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhóm phân hệ không được để trống")
                .MaximumLength(100).WithMessage("Tên nhóm tối đa 100 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Mô tả tối đa 255 ký tự");
        }
    }
}
