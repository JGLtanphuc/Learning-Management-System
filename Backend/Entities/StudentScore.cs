using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class StudentScore : BaseEntity
{
    public string StudentCode { get; set; } = null!;

    public string StudentName { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public decimal? OverallScore { get; set; }

    public virtual ICollection<StudentScoreColumn> StudentScoreColumns { get; set; } = new List<StudentScoreColumn>();
}
