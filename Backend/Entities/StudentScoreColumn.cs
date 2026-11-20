using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class StudentScoreColumn
{
    public long StudentScoreId { get; set; }

    public string ScoreKey { get; set; } = null!;

    public decimal? ScoreValue { get; set; }

    public virtual StudentScore StudentScore { get; set; } = null!;
}

