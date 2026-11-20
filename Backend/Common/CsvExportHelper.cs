using System;
using System.Text;

namespace Backend.Common
{
    /// <summary>
    /// Helper tiện ích dùng chung để xử lý xuất dữ liệu định dạng CSV chuẩn UTF-8 kèm BOM.
    /// Giúp hiển thị tiếng Việt chính xác khi mở trên Microsoft Excel mà không bị lỗi font/dấu.
    /// </summary>
    public static class CsvExportHelper
    {
        /// <summary>
        /// Chuyển đổi chuỗi CSV thành mảng byte UTF-8 có chứa BOM (Byte Order Mark: 0xEF, 0xBB, 0xBF).
        /// </summary>
        public static byte[] ToUtf8BomBytes(string csvContent)
        {
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(csvContent ?? string.Empty);
            var result = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);
            return result;
        }

        /// <summary>
        /// Escape một trường văn bản trong CSV (thay thế dấu ngoặc kép đôi và bao quanh bằng ngoặc kép nếu có dấu phẩy/xuống dòng).
        /// </summary>
        public static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
            {
                return $"\"{escaped}\"";
            }
            return escaped;
        }
    }
}
