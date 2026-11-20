using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Class;
using Backend.Data;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend.Services.Implementations
{
    public class ClassService : BaseService<TrainingClass, ClassResponseDto, CreateClassRequest, UpdateClassRequest>, IClassService
    {
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;

        public ClassService(IClassRepository classRepository, ICurrentUserService currentUserService) : base(classRepository)
        {
            _classRepository = classRepository;
            _currentUserService = currentUserService;
        }

        protected override ClassResponseDto MapToResponseDto(TrainingClass entity) => entity.ToDto();
        protected override TrainingClass MapToEntity(CreateClassRequest request) => request.ToEntity();
        protected override void UpdateEntity(TrainingClass entity, UpdateClassRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<ClassResponseDto>> GetPageAsync(ClassFilterDto filter)
        {
            var query = _classRepository.QueryWithStudents().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(c => c.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(c => c.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var statuses = filter.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (statuses.Length > 1)
                {
                    query = query.Where(c => statuses.Contains(c.Status));
                }
                else
                {
                    query = query.Where(c => c.Status == statuses[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.ProgramName))
            {
                query = query.Where(c => c.ProgramName != null && c.ProgramName.Contains(filter.ProgramName));
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                query = query.Where(c => c.Location != null && c.Location.Contains(filter.Location));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(c => c.EndDate <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(term) ||
                                         c.ClassName.ToLower().Contains(term) ||
                                         (c.ProgramName != null && c.ProgramName.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<ClassResponseDto>
            {
                Content = items.Select(c => c.ToDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<ClassResponseDto?> GetByIdAsync(long id)
        {
            var item = await _classRepository.QueryWithStudents()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return null;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue && item.TeacherId != _currentUserService.UserId.Value)
            {
                return null;
            }
            if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                if (!item.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)))
                {
                    return null;
                }
            }

            return item.ToDto();
        }

        public override async Task<ClassResponseDto> CreateAsync(CreateClassRequest request)
        {
            // Business rule: ClassCode uniqueness
            var existing = await _classRepository.GetByCodeAsync(request.ClassCode.Trim().ToUpper());
            if (existing != null)
            {
                throw new ConflictException($"Mã lớp '{request.ClassCode}' đã tồn tại trong hệ thống.");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
            {
                throw new BadRequestException("Ngày kết thúc khóa học phải sau hoặc bằng ngày bắt đầu.");
            }

            var entity = request.ToEntity();
            await _classRepository.AddAsync(entity);
            await _classRepository.SaveChangesAsync();
            return entity.ToResponseDto();
        }

        public override async Task<ClassResponseDto?> UpdateAsync(long id, UpdateClassRequest request)
        {
            var entity = await _classRepository.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return null;

            // Check if another class uses this code
            var existing = await _classRepository.GetByCodeAsync(request.ClassCode.Trim().ToUpper());
            if (existing != null && existing.Id != id)
            {
                throw new ConflictException($"Mã lớp \'{request.ClassCode}\' đã được sử dụng bởi lớp khác.");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
            {
                throw new BadRequestException("Ngày kết thúc khóa học phải sau hoặc bằng ngày bắt đầu.");
            }

            entity.UpdateEntity(request);
            await _classRepository.SaveChangesAsync();
            return entity.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _classRepository.Query().FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return false;

            _classRepository.Delete(entity);
            await _classRepository.SaveChangesAsync();
            return true;
        }

        private IQueryable<TrainingClass> GetScopedClassQuery()
        {
            var query = _classRepository.QueryWithStudents().AsNoTracking().AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(c => c.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(c => c.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)));
            }

            return query;
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await GetScopedClassQuery()
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("STT,Mã lớp,Tên lớp,Chương trình đào tạo,Trạng thái,Sĩ số hiện tại,Quy mô,Địa điểm,Phòng học,Ngày bắt đầu,Ngày kết thúc,Kích hoạt");
            int stt = 1;
            foreach (var c in items)
            {
                var startStr = c.StartDate.HasValue ? c.StartDate.Value.ToString("yyyy-MM-dd") : "";
                var endStr = c.EndDate.HasValue ? c.EndDate.Value.ToString("yyyy-MM-dd") : "";
                var activeStr = c.IsActive ? "Hoạt động" : "Tạm khóa";
                csv.AppendLine($"{stt++},\"{c.ClassCode}\",\"{c.ClassName}\",\"{c.ProgramName}\",{c.Status},{c.Students.Count},{c.Capacity},\"{c.Location}\",\"{c.Room}\",{startStr},{endStr},{activeStr}");
            }

            return CsvExportHelper.ToUtf8BomBytes(csv.ToString());
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await GetScopedClassQuery()
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh sách lớp học");

            ws.ShowGridLines = true;

            // 1. Tiêu đề Báo cáo (Banner Title)
            ws.Cell("A1").Value = "DANH SÁCH LỚP HỌC (TRAINING CLASSES)";
            ws.Range("A1:K1").Merge();
            ws.Row(1).Height = 36;
            var title = ws.Cell("A1").Style;
            title.Font.FontSize = 16;
            title.Font.Bold = true;
            title.Font.FontColor = XLColor.FromHtml("#1E3A8A");
            title.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            title.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");

            // 2. Thông tin phụ (Metadata / Timestamp)
            ws.Cell("A2").Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}  |  Tổng số lớp: {items.Count}";
            ws.Range("A2:K2").Merge();
            ws.Row(2).Height = 20;
            var subTitle = ws.Cell("A2").Style;
            subTitle.Font.FontSize = 10;
            subTitle.Font.Italic = true;
            subTitle.Font.FontColor = XLColor.FromHtml("#64748B");
            subTitle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subTitle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            subTitle.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");

            // Dòng trống cách biệt
            ws.Row(3).Height = 10;

            // 3. Tiêu đề Bảng (Header Row)
            var headers = new string[]
            {
                "STT",
                "Mã lớp",
                "Tên lớp học",
                "Chương trình đào tạo",
                "Trạng thái",
                "Sĩ số / Quy mô",
                "Địa điểm",
                "Phòng học",
                "Ngày bắt đầu",
                "Ngày kết thúc",
                "Kích hoạt"
            };

            int headerRow = 4;
            ws.Row(headerRow).Height = 28;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                var hStyle = cell.Style;
                hStyle.Font.Bold = true;
                hStyle.Font.FontSize = 11;
                hStyle.Font.FontColor = XLColor.White;
                hStyle.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
                hStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
                hStyle.Border.OutsideBorderColor = XLColor.FromHtml("#1E3A8A");
            }

            // 4. Đổ dữ liệu (Data Rows)
            int currentRow = 5;
            for (int idx = 0; idx < items.Count; idx++)
            {
                var c = items[idx];
                ws.Row(currentRow).Height = 22;

                var isEven = idx % 2 == 1;
                var rowBgColor = isEven ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                // STT
                var cellStt = ws.Cell(currentRow, 1);
                cellStt.Value = idx + 1;
                cellStt.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Mã lớp
                var cellCode = ws.Cell(currentRow, 2);
                cellCode.Value = c.ClassCode;
                cellCode.Style.Font.Bold = true;
                cellCode.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                cellCode.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Tên lớp
                var cellName = ws.Cell(currentRow, 3);
                cellName.Value = c.ClassName;
                cellName.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Chương trình
                var cellProgram = ws.Cell(currentRow, 4);
                cellProgram.Value = c.ProgramName ?? "-";
                cellProgram.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Trạng thái (Huy hiệu màu)
                var cellStatus = ws.Cell(currentRow, 5);
                cellStatus.Value = c.Status;
                cellStatus.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellStatus.Style.Font.Bold = true;
                switch (c.Status?.ToUpperInvariant())
                {
                    case "ONGOING":
                        cellStatus.Style.Font.FontColor = XLColor.FromHtml("#15803D");
                        cellStatus.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
                        break;
                    case "COMPLETED":
                        cellStatus.Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                        cellStatus.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                        break;
                    case "CANCELLED":
                        cellStatus.Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
                        cellStatus.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                        break;
                    default:
                        cellStatus.Style.Font.FontColor = XLColor.FromHtml("#475569");
                        cellStatus.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        break;
                }

                // Sĩ số / Quy mô
                var cellCapacity = ws.Cell(currentRow, 6);
                cellCapacity.Value = $"{c.Students.Count} / {c.Capacity}";
                cellCapacity.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Địa điểm
                var cellLocation = ws.Cell(currentRow, 7);
                cellLocation.Value = c.Location ?? "-";
                cellLocation.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Phòng học
                var cellRoom = ws.Cell(currentRow, 8);
                cellRoom.Value = c.Room ?? "-";
                cellRoom.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Ngày bắt đầu
                var cellStart = ws.Cell(currentRow, 9);
                cellStart.Value = c.StartDate.HasValue ? c.StartDate.Value.ToString("dd/MM/yyyy") : "-";
                cellStart.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Ngày kết thúc
                var cellEnd = ws.Cell(currentRow, 10);
                cellEnd.Value = c.EndDate.HasValue ? c.EndDate.Value.ToString("dd/MM/yyyy") : "-";
                cellEnd.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Kích hoạt
                var cellActive = ws.Cell(currentRow, 11);
                cellActive.Value = c.IsActive ? "Hoạt động" : "Tạm khóa";
                cellActive.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellActive.Style.Font.FontColor = c.IsActive ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");

                // Áp dụng định dạng chung cho ô
                for (int col = 1; col <= 11; col++)
                {
                    var cell = ws.Cell(currentRow, col);
                    if (col != 5)
                    {
                        cell.Style.Fill.BackgroundColor = rowBgColor;
                    }
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                }

                currentRow++;
            }

            // 5. Dòng Tổng kết (Footer Summary)
            ws.Row(currentRow).Height = 24;
            var summaryCell = ws.Cell(currentRow, 1);
            summaryCell.Value = $"TỔNG CỘNG: {items.Count} LỚP HỌC";
            ws.Range(currentRow, 1, currentRow, 11).Merge();
            var summaryStyle = summaryCell.Style;
            summaryStyle.Font.Bold = true;
            summaryStyle.Font.FontSize = 11;
            summaryStyle.Font.FontColor = XLColor.FromHtml("#1E293B");
            summaryStyle.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            summaryStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            summaryStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            for (int col = 1; col <= 11; col++)
            {
                ws.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(currentRow, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            }

            // 6. Căn chỉnh độ rộng cột tối ưu
            ws.Columns().AdjustToContents();
            if (ws.Column(1).Width < 8) ws.Column(1).Width = 8;
            if (ws.Column(2).Width < 22) ws.Column(2).Width = 22;
            if (ws.Column(3).Width < 35) ws.Column(3).Width = 35;
            if (ws.Column(4).Width < 28) ws.Column(4).Width = 28;
            if (ws.Column(5).Width < 18) ws.Column(5).Width = 18;
            if (ws.Column(6).Width < 18) ws.Column(6).Width = 18;
            if (ws.Column(7).Width < 20) ws.Column(7).Width = 20;
            if (ws.Column(8).Width < 14) ws.Column(8).Width = 14;
            if (ws.Column(9).Width < 16) ws.Column(9).Width = 16;
            if (ws.Column(10).Width < 16) ws.Column(10).Width = 16;
            if (ws.Column(11).Width < 15) ws.Column(11).Width = 15;

            // Đóng băng tiêu đề cố định dòng 4 khi cuộn
            ws.SheetView.FreezeRows(4);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportPdfAsync()
        {
            var items = await GetScopedClassQuery()
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI", "Arial", "Roboto", "Helvetica"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("FAMS - HỆ THỐNG QUẢN LÝ ĐÀO TẠO")
                                    .FontSize(11).Bold().FontColor("#1E40AF");
                                titleCol.Item().Text("DANH SÁCH LỚP HỌC (TRAINING CLASSES)")
                                    .FontSize(16).Bold().FontColor("#0F172A");
                            });

                            row.ConstantItem(220).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                                metaCol.Item().Text($"Tổng số lớp học: {items.Count}").FontSize(9).Bold().FontColor("#1E293B");
                            });
                        });

                        col.Item().PaddingTop(6).PaddingBottom(6).LineHorizontal(1).LineColor("#CBD5E1");
                    });

                    page.Content().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);   // STT
                            columns.RelativeColumn(1.4f); // Mã lớp
                            columns.RelativeColumn(2.4f); // Tên lớp
                            columns.RelativeColumn(2.0f); // Chương trình
                            columns.RelativeColumn(1.4f); // Trạng thái
                            columns.RelativeColumn(1.1f); // Sĩ số
                            columns.RelativeColumn(1.4f); // Địa điểm
                            columns.ConstantColumn(55);   // Phòng
                            columns.ConstantColumn(68);   // Bắt đầu
                            columns.ConstantColumn(68);   // Kết thúc
                            columns.ConstantColumn(58);   // Kích hoạt
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("STT").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Mã lớp").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignLeft().Text("Tên lớp học").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignLeft().Text("Chương trình").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Trạng thái").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Sĩ số").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignLeft().Text("Địa điểm").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Phòng").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Bắt đầu").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Kết thúc").Bold().FontColor("#FFFFFF");
                            header.Cell().Background("#1E40AF").Padding(5).AlignCenter().Text("Kích hoạt").Bold().FontColor("#FFFFFF");
                        });

                        int index = 1;
                        foreach (var c in items)
                        {
                            var isEven = index % 2 == 0;
                            var bg = isEven ? "#F8FAFC" : "#FFFFFF";

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(index.ToString());
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(c.ClassCode).Bold();
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignLeft().Text(c.ClassName);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignLeft().Text(c.ProgramName ?? "-");

                            var statusColor = c.Status?.ToUpperInvariant() switch
                            {
                                "ONGOING" => "#15803D",
                                "COMPLETED" => "#1D4ED8",
                                "CANCELLED" => "#B91C1C",
                                _ => "#475569"
                            };
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(c.Status ?? "-").Bold().FontColor(statusColor);

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text($"{c.Students.Count}/{c.Capacity}");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignLeft().Text(c.Location ?? "-");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(c.Room ?? "-");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(c.StartDate.HasValue ? c.StartDate.Value.ToString("dd/MM/yyyy") : "-");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(c.EndDate.HasValue ? c.EndDate.Value.ToString("dd/MM/yyyy") : "-");

                            var activeText = c.IsActive ? "Hoạt động" : "Tạm khóa";
                            var activeColor = c.IsActive ? "#16A34A" : "#DC2626";
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignCenter().Text(activeText).FontColor(activeColor);

                            index++;
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("FAMS Training System - Báo cáo bảo mật nội bộ").FontSize(8).FontColor("#94A3B8");
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Trang ").FontSize(8).FontColor("#64748B");
                            text.CurrentPageNumber().FontSize(8).Bold().FontColor("#1E293B");
                            text.Span(" / ").FontSize(8).FontColor("#64748B");
                            text.TotalPages().FontSize(8).Bold().FontColor("#1E293B");
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}

