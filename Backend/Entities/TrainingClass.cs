using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class TrainingClass : BaseEntity
{
    public string ClassCode { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string? ProgramName { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Capacity { get; set; } = 25;

    public string? Location { get; set; }

    public string? Room { get; set; }

    public string Status { get; set; } = "PLANNING"; // PLANNING, ONGOING, COMPLETED, CANCELLED

    public long? TeacherId { get; set; }

    public virtual User? Teacher { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
