using System;
using Backend.DTOs.Common;

namespace Backend.DTOs.Allocation
{
    public class AllocationResponseDto : IHasId
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public string? StudentCode { get; set; }
        public string? StudentName { get; set; }
        public string TargetUnit { get; set; } = null!;
        public string? ProjectName { get; set; }
        public string RoleTitle { get; set; } = "Fresher Developer";
        public decimal? MatchScore { get; set; }
        public string Status { get; set; } = "PROPOSED";
        public DateTime? InterviewDate { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

