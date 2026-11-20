using Backend.DTOs.Common;
using FluentValidation;
using System.IO;

namespace Backend.Validators.Common
{
    public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
    {
        public FileUploadRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("Vui lòng chọn file tải lên")
                .Must(file => file != null && file.Length > 0).WithMessage("File tải lên không có dữ liệu")
                .Must(file =>
                {
                    if (file == null) return true;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return ext == ".csv" || ext == ".xlsx" || ext == ".xls";
                }).WithMessage("Hệ thống chỉ chấp nhận định dạng file .csv hoặc .xlsx");
        }
    }
}
