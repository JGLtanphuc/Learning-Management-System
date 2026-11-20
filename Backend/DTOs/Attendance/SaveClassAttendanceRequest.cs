using System;
using System.Collections.Generic;

namespace Backend.DTOs.Attendance
{
    public class SaveAttendanceItemRequest
    {
        public long StudentId { get; set; }
        public string Status { get; set; } = "PRESENT";
        public string? Note { get; set; }
    }

    public class SaveClassAttendanceRequest
    {
        public long ClassId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Slot { get; set; } = "MORNING";
        public List<SaveAttendanceItemRequest> Items { get; set; } = new();
    }
}

