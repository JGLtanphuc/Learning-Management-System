using System;

namespace Backend.Entities;

public partial class Attendance : BaseEntity
{
    public long StudentId { get; set; }

    public long ClassId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public string Slot { get; set; } = "MORNING"; // MORNING, AFTERNOON, ALL_DAY

    public string Status { get; set; } = "PRESENT"; // PRESENT, ABSENT, LATE, EXCUSED

    public string? Note { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual TrainingClass Class { get; set; } = null!;
}
