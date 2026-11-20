using System;

namespace Backend.DTOs.Attendance
{
    public class AttendanceFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public long? ClassId { get; set; }
        public long? StudentId { get; set; }
        public DateTime? AttendanceDate { get; set; }
        public string? Slot { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}

