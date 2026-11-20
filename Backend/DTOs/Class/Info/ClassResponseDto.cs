using System;
using Backend.DTOs.Common;

namespace Backend.DTOs.Class
{
    public class ClassResponseDto : IHasId
    {
        public long Id { get; set; }
        public string ClassCode { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string? ProgramName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Capacity { get; set; } = 25;
        public string? Location { get; set; }
        public string? Room { get; set; }
        public string Status { get; set; } = "PLANNING";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalStudents { get; set; }
        public long? TeacherId { get; set; }
        public string? TeacherName { get; set; }
    }
}
