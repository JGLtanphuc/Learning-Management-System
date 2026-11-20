using System;
using Backend.DTOs.Common;

namespace Backend.DTOs.Student
{
    public class StudentResponseDto : IHasId
    {
        public long Id { get; set; }
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public long? ClassId { get; set; }
        public string? ClassName { get; set; }
        public long? MockProjectId { get; set; }
        public string? MockProjectName { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public decimal? Gpa { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

