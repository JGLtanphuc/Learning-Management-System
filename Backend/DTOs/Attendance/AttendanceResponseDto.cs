using System;
using Backend.DTOs.Common;

namespace Backend.DTOs.Attendance
{
    public class AttendanceResponseDto : IHasId
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public string? StudentCode { get; set; }
        public string? StudentName { get; set; }
        public long ClassId { get; set; }
        public string? ClassName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Slot { get; set; } = "MORNING";
        public string Status { get; set; } = "PRESENT";
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

