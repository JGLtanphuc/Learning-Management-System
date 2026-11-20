using System;

namespace Backend.DTOs.Class
{
    public class ClassFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? Status { get; set; }
        public string? ProgramName { get; set; }
        public string? Location { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}
