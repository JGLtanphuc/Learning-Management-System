using System;

namespace Backend.DTOs.Allocation
{
    public class CreateAllocationRequest
    {
        public long StudentId { get; set; }
        public string TargetUnit { get; set; } = null!;
        public string? ProjectName { get; set; }
        public string RoleTitle { get; set; } = "Fresher Developer";
        public decimal? MatchScore { get; set; }
        public string Status { get; set; } = "PROPOSED";
        public DateTime? InterviewDate { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

