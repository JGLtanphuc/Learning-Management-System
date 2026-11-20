using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class Student : BaseEntity
{
    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public long? ClassId { get; set; }

    public string? University { get; set; }

    public string? Major { get; set; }

    public decimal? Gpa { get; set; }

    public string Status { get; set; } = "ACTIVE"; // ACTIVE, RESERVE, DROP_OUT, COMPLETED

    public DateTime JoinedAt { get; set; }

    public long? MockProjectId { get; set; }

    public long? UserId { get; set; }

    public virtual User? User { get; set; }

    public virtual TrainingClass? Class { get; set; }

    public virtual MockProject? MockProject { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
}
