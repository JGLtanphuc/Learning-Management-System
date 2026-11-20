using System;

namespace Backend.DTOs.Allocation
{
    public class AllocationFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public long? StudentId { get; set; }
        public string? TargetUnit { get; set; }
        public string? RoleTitle { get; set; }
        public string? Status { get; set; }
        public DateTime? InterviewDate { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}

