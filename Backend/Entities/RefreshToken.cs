using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class RefreshToken
{
    public long Id { get; set; }

    public string TokenHash { get; set; } = null!;

    public long UserId { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }

    public bool Revoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

