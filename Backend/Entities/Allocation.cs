using System;

namespace Backend.Entities;

public partial class Allocation : BaseEntity
{
    public long StudentId { get; set; }

    public string TargetUnit { get; set; } = null!; // e.g. FSU1.BU9, DXL, GAM

    public string? ProjectName { get; set; }

    public string RoleTitle { get; set; } = "Fresher Developer";

    public decimal? MatchScore { get; set; }

    public string Status { get; set; } = "PROPOSED"; // PROPOSED, INTERVIEWING, ALLOCATED, REJECTED

    public DateTime? InterviewDate { get; set; }

    public string? Note { get; set; }

    public virtual Student Student { get; set; } = null!;
}
