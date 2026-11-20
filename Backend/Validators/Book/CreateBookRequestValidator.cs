using Backend.DTOs.Book;
using FluentValidation;

namespace Backend.Validators.Book
{
    public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
    {
        public CreateBookRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sách không được để trống")
                .Length(1, 100).WithMessage("Tên sách phải từ 1 đến 100 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả sách tối đa 500 ký tự");
        }
    }
}
