using System.Text;

﻿using System;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.IO;

using System.Linq;

using System.Text.RegularExpressions;

using System.Threading.Tasks;

using Backend.DTOs.Common;

using Backend.DTOs.Class;

using Backend.DTOs.Student;

using Backend.DTOs.Attendance;

using Backend.DTOs.Allocation;

using Backend.DTOs.MockProject;

using Backend.Data;

using Backend.Entities;

using Backend.Mappers;

using Backend.Services.Interfaces;

using ClosedXML.Excel;

using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;



namespace Backend.Services.Implementations

{

    public class ImportExportService : IImportExportService

    {

        private readonly AppDbContext _context;



        public ImportExportService(AppDbContext context)

        {

            _context = context;

        }







        #region Helper Methods

        private static string GetCellStr(IXLRangeRow row, int col)

        {

            if (col <= 0) return string.Empty;

            var cell = row.Cell(col);

            if (cell == null || cell.IsEmpty()) return string.Empty;

            return cell.GetString()?.Trim() ?? string.Empty;

        }



        private static bool IsRowCompletelyEmpty(IXLRangeRow row, int maxCol = 10)

        {

            for (int i = 1; i <= maxCol; i++)

            {

                if (!string.IsNullOrWhiteSpace(GetCellStr(row, i))) return false;

            }

            return true;

        }



        private static bool ValidateFile(IFormFile? file, out ImportErrorDto? error)

        {

            error = null;

            if (file == null || file.Length == 0)

            {

                error = new ImportErrorDto { RowNumber = 0, Message = "Vui lòng chọn file Excel (.xlsx) hoặc CSV (.csv) để tải lên." };

                return false;

            }



            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".xlsx" && ext != ".xls" && ext != ".csv")

            {

                error = new ImportErrorDto { RowNumber = 0, Message = "Chỉ chấp nhận file định dạng Excel (.xlsx) hoặc CSV (.csv)." };

                return false;

            }



            if (file.Length > 50 * 1024 * 1024)

            {

                error = new ImportErrorDto { RowNumber = 0, Message = "Dung lượng file không được vượt quá 50MB." };

                return false;

            }



            return true;

        }



        private static List<List<string>> ParseCsv(Stream stream)

        {

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            var rows = new List<List<string>>();

            var row = new List<string>();

            var sb = new StringBuilder();

            bool inQuotes = false;

            int ch;



            while ((ch = reader.Read()) != -1)

            {

                char c = (char)ch;

                if (c == '"')

                {

                    if (inQuotes && reader.Peek() == '"')

                    {

                        reader.Read();

                        sb.Append('"');

                    }

                    else

                    {

                        inQuotes = !inQuotes;

                    }

                }

                else if (c == ',' && !inQuotes)

                {

                    row.Add(sb.ToString().Trim());

                    sb.Clear();

                }

                else if ((c == '\r' || c == '\n') && !inQuotes)

                {

                    if (c == '\r' && reader.Peek() == '\n') reader.Read();

                    row.Add(sb.ToString().Trim());

                    sb.Clear();

                    if (row.Any(cell => !string.IsNullOrEmpty(cell)))

                    {

                        rows.Add(row);

                    }

                    row = new List<string>();

                }

                else

                {

                    sb.Append(c);

                }

            }

            if (sb.Length > 0 || row.Any())

            {

                row.Add(sb.ToString().Trim());

                if (row.Any(cell => !string.IsNullOrEmpty(cell)))

                {

                    rows.Add(row);

                }

            }

            return rows;

        }



        private static XLWorkbook? LoadWorkbook(IFormFile file, out ImportErrorDto? error)

        {

            error = null;

            var ext = Path.GetExtension(file.FileName).ToLower();

            try

            {

                using var stream = file.OpenReadStream();

                if (ext == ".csv")

                {

                    var rows = ParseCsv(stream);

                    if (!rows.Any())

                    {

                        error = new ImportErrorDto { RowNumber = 0, Message = "File CSV không có bất kỳ dòng dữ liệu nào." };

                        return null;

                    }

                    var wb = new XLWorkbook();

                    var ws = wb.Worksheets.Add("Data");

                    for (int r = 0; r < rows.Count; r++)

                    {

                        for (int c = 0; c < rows[r].Count; c++)

                        {

                            ws.Cell(r + 1, c + 1).Value = rows[r][c];

                        }

                    }

                    return wb;

                }

                else

                {

                    return new XLWorkbook(stream);

                }

            }

            catch (Exception ex)

            {

                error = new ImportErrorDto

                {

                    RowNumber = 0,

                    Message = $"Không thể đọc file: {ex.Message}. Vui lòng kiểm tra định dạng .xlsx hoặc .csv."

                };

                return null;

            }

        }

        #endregion



        #region 2. Import Students

        public async Task<ImportResultDto> ImportStudentsAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "studentCode",

                "fullName",

                "email",

                "phone",

                "className",

                "university",

                "major",

                "gpa",

                "status"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "studentCode", "fullName", "email", "phone" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colCode = recognizedCols["studentCode"];

            int colName = recognizedCols["fullName"];

            int colEmail = recognizedCols["email"];

            int colPhone = recognizedCols["phone"];

            int colClass = recognizedCols.TryGetValue("className", out var cc) ? cc : -1;

            int colUni = recognizedCols.TryGetValue("university", out var cu) ? cu : -1;

            int colMajor = recognizedCols.TryGetValue("major", out var cm) ? cm : -1;

            int colGpa = recognizedCols.TryGetValue("gpa", out var cg) ? cg : -1;

            int colStatus = recognizedCols.TryGetValue("status", out var cs) ? cs : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<Student>();



            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);



            var dbCodes = (await _context.Students.Select(s => s.StudentCode).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var dbEmails = (await _context.Students.Select(s => s.Email).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var classes = await _context.Classes.ToDictionaryAsync(c => c.ClassCode.Trim().ToUpper(), c => c.Id);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 9))) continue;



                var code = GetCellStr(row, colCode);

                var name = GetCellStr(row, colName);

                var email = GetCellStr(row, colEmail);

                var phone = GetCellStr(row, colPhone);

                var className = GetCellStr(row, colClass);

                var university = GetCellStr(row, colUni);

                var major = GetCellStr(row, colMajor);

                var gpaStr = GetCellStr(row, colGpa);

                var status = GetCellStr(row, colStatus);



                var rowHasError = false;



                // 1. Phân tích điểm GPA

                decimal? gpa = null;

                if (!string.IsNullOrWhiteSpace(gpaStr))

                {

                    if (decimal.TryParse(gpaStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedGpa))

                    {

                        if (parsedGpa < 0 || parsedGpa > 10)

                        {

                            errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Điểm GPA '{gpaStr}' phải nằm trong khoảng từ 0.0 đến 10.0." });

                            rowHasError = true;

                        }

                        else

                        {

                            gpa = parsedGpa;

                        }

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Điểm GPA '{gpaStr}' không phải là số hợp lệ." });

                        rowHasError = true;

                    }

                }



                // 2. Tra cứu mã lớp học

                long? classId = null;

                if (!string.IsNullOrWhiteSpace(className))

                {

                    if (classes.TryGetValue(className.ToUpper(), out var foundId))

                    {

                        classId = foundId;

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã lớp '{className}' không tồn tại trong hệ thống." });

                        rowHasError = true;

                    }

                }



                // 3. Đóng gói DTO và Validate

                var studentDto = new CreateStudentRequest

                {

                    StudentCode = code,

                    FullName = name,

                    Email = email,

                    Phone = phone,

                    ClassId = classId,

                    University = string.IsNullOrWhiteSpace(university) ? null : university.Trim(),

                    Major = string.IsNullOrWhiteSpace(major) ? null : major.Trim(),

                    Gpa = gpa,

                    Status = string.IsNullOrWhiteSpace(status) ? "ACTIVE" : status.Trim().ToUpper(),

                    JoinedAt = DateTime.UtcNow,

                    IsActive = true

                };



                var validationContext = new ValidationContext(studentDto);

                var validationResults = new List<ValidationResult>();



                if (!Validator.TryValidateObject(studentDto, validationContext, validationResults, validateAllProperties: true))

                {

                    foreach (var valErr in validationResults)

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = valErr.ErrorMessage ?? "Dữ liệu học viên không hợp lệ." });

                    }

                    rowHasError = true;

                }



                // 4. Kiểm tra trùng mã học viên & email

                if (!string.IsNullOrWhiteSpace(code))

                {

                    if (seenCodes.Contains(code))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã học viên '{code}' bị trùng lặp trong file Excel." });

                        rowHasError = true;

                    }

                    else if (dbCodes.Contains(code))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã học viên '{code}' đã tồn tại trong hệ thống." });

                        rowHasError = true;

                    }

                }



                if (!string.IsNullOrWhiteSpace(email))

                {

                    if (seenEmails.Contains(email))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Email '{email}' bị trùng lặp trong file Excel." });

                        rowHasError = true;

                    }

                    else if (dbEmails.Contains(email))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Email '{email}' đã được đăng ký bởi học viên khác." });

                        rowHasError = true;

                    }

                }



                if (!rowHasError)

                {

                    seenCodes.Add(code);

                    seenEmails.Add(email);

                    dbCodes.Add(code);

                    dbEmails.Add(email);



                    validEntities.Add(studentDto.ToEntity());

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Students.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 3. Import Classes

        public async Task<ImportResultDto> ImportClassesAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "classCode",

                "className",

                "programName",

                "startDate",

                "endDate",

                "capacity",

                "location",

                "room",

                "status"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                // Nếu ô tiêu đề bị bỏ trống

                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                // Nếu tiêu đề bị đổi chữ, gõ sai, hoặc không có trong file mẫu FE

                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            // Nếu có bất kỳ cột nào sai định dạng tiêu đề, dừng ngay và báo lỗi

            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu (classCode, className, capacity)

            var requiredColumns = new[] { "classCode", "className", "capacity" };



            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // Lấy vị trí các cột đã được định vị chính xác theo file mẫu

            int colCode = recognizedCols["classCode"];

            int colName = recognizedCols["className"];

            int colCapacity = recognizedCols["capacity"];

            int colProgram = recognizedCols.TryGetValue("programName", out var cp) ? cp : -1;

            int colStart = recognizedCols.TryGetValue("startDate", out var cs) ? cs : -1;

            int colEnd = recognizedCols.TryGetValue("endDate", out var ce) ? ce : -1;

            int colLocation = recognizedCols.TryGetValue("location", out var cl) ? cl : -1;

            int colRoom = recognizedCols.TryGetValue("room", out var cr) ? cr : -1;

            int colStatus = recognizedCols.TryGetValue("status", out var cst) ? cst : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<TrainingClass>();



            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dbCodes = (await _context.Classes.Select(c => c.ClassCode).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 9))) continue;



                var code = GetCellStr(row, colCode);

                var name = GetCellStr(row, colName);

                var program = GetCellStr(row, colProgram);

                var capacityStr = GetCellStr(row, colCapacity);

                var location = GetCellStr(row, colLocation);

                var room = GetCellStr(row, colRoom);

                var status = GetCellStr(row, colStatus);

                var startCell = colStart > 0 ? row.Cell(colStart) : null;

                var endCell = colEnd > 0 ? row.Cell(colEnd) : null;



                var rowHasError = false;



                // 1. Phân tích kiểu số Sĩ số (Capacity) từ Excel

                int? capacity = null;

                if (!string.IsNullOrWhiteSpace(capacityStr))

                {

                    if (int.TryParse(capacityStr, out var parsedCap))

                    {

                        capacity = parsedCap;

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = "Sĩ số phải là số nguyên hợp lệ." });

                        rowHasError = true;

                    }

                }



                // 2. Phân tích kiểu ngày tháng (StartDate & EndDate) từ Excel

                DateTime? startDate = null;

                DateTime? endDate = null;



                if (startCell != null && !startCell.IsEmpty())

                {

                    if (startCell.DataType == XLDataType.DateTime)

                    {

                        startDate = startCell.GetDateTime();

                    }

                    else

                    {

                        var raw = startCell.GetString()?.Trim();

                        if (!string.IsNullOrWhiteSpace(raw))

                        {

                            var normalized = Regex.Replace(raw, @"[\s/\\]+", "-");

                            if (DateTime.TryParse(normalized, out var sDate)) startDate = sDate;

                            else if (DateTime.TryParseExact(normalized, new[] { "yyyy-MM-dd", "yyyy-M-d", "dd-MM-yyyy", "d-M-yyyy" },

                                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var sDateExact))

                            {

                                startDate = sDateExact;

                            }

                            else

                            {

                                errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Ngày bắt đầu '{raw}' không đúng định dạng (yyyy-MM-dd)." });

                                rowHasError = true;

                            }

                        }

                    }

                }



                if (endCell != null && !endCell.IsEmpty())

                {

                    if (endCell.DataType == XLDataType.DateTime)

                    {

                        endDate = endCell.GetDateTime();

                    }

                    else

                    {

                        var raw = endCell.GetString()?.Trim();

                        if (!string.IsNullOrWhiteSpace(raw))

                        {

                            var normalized = Regex.Replace(raw, @"[\s/\\]+", "-");

                            if (DateTime.TryParse(normalized, out var eDate)) endDate = eDate;

                            else if (DateTime.TryParseExact(normalized, new[] { "yyyy-MM-dd", "yyyy-M-d", "dd-MM-yyyy", "d-M-yyyy" },

                                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var eDateExact))

                            {

                                endDate = eDateExact;

                            }

                            else

                            {

                                errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Ngày kết thúc '{raw}' không đúng định dạng (yyyy-MM-dd)." });

                                rowHasError = true;

                            }

                        }

                    }

                }



                // 3. Chuẩn hóa Status

                var upperStatus = string.IsNullOrWhiteSpace(status) ? "PLANNING" : status.Trim().ToUpper().Replace(" ", "_");

                if (upperStatus == "OPENFORREGISTRATION") upperStatus = "OPEN_FOR_REGISTRATION";



                // 4. Khởi tạo DTO CreateClassRequest từ dữ liệu Excel

                var dto = new CreateClassRequest

                {

                    ClassCode = code,

                    ClassName = name,

                    ProgramName = string.IsNullOrWhiteSpace(program) ? null : program.Trim(),

                    Capacity = capacity,

                    Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),

                    Room = string.IsNullOrWhiteSpace(room) ? null : room.Trim(),

                    StartDate = startDate,

                    EndDate = endDate,

                    Status = upperStatus,

                    IsActive = true

                };



                // 5. Tự động kiểm tra tính hợp lệ qua DTO (DataAnnotations & IValidatableObject)

                var validationResults = new List<ValidationResult>();

                var validationContext = new ValidationContext(dto);

                bool isValidDto = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);



                if (!isValidDto)

                {

                    foreach (var valResult in validationResults)

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = valResult.ErrorMessage ?? "Dữ liệu không hợp lệ." });

                    }

                    rowHasError = true;

                }



                // 6. Kiểm tra quy tắc nghiệp vụ Database (trùng mã lớp)

                if (!string.IsNullOrWhiteSpace(dto.ClassCode))

                {

                    if (seenCodes.Contains(dto.ClassCode))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã lớp '{dto.ClassCode}' bị trùng lặp trong chính file Excel." });

                        rowHasError = true;

                    }

                    else if (dbCodes.Contains(dto.ClassCode))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã lớp '{dto.ClassCode}' đã tồn tại trong hệ thống." });

                        rowHasError = true;

                    }

                }



                if (!rowHasError)

                {

                    seenCodes.Add(dto.ClassCode);

                    dbCodes.Add(dto.ClassCode);



                    // 7. Chuyển đổi DTO sang Entity bằng ClassMapper

                    validEntities.Add(dto.ToEntity());

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Classes.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 4. Import Attendances

        public async Task<ImportResultDto> ImportAttendancesAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "studentCode",

                "studentName",

                "className",

                "attendanceDate",

                "slot",

                "status",

                "note"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "studentCode", "className", "attendanceDate", "slot", "status" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colStudentCode = recognizedCols["studentCode"];

            int colClassName = recognizedCols["className"];

            int colDate = recognizedCols["attendanceDate"];

            int colSlot = recognizedCols["slot"];

            int colStatus = recognizedCols["status"];

            int colNote = recognizedCols.TryGetValue("note", out var cn) ? cn : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<Attendance>();



            var students = await _context.Students.ToDictionaryAsync(s => s.StudentCode.Trim().ToUpper(), s => s.Id);

            var classes = await _context.Classes.ToDictionaryAsync(c => c.ClassCode.Trim().ToUpper(), c => c.Id);

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 7))) continue;



                var studentCode = GetCellStr(row, colStudentCode);

                var classCode = GetCellStr(row, colClassName);

                var slot = GetCellStr(row, colSlot);

                var status = GetCellStr(row, colStatus);

                var note = GetCellStr(row, colNote);

                var dateCell = row.Cell(colDate);



                var rowHasError = false;



                // 1. Tra cứu học viên & lớp học

                long studentId = 0;

                if (string.IsNullOrWhiteSpace(studentCode))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Mã học viên (studentCode) không được để trống." });

                    rowHasError = true;

                }

                else if (!students.TryGetValue(studentCode.ToUpper(), out studentId))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã học viên '{studentCode}' không tồn tại trong hệ thống." });

                    rowHasError = true;

                }



                long classId = 0;

                if (string.IsNullOrWhiteSpace(classCode))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Mã lớp học (className) không được để trống." });

                    rowHasError = true;

                }

                else if (!classes.TryGetValue(classCode.ToUpper(), out classId))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã lớp '{classCode}' không tồn tại trong hệ thống." });

                    rowHasError = true;

                }



                // 2. Parse ngày điểm danh

                DateTime attendanceDate = DateTime.MinValue;

                if (dateCell == null || dateCell.IsEmpty())

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Ngày điểm danh (attendanceDate) không được để trống." });

                    rowHasError = true;

                }

                else if (dateCell.DataType == XLDataType.DateTime)

                {

                    attendanceDate = dateCell.GetDateTime().Date;

                }

                else if (DateTime.TryParse(dateCell.GetString()?.Trim(), out var parsedDate))

                {

                    attendanceDate = parsedDate.Date;

                }

                else

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Ngày điểm danh '{dateCell.GetString()}' không đúng định dạng ngày (yyyy-MM-dd)." });

                    rowHasError = true;

                }



                // 3. Đóng gói DTO và Validate

                var upperSlot = slot.Trim().ToUpper();

                var upperStatus = status.Trim().ToUpper();



                var dto = new CreateAttendanceRequest

                {

                    StudentId = studentId,

                    ClassId = classId,

                    AttendanceDate = attendanceDate,

                    Slot = upperSlot,

                    Status = upperStatus,

                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),

                    IsActive = true

                };



                var validationContext = new ValidationContext(dto);

                var validationResults = new List<ValidationResult>();



                if (!Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true))

                {

                    foreach (var valResult in validationResults)

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = valResult.ErrorMessage ?? "Dữ liệu điểm danh không hợp lệ." });

                    }

                    rowHasError = true;

                }



                // 4. Kiểm tra trùng lịch điểm danh

                if (!rowHasError)

                {

                    var key = $"{studentId}_{classId}_{attendanceDate:yyyyMMdd}_{upperSlot}";

                    if (seenKeys.Contains(key))

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Bản ghi điểm danh cho học viên '{studentCode}' ca {upperSlot} ngày {attendanceDate:dd/MM/yyyy} bị trùng trong file Excel." });

                        rowHasError = true;

                    }

                    else

                    {

                        var existsInDb = await _context.Attendances.AnyAsync(a => a.StudentId == studentId && a.ClassId == classId && a.AttendanceDate.Date == attendanceDate && a.Slot == upperSlot);

                        if (existsInDb)

                        {

                            errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Đã có bản ghi điểm danh cho học viên '{studentCode}' ca {upperSlot} ngày {attendanceDate:dd/MM/yyyy} trong hệ thống." });

                            rowHasError = true;

                        }

                    }



                    if (!rowHasError)

                    {

                        seenKeys.Add(key);

                        validEntities.Add(dto.ToEntity());

                    }

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Attendances.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 5. Import Allocations

        public async Task<ImportResultDto> ImportAllocationsAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "studentCode",

                "studentName",

                "targetUnit",

                "projectName",

                "roleTitle",

                "matchScore",

                "status",

                "interviewDate",

                "note"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "studentCode", "targetUnit", "roleTitle" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colStudentCode = recognizedCols["studentCode"];

            int colTargetUnit = recognizedCols["targetUnit"];

            int colRoleTitle = recognizedCols["roleTitle"];

            int colProjectName = recognizedCols.TryGetValue("projectName", out var cpn) ? cpn : -1;

            int colScore = recognizedCols.TryGetValue("matchScore", out var csc) ? csc : -1;

            int colStatus = recognizedCols.TryGetValue("status", out var cst) ? cst : -1;

            int colInterviewDate = recognizedCols.TryGetValue("interviewDate", out var cid) ? cid : -1;

            int colNote = recognizedCols.TryGetValue("note", out var cn) ? cn : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<Allocation>();

            var students = await _context.Students.ToDictionaryAsync(s => s.StudentCode.Trim().ToUpper(), s => s.Id);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 9))) continue;



                var studentCode = GetCellStr(row, colStudentCode);

                var targetUnit = GetCellStr(row, colTargetUnit);

                var roleTitle = GetCellStr(row, colRoleTitle);

                var projectName = GetCellStr(row, colProjectName);

                var scoreStr = GetCellStr(row, colScore);

                var status = GetCellStr(row, colStatus);

                var note = GetCellStr(row, colNote);

                var interviewCell = colInterviewDate > 0 ? row.Cell(colInterviewDate) : null;



                var rowHasError = false;



                // 1. Tra cứu học viên

                long studentId = 0;

                if (string.IsNullOrWhiteSpace(studentCode))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Mã học viên (studentCode) không được để trống." });

                    rowHasError = true;

                }

                else if (!students.TryGetValue(studentCode.ToUpper(), out studentId))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Mã học viên '{studentCode}' không tồn tại trong hệ thống." });

                    rowHasError = true;

                }



                // 2. Điểm đánh giá phù hợp

                decimal? matchScore = null;

                if (!string.IsNullOrWhiteSpace(scoreStr))

                {

                    if (decimal.TryParse(scoreStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sc))

                    {

                        if (sc < 0 || sc > 100)

                        {

                            errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Tỷ lệ phù hợp '{scoreStr}' phải từ 0 đến 100%." });

                            rowHasError = true;

                        }

                        else

                        {

                            matchScore = sc;

                        }

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Tỷ lệ phù hợp '{scoreStr}' không phải là số hợp lệ." });

                        rowHasError = true;

                    }

                }



                // 3. Ngày phỏng vấn

                DateTime? interviewDate = null;

                if (interviewCell != null && !interviewCell.IsEmpty())

                {

                    if (interviewCell.DataType == XLDataType.DateTime)

                    {

                        interviewDate = interviewCell.GetDateTime();

                    }

                    else if (DateTime.TryParse(interviewCell.GetString()?.Trim(), out var parsedDate))

                    {

                        interviewDate = parsedDate;

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Ngày phỏng vấn '{interviewCell.GetString()}' không đúng định dạng ngày (yyyy-MM-dd)." });

                        rowHasError = true;

                    }

                }



                // 4. Trạng thái phân bổ

                var finalStatus = string.IsNullOrWhiteSpace(status) ? "PROPOSED" : status.Trim().ToUpper();



                // 5. Đóng gói DTO và Validate

                var dto = new CreateAllocationRequest

                {

                    StudentId = studentId,

                    TargetUnit = targetUnit,

                    ProjectName = string.IsNullOrWhiteSpace(projectName) ? null : projectName.Trim(),

                    RoleTitle = string.IsNullOrWhiteSpace(roleTitle) ? "Fresher Developer" : roleTitle.Trim(),

                    MatchScore = matchScore,

                    Status = finalStatus,

                    InterviewDate = interviewDate,

                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),

                    IsActive = true

                };



                var validationContext = new ValidationContext(dto);

                var validationResults = new List<ValidationResult>();



                if (!Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true))

                {

                    foreach (var valResult in validationResults)

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = valResult.ErrorMessage ?? "Dữ liệu phân bổ không hợp lệ." });

                    }

                    rowHasError = true;

                }



                if (!rowHasError)

                {

                    validEntities.Add(dto.ToEntity());

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Allocations.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 6. Import MockProjects

        public async Task<ImportResultDto> ImportMockProjectsAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "projectName",

                "studentCodes",

                "mentorName",

                "techStack",

                "repoUrl",

                "status",

                "finalScore"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "projectName" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colProjectName = recognizedCols["projectName"];

            int colStudentCodes = recognizedCols.TryGetValue("studentCodes", out var csc) ? csc : -1;

            int colMentor = recognizedCols.TryGetValue("mentorName", out var cm) ? cm : -1;

            int colTech = recognizedCols.TryGetValue("techStack", out var ct) ? ct : -1;

            int colRepo = recognizedCols.TryGetValue("repoUrl", out var cr) ? cr : -1;

            int colStatus = recognizedCols.TryGetValue("status", out var cst) ? cst : -1;

            int colScore = recognizedCols.TryGetValue("finalScore", out var cfs) ? cfs : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<(MockProject Project, List<string> StudentCodes)>();

            var studentsMap = await _context.Students.ToDictionaryAsync(s => s.StudentCode.Trim().ToUpper(), s => s);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 7))) continue;



                var name = GetCellStr(row, colProjectName);

                var studentCodesStr = GetCellStr(row, colStudentCodes);

                var mentor = GetCellStr(row, colMentor);

                var tech = GetCellStr(row, colTech);

                var repo = GetCellStr(row, colRepo);

                var status = GetCellStr(row, colStatus);

                var scoreStr = GetCellStr(row, colScore);



                var rowHasError = false;



                // 1. Phân tích điểm

                decimal? score = null;

                if (!string.IsNullOrWhiteSpace(scoreStr))

                {

                    if (decimal.TryParse(scoreStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sc))

                    {

                        if (sc < 0 || sc > 10)

                        {

                            errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Điểm đánh giá '{scoreStr}' phải từ 0.0 đến 10.0." });

                            rowHasError = true;

                        }

                        else

                        {

                            score = sc;

                        }

                    }

                    else

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Điểm đánh giá '{scoreStr}' không phải là số hợp lệ." });

                        rowHasError = true;

                    }

                }



                // 2. Trạng thái đồ án

                var finalStatus = string.IsNullOrWhiteSpace(status) ? "PLANNING" : status.Trim().ToUpper();



                // 3. Đóng gói DTO và Validate

                var dto = new CreateMockProjectRequest

                {

                    ProjectName = name,

                    MentorName = string.IsNullOrWhiteSpace(mentor) ? null : mentor.Trim(),

                    TechStack = string.IsNullOrWhiteSpace(tech) ? null : tech.Trim(),

                    RepoUrl = string.IsNullOrWhiteSpace(repo) ? null : repo.Trim(),

                    Status = finalStatus,

                    FinalScore = score,

                    IsActive = true

                };



                var validationContext = new ValidationContext(dto);

                var validationResults = new List<ValidationResult>();



                if (!Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true))

                {

                    foreach (var valResult in validationResults)

                    {

                        errors.Add(new ImportErrorDto { RowNumber = r, Message = valResult.ErrorMessage ?? "Dữ liệu đồ án không hợp lệ." });

                    }

                    rowHasError = true;

                }



                // 4. Danh sách mã học viên tham gia

                var studentCodesList = new List<string>();

                if (!string.IsNullOrWhiteSpace(studentCodesStr))

                {

                    var splitCodes = studentCodesStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)

                                                    .Select(s => s.Trim())

                                                    .Where(s => !string.IsNullOrEmpty(s))

                                                    .ToList();

                    studentCodesList.AddRange(splitCodes);

                }



                if (!rowHasError)

                {

                    validEntities.Add((dto.ToEntity(), studentCodesList));

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    foreach (var (project, studentCodesList) in validEntities)

                    {

                        await _context.MockProjects.AddAsync(project);

                        await _context.SaveChangesAsync();



                        // Liên kết học viên nếu có

                        foreach (var code in studentCodesList)

                        {

                            if (studentsMap.TryGetValue(code.ToUpper(), out var student))

                            {

                                student.MockProjectId = project.Id;

                            }

                        }

                    }

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 7. Import Users

        public async Task<ImportResultDto> ImportUsersAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "email",

                "firstName",

                "lastName",

                "role",

                "password"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "email", "firstName", "lastName" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colEmail = recognizedCols["email"];

            int colFirstName = recognizedCols["firstName"];

            int colLastName = recognizedCols["lastName"];

            int colRole = recognizedCols.TryGetValue("role", out var cr) ? cr : -1;

            int colPass = recognizedCols.TryGetValue("password", out var cp) ? cp : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<User>();



            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dbEmails = (await _context.Users.Select(u => u.Email).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var roles = await _context.Roles.ToDictionaryAsync(r => r.Name.Trim().ToUpper(), r => r);



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 5))) continue;



                var email = GetCellStr(row, colEmail);

                var firstName = GetCellStr(row, colFirstName);

                var lastName = GetCellStr(row, colLastName);

                var roleName = GetCellStr(row, colRole);

                var password = GetCellStr(row, colPass);



                var rowHasError = false;



                // 1. Email validate

                if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Email '{email}' không đúng định dạng." });

                    rowHasError = true;

                }

                else if (seenEmails.Contains(email) || dbEmails.Contains(email))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Email '{email}' đã tồn tại trong hệ thống." });

                    rowHasError = true;

                }



                // 2. Họ và tên

                if (string.IsNullOrWhiteSpace(firstName))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Họ (firstName) không được để trống." });

                    rowHasError = true;

                }



                if (string.IsNullOrWhiteSpace(lastName))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên (lastName) không được để trống." });

                    rowHasError = true;

                }



                // 3. Mật khẩu (mặc định nếu không nhập)

                var rawPass = string.IsNullOrWhiteSpace(password) ? "Password@123" : password.Trim();

                if (rawPass.Length < 6)

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Mật khẩu phải có tối thiểu 6 ký tự." });

                    rowHasError = true;

                }



                // 4. Role

                Role? assignedRole = null;

                if (!string.IsNullOrWhiteSpace(roleName))

                {

                    var normalizedRoleName = roleName.Trim().ToUpper();

                    if (roles.TryGetValue(normalizedRoleName, out var foundRole))

                    {

                        assignedRole = foundRole;

                    }

                    else

                    {

                        assignedRole = new Role

                        {

                            Name = normalizedRoleName,

                            Description = $"Vai trò {normalizedRoleName} (tự động tạo khi import)"

                        };

                        _context.Roles.Add(assignedRole);

                        roles[normalizedRoleName] = assignedRole;

                    }

                }



                if (!rowHasError)

                {

                    seenEmails.Add(email);

                    dbEmails.Add(email);



                    var user = new User

                    {

                        Email = email.Trim().ToLower(),

                        FirstName = firstName.Trim(),

                        LastName = lastName.Trim(),

                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPass),

                        IsActive = true,

                        CreatedAt = DateTime.UtcNow,

                        UpdatedAt = DateTime.UtcNow

                    };



                    if (assignedRole != null)

                    {

                        user.Roles.Add(assignedRole);

                    }



                    validEntities.Add(user);

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Users.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 8. Import Books

        public async Task<ImportResultDto> ImportBooksAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            // 1. Danh mục các thuộc tính chuẩn 100% theo đúng file mẫu từ Frontend

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "name",

                "description"

            };



            // 2. Kiểm tra tính hợp lệ của dòng tiêu đề (Dòng 1)

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var headerErrors = new List<ImportErrorDto>();

            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);



                if (string.IsNullOrWhiteSpace(rawHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Tiêu đề tại cột số {c} bị để trống. Mọi cột trong file phải đúng thuộc tính theo file mẫu."

                    });

                    continue;

                }



                var cleanHeader = rawHeader.Trim();



                if (!allowedColumns.Contains(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' (cột số {c}) không đúng định dạng theo file mẫu. Vui lòng giữ nguyên thuộc tính chuẩn từ file mẫu."

                    });

                    continue;

                }



                if (recognizedCols.ContainsKey(cleanHeader))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"Thuộc tính cột '{rawHeader}' bị lặp lại nhiều lần trong file Excel."

                    });

                    continue;

                }



                recognizedCols[cleanHeader] = c;

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            // 3. Kiểm tra các cột bắt buộc phải có mặt từ file mẫu

            var requiredColumns = new[] { "name" };

            foreach (var reqCol in requiredColumns)

            {

                if (!recognizedCols.ContainsKey(reqCol))

                {

                    headerErrors.Add(new ImportErrorDto

                    {

                        RowNumber = 1,

                        Message = $"File Excel thiếu thuộc tính bắt buộc '{reqCol}'. Vui lòng sử dụng đúng file mẫu."

                    });

                }

            }



            if (headerErrors.Any())

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = headerErrors.Count,

                    Errors = headerErrors

                };

            }



            int colName = recognizedCols["name"];

            int colDesc = recognizedCols.TryGetValue("description", out var cd) ? cd : -1;



            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList();

            if (rows == null || !rows.Any())

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var errors = new List<ImportErrorDto>();

            var validEntities = new List<Book>();



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 2))) continue;



                var name = GetCellStr(row, colName);

                var desc = GetCellStr(row, colDesc);



                if (string.IsNullOrWhiteSpace(name))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên sách (name) không được để trống." });

                }

                else

                {

                    validEntities.Add(new Book

                    {

                        Name = name.Trim(),

                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),

                        IsActive = true,

                        CreatedAt = DateTime.UtcNow,

                        UpdatedAt = DateTime.UtcNow

                    });

                }

            }



            if (validEntities.Any())

            {

                _context.ChangeTracker.AutoDetectChangesEnabled = false;

                try

                {

                    await _context.Books.AddRangeAsync(validEntities);

                    await _context.SaveChangesAsync();

                }

                finally

                {

                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                }

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion



        #region 9. Import Student Scores

        public async Task<ImportResultDto> ImportStudentScoresAsync(IFormFile? file)

        {

            if (!ValidateFile(file, out var fileErr))

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };

            }



            using var workbook = LoadWorkbook(file!, out var loadErr);

            if (loadErr != null || workbook == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };

            }

            var ws = workbook.Worksheets.FirstOrDefault();

            if (ws == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có bất kỳ sheet dữ liệu nào." } } };

            }



            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();

            if (firstRow == null)

            {

                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File Excel không có dòng dữ liệu nào." } } };

            }



            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

            {

                "studentCode", "studentName", "className", "overallScore"

            };



            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "Dòng tiêu đề file Excel không có dữ liệu." } }

                };

            }



            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);



            for (int c = 1; c <= lastCol; c++)

            {

                var rawHeader = GetCellStr(firstRow, c);

                if (string.IsNullOrWhiteSpace(rawHeader)) continue;

                var cleanHeader = rawHeader.Trim();

                if (allowedColumns.Contains(cleanHeader))

                {

                    recognizedCols[cleanHeader] = c;

                }

            }



            int colStudentCode = recognizedCols.GetValueOrDefault("studentCode", 0);

            int colStudentName = recognizedCols.GetValueOrDefault("studentName", 0);

            int colClassName = recognizedCols.GetValueOrDefault("className", 0);

            int colOverallScore = recognizedCols.GetValueOrDefault("overallScore", 0);



            if (colStudentCode == 0 || colStudentName == 0 || colClassName == 0)

            {

                return new ImportResultDto

                {

                    SuccessCount = 0,

                    FailureCount = 1,

                    Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu các cột bắt buộc: studentCode, studentName, className." } }

                };

            }



            var rows = ws.RangeUsed()!.RowsUsed().Skip(1);

            var errors = new List<ImportErrorDto>();

            var validEntities = new List<StudentScore>();



            foreach (var row in rows)

            {

                int r = row.RowNumber();

                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 4))) continue;



                var studentCode = GetCellStr(row, colStudentCode);

                var studentName = GetCellStr(row, colStudentName);

                var className = GetCellStr(row, colClassName);

                var overallScoreStr = colOverallScore > 0 ? GetCellStr(row, colOverallScore) : null;



                if (string.IsNullOrWhiteSpace(studentCode))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Mã sinh viên (studentCode) không được để trống." });

                    continue;

                }



                if (string.IsNullOrWhiteSpace(studentName))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên sinh viên (studentName) không được để trống." });

                    continue;

                }



                if (string.IsNullOrWhiteSpace(className))

                {

                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên lớp học (className) không được để trống." });

                    continue;

                }



                decimal? overallScore = null;

                if (!string.IsNullOrWhiteSpace(overallScoreStr))

                {

                    if (decimal.TryParse(overallScoreStr.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var scoreVal))

                    {

                        overallScore = scoreVal;

                    }

                }



                validEntities.Add(new StudentScore

                {

                    StudentCode = studentCode.Trim(),

                    StudentName = studentName.Trim(),

                    ClassName = className.Trim(),

                    OverallScore = overallScore,

                    IsActive = true,

                    CreatedAt = DateTime.UtcNow,

                    UpdatedAt = DateTime.UtcNow

                });

            }



            if (validEntities.Any())

            {

                await _context.StudentScores.AddRangeAsync(validEntities);

                await _context.SaveChangesAsync();

            }



            return new ImportResultDto

            {

                SuccessCount = validEntities.Count,

                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),

                Errors = errors

            };

        }

        #endregion

    
        #region 10. Import Roles
        public async Task<ImportResultDto> ImportRolesAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("name"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'name'." } } };
            }

            int colName = recognizedCols["name"];
            int colDesc = recognizedCols.TryGetValue("description", out var cd) ? cd : -1;

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<Role>();
            var existingNames = (await _context.Roles.Select(r => r.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 2))) continue;

                var name = GetCellStr(row, colName);
                var desc = GetCellStr(row, colDesc);

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên vai trò (name) không được để trống." });
                    continue;
                }

                name = name.Trim().ToUpper();
                if (existingNames.Contains(name) || seen.Contains(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Tên vai trò '{name}' đã tồn tại trong hệ thống hoặc bị lặp lại." });
                    continue;
                }

                seen.Add(name);
                existingNames.Add(name);
                validEntities.Add(new Role
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.Roles.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

        #region 11. Import Permissions
        public async Task<ImportResultDto> ImportPermissionsAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("name") || !recognizedCols.ContainsKey("resource") || !recognizedCols.ContainsKey("action"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'name', 'resource' hoặc 'action'." } } };
            }

            int colName = recognizedCols["name"];
            int colResource = recognizedCols["resource"];
            int colAction = recognizedCols["action"];
            int colDesc = recognizedCols.TryGetValue("description", out var cd) ? cd : -1;

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<Permission>();
            var existingNames = (await _context.Permissions.Select(p => p.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 4))) continue;

                var name = GetCellStr(row, colName);
                var resource = GetCellStr(row, colResource);
                var action = GetCellStr(row, colAction);
                var desc = GetCellStr(row, colDesc);

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Các trường 'name', 'resource', 'action' không được để trống." });
                    continue;
                }

                name = name.Trim().ToUpper();
                if (existingNames.Contains(name) || seen.Contains(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Quyền hạn '{name}' đã tồn tại trong hệ thống hoặc bị lặp lại." });
                    continue;
                }

                seen.Add(name);
                existingNames.Add(name);
                validEntities.Add(new Permission
                {
                    Name = name,
                    Resource = resource.Trim().ToUpper(),
                    Action = action.Trim().ToUpper(),
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.Permissions.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

        #region 12. Import ModuleGroups
        public async Task<ImportResultDto> ImportModuleGroupsAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("name"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'name'." } } };
            }

            int colName = recognizedCols["name"];
            int colDesc = recognizedCols.TryGetValue("description", out var cd) ? cd : -1;
            int colOrder = recognizedCols.TryGetValue("displayOrder", out var co) ? co : -1;

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<ModuleGroup>();
            var existingNames = (await _context.ModuleGroups.Select(g => g.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 3))) continue;

                var name = GetCellStr(row, colName);
                var desc = GetCellStr(row, colDesc);
                var orderStr = GetCellStr(row, colOrder);

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên nhóm menu (name) không được để trống." });
                    continue;
                }

                name = name.Trim();
                if (existingNames.Contains(name) || seen.Contains(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Tên nhóm menu '{name}' đã tồn tại hoặc bị lặp lại." });
                    continue;
                }

                int displayOrder = int.TryParse(orderStr, out var ord) ? ord : 0;

                seen.Add(name);
                existingNames.Add(name);
                validEntities.Add(new ModuleGroup
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.ModuleGroups.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

        #region 13. Import Modules
        public async Task<ImportResultDto> ImportModulesAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("title"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'title'." } } };
            }

            int colTitle = recognizedCols["title"];
            int colUrl = recognizedCols.TryGetValue("url", out var cu) ? cu : -1;
            int colIcon = recognizedCols.TryGetValue("icon", out var ci) ? ci : -1;
            int colPerm = recognizedCols.TryGetValue("requiredPermission", out var cp) ? cp : -1;
            int colOrder = recognizedCols.TryGetValue("displayOrder", out var co) ? co : -1;
            int colGroup = recognizedCols.TryGetValue("moduleGroup", out var cg) ? cg : (recognizedCols.TryGetValue("group", out var cg2) ? cg2 : -1);

            var defaultGroup = await _context.ModuleGroups.FirstOrDefaultAsync();
            if (defaultGroup == null)
            {
                defaultGroup = new ModuleGroup { Name = "General", Description = "Default group for imported modules", DisplayOrder = 0 };
                _context.ModuleGroups.Add(defaultGroup);
                await _context.SaveChangesAsync();
            }

            var groupsDict = await _context.ModuleGroups.ToDictionaryAsync(g => g.Name.Trim().ToLower(), g => g.Id);

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<Module>();

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 5))) continue;

                var title = GetCellStr(row, colTitle);
                var url = GetCellStr(row, colUrl);
                var icon = GetCellStr(row, colIcon);
                var reqPerm = GetCellStr(row, colPerm);
                var orderStr = GetCellStr(row, colOrder);
                var groupName = GetCellStr(row, colGroup);

                if (string.IsNullOrWhiteSpace(title))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tiêu đề module (title) không được để trống." });
                    continue;
                }

                long groupId = defaultGroup.Id;
                if (!string.IsNullOrWhiteSpace(groupName) && groupsDict.TryGetValue(groupName.Trim().ToLower(), out var gId))
                {
                    groupId = gId;
                }

                int displayOrder = int.TryParse(orderStr, out var ord) ? ord : 0;

                validEntities.Add(new Module
                {
                    Title = title.Trim(),
                    Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
                    Icon = string.IsNullOrWhiteSpace(icon) ? "circle" : icon.Trim(),
                    RequiredPermission = string.IsNullOrWhiteSpace(reqPerm) ? null : reqPerm.Trim(),
                    DisplayOrder = displayOrder,
                    ModuleGroupId = groupId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.Modules.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

        #region 14. Import Interview Topics
        public async Task<ImportResultDto> ImportInterviewTopicsAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("name"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'name'." } } };
            }

            int colName = recognizedCols["name"];
            int colDesc = recognizedCols.TryGetValue("description", out var cd) ? cd : -1;
            int colOrder = recognizedCols.TryGetValue("displayOrder", out var co) ? co : -1;

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<InterviewTopic>();
            var existingNames = (await _context.InterviewTopics.Select(t => t.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 3))) continue;

                var name = GetCellStr(row, colName);
                var desc = GetCellStr(row, colDesc);
                var orderStr = GetCellStr(row, colOrder);

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Tên chủ đề phỏng vấn (name) không được để trống." });
                    continue;
                }

                name = name.Trim();
                if (existingNames.Contains(name) || seen.Contains(name))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = $"Tên chủ đề phỏng vấn '{name}' đã tồn tại hoặc bị lặp lại." });
                    continue;
                }

                int displayOrder = int.TryParse(orderStr, out var ord) ? ord : 0;

                seen.Add(name);
                existingNames.Add(name);
                validEntities.Add(new InterviewTopic
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim(),
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.InterviewTopics.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

        #region 15. Import Interview Questions
        public async Task<ImportResultDto> ImportInterviewQuestionsAsync(IFormFile? file)
        {
            if (!ValidateFile(file, out var fileErr))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { fileErr! } };
            }

            using var workbook = LoadWorkbook(file!, out var loadErr);
            if (loadErr != null || workbook == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { loadErr ?? new ImportErrorDto { RowNumber = 0, Message = "Lỗi khi đọc file." } } };
            }

            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có bất kỳ sheet dữ liệu nào." } } };
            }

            var firstRow = ws.RangeUsed()?.RowsUsed()?.FirstOrDefault();
            if (firstRow == null)
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 0, Message = "File không có dòng dữ liệu nào." } } };
            }

            int lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var recognizedCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = GetCellStr(firstRow, c);
                if (!string.IsNullOrWhiteSpace(h)) recognizedCols[h.Trim()] = c;
            }

            if (!recognizedCols.ContainsKey("questionText"))
            {
                return new ImportResultDto { SuccessCount = 0, FailureCount = 1, Errors = new List<ImportErrorDto> { new ImportErrorDto { RowNumber = 1, Message = "File thiếu thuộc tính bắt buộc 'questionText'." } } };
            }

            int colText = recognizedCols["questionText"];
            int colTopic = recognizedCols.TryGetValue("topicName", out var ct) ? ct : (recognizedCols.TryGetValue("topic", out var ct2) ? ct2 : -1);
            int colType = recognizedCols.TryGetValue("questionType", out var ctype) ? ctype : (recognizedCols.TryGetValue("type", out var ctype2) ? ctype2 : -1);
            int colDiff = recognizedCols.TryGetValue("difficulty", out var cdiff) ? cdiff : -1;
            int colAnswer = recognizedCols.TryGetValue("sampleAnswer", out var ca) ? ca : (recognizedCols.TryGetValue("answer", out var ca2) ? ca2 : -1);
            int colKeywords = recognizedCols.TryGetValue("keywords", out var ck) ? ck : -1;
            int colCorrect = recognizedCols.TryGetValue("correctOptionId", out var cco) ? cco : -1;
            int colOrder = recognizedCols.TryGetValue("displayOrder", out var co) ? co : -1;

            var defaultTopic = await _context.InterviewTopics.FirstOrDefaultAsync();
            if (defaultTopic == null)
            {
                defaultTopic = new InterviewTopic { Name = "General Assessment", Description = "Default topic for imported questions", DisplayOrder = 0 };
                _context.InterviewTopics.Add(defaultTopic);
                await _context.SaveChangesAsync();
            }

            var topicsDict = await _context.InterviewTopics.ToDictionaryAsync(t => t.Name.Trim().ToLower(), t => t.Id);

            var rows = ws.RangeUsed()?.RowsUsed()?.Skip(1).ToList() ?? new List<IXLRangeRow>();
            var errors = new List<ImportErrorDto>();
            var validEntities = new List<InterviewQuestion>();

            foreach (var row in rows)
            {
                int r = row.RowNumber();
                if (IsRowCompletelyEmpty(row, Math.Max(lastCol, 6))) continue;

                var text = GetCellStr(row, colText);
                var topicName = GetCellStr(row, colTopic);
                var type = GetCellStr(row, colType);
                var diff = GetCellStr(row, colDiff);
                var sampleAnswer = GetCellStr(row, colAnswer);
                var keywords = GetCellStr(row, colKeywords);
                var correctOption = GetCellStr(row, colCorrect);
                var orderStr = GetCellStr(row, colOrder);

                if (string.IsNullOrWhiteSpace(text))
                {
                    errors.Add(new ImportErrorDto { RowNumber = r, Message = "Nội dung câu hỏi (questionText) không được để trống." });
                    continue;
                }

                long topicId = defaultTopic.Id;
                if (!string.IsNullOrWhiteSpace(topicName))
                {
                    var tn = topicName.Trim().ToLower();
                    if (topicsDict.TryGetValue(tn, out var foundTopicId))
                    {
                        topicId = foundTopicId;
                    }
                    else
                    {
                        var newTopic = new InterviewTopic { Name = topicName.Trim(), DisplayOrder = 0 };
                        _context.InterviewTopics.Add(newTopic);
                        await _context.SaveChangesAsync();
                        topicsDict[tn] = newTopic.Id;
                        topicId = newTopic.Id;
                    }
                }

                var qType = string.IsNullOrWhiteSpace(type) ? "WRITTEN" : type.Trim().ToUpper();
                if (qType != "MCQ" && qType != "WRITTEN" && qType != "CODE" && qType != "SQL")
                {
                    qType = "WRITTEN";
                }

                var qDiff = string.IsNullOrWhiteSpace(diff) ? "MEDIUM" : diff.Trim().ToUpper();
                if (qDiff != "EASY" && qDiff != "MEDIUM" && qDiff != "HARD")
                {
                    qDiff = "MEDIUM";
                }

                int displayOrder = int.TryParse(orderStr, out var ord) ? ord : 0;

                validEntities.Add(new InterviewQuestion
                {
                    TopicId = topicId,
                    QuestionText = text.Trim(),
                    QuestionType = qType,
                    Difficulty = qDiff,
                    SampleAnswer = string.IsNullOrWhiteSpace(sampleAnswer) ? null : sampleAnswer.Trim(),
                    Keywords = string.IsNullOrWhiteSpace(keywords) ? null : keywords.Trim(),
                    CorrectOptionId = string.IsNullOrWhiteSpace(correctOption) ? null : correctOption.Trim(),
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (validEntities.Any())
            {
                await _context.InterviewQuestions.AddRangeAsync(validEntities);
                await _context.SaveChangesAsync();
            }

            return new ImportResultDto
            {
                SuccessCount = validEntities.Count,
                FailureCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors
            };
        }
        #endregion

    }
}
