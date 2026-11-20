using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class OneTimeToken
{
    public long Id { get; set; }

    public string TokenHash { get; set; } = null!;

    public long UserId { get; set; }

    public string Type { get; set; } = null!;

    public DateTimeOffset ExpiryDate { get; set; }

    public bool Used { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

