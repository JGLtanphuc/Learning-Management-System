using Backend.DTOs.Rbac;
using FluentValidation;

namespace Backend.Validators.Permission
{
    public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
    {
        public UpdatePermissionRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên quyền không được để trống")
                .MaximumLength(100).WithMessage("Tên quyền tối đa 100 ký tự");

            RuleFor(x => x.Resource)
                .NotEmpty().WithMessage("Mã tài nguyên (Resource) không được để trống")
                .MaximumLength(50).WithMessage("Resource tối đa 50 ký tự");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Hành động (Action) không được để trống")
                .MaximumLength(50).WithMessage("Action tối đa 50 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Mô tả tối đa 255 ký tự");
        }
    }
}
