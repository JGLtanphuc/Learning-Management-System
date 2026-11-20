using System;

namespace Backend.DTOs.Attendance
{
    public class UpdateAttendanceRequest
    {
        public long StudentId { get; set; }
        public long ClassId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Slot { get; set; } = "MORNING";
        public string Status { get; set; } = "PRESENT";
        public string? Note { get; set; }
        public bool IsActive { get; set; }
    }
}

