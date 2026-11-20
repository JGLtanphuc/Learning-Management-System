using Backend.DTOs.Allocation;
using FluentValidation;

namespace Backend.Validators.Allocation
{
    public class CreateAllocationRequestValidator : AbstractValidator<CreateAllocationRequest>
    {
        public CreateAllocationRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("Vui lòng chọn học viên cần phân bổ");

            RuleFor(x => x.TargetUnit)
                .NotEmpty().WithMessage("Đơn vị tiếp nhận (BU) không được để trống")
                .Length(2, 100).WithMessage("Tên đơn vị tiếp nhận phải từ 2 đến 100 ký tự");

            RuleFor(x => x.ProjectName)
                .MaximumLength(100).WithMessage("Tên dự án tối đa 100 ký tự");

            RuleFor(x => x.RoleTitle)
                .NotEmpty().WithMessage("Vị trí tiếp nhận không được để trống")
                .MaximumLength(100).WithMessage("Vị trí tiếp nhận tối đa 100 ký tự");

            RuleFor(x => x.MatchScore)
                .InclusiveBetween(0.0m, 100.0m).When(x => x.MatchScore.HasValue)
                .WithMessage("Tỷ lệ phù hợp kỹ năng phải từ 0 đến 100%");

            RuleFor(x => x.Status)
                .Matches("^(PROPOSED|INTERVIEWING|ACCEPTED|REJECTED|TRANSFERRED|ALLOCATED)$")
                .WithMessage("Trạng thái phân bổ phải là PROPOSED, INTERVIEWING, ACCEPTED, REJECTED, TRANSFERRED hoặc ALLOCATED");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự");
        }
    }
}
