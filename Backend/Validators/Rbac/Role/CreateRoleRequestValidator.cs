using Backend.DTOs.Rbac;
using FluentValidation;

namespace Backend.Validators.Role
{
    public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
    {
        public CreateRoleRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên vai trò không được để trống")
                .MaximumLength(50).WithMessage("Tên vai trò tối đa 50 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Mô tả tối đa 255 ký tự");
        }
    }
}
