using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Backend.Common
{
    public static class ExcelExportHelper
    {
        public static byte[] CreateWorkbook(string sheetName, string title, string[] headers, IEnumerable<object?[]> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);
            ws.ShowGridLines = true;

            int colCount = headers.Length > 0 ? headers.Length : 1;

            // Row 1: Main Title Banner
            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, colCount).Merge();
            ws.Row(1).Height = 32;
            var titleStyle = ws.Cell(1, 1).Style;
            titleStyle.Font.FontSize = 14;
            titleStyle.Font.Bold = true;
            titleStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            titleStyle.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            titleStyle.Font.FontColor = XLColor.FromHtml("#0F172A");

            // Materialize rows to count
            var rowList = rows.ToList();

            // Row 2: Subtitle / Metadata
            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số bản ghi: {rowList.Count}";
            ws.Range(2, 1, 2, colCount).Merge();
            ws.Row(2).Height = 20;
            var subStyle = ws.Cell(2, 1).Style;
            subStyle.Font.FontSize = 9;
            subStyle.Font.Italic = true;
            subStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            subStyle.Font.FontColor = XLColor.FromHtml("#64748B");

            // Row 4: Column Headers
            ws.Row(4).Height = 24;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Row 5+: Data Rows
            int currentRow = 5;
            foreach (var r in rowList)
            {
                ws.Row(currentRow).Height = 20;
                for (int c = 0; c < r.Length && c < headers.Length; c++)
                {
                    var val = r[c];
                    var cell = ws.Cell(currentRow, c + 1);

                    if (val == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (val is DateTime dt)
                    {
                        cell.Value = dt.ToString("dd/MM/yyyy HH:mm");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (val is DateOnly dOnly)
                    {
                        cell.Value = dOnly.ToString("dd/MM/yyyy");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (val is bool b)
                    {
                        cell.Value = b ? "Kích hoạt" : "Đã khóa";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (val is int || val is long || val is short || val is byte)
                    {
                        cell.Value = Convert.ToInt64(val);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (val is double || val is decimal || val is float)
                    {
                        cell.Value = Convert.ToDouble(val);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else
                    {
                        cell.Value = val.ToString() ?? string.Empty;
                    }

                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                currentRow++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
