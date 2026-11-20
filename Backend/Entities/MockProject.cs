using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class MockProject : BaseEntity
{
    public string ProjectName { get; set; } = null!;

    public string? MentorName { get; set; }

    public string? TechStack { get; set; }

    public string? RepoUrl { get; set; }

    public string Status { get; set; } = "PLANNING"; // PLANNING, ACTIVE, COMPLETED, CANCELLED

    public decimal? FinalScore { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
