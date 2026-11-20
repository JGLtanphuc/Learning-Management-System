using System;
using System.Collections.Generic;

namespace Backend.DTOs.Attendance
{
    public class ClassAttendanceItemDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long? AttendanceId { get; set; }
        public string Status { get; set; } = "PRESENT";
        public string? Note { get; set; }
    }

    public class ClassAttendanceSheetDto
    {
        public long ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime AttendanceDate { get; set; }
        public string Slot { get; set; } = "MORNING";
        public List<ClassAttendanceItemDto> Students { get; set; } = new();
    }
}

