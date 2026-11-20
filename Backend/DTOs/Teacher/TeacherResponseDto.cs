using System;
using System.Collections.Generic;
using Backend.DTOs.Common;

namespace Backend.DTOs.Teacher
{
    public class TeacherResponseDto : IHasId
    {
        public long Id { get; set; }
        public string? TeacherCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> AssignedClasses { get; set; } = new();
        public List<long> AssignedClassIds { get; set; } = new();
        public List<long> ClassIds => AssignedClassIds;
        public int TotalAssignedClasses { get; set; }
    }
}

