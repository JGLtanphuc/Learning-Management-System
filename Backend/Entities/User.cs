using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class User : BaseEntity
{
    public string Email { get; set; } = null!;

    public string? TeacherCode { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? LastName { get; set; }

    public virtual ICollection<OneTimeToken> OneTimeTokens { get; set; } = new List<OneTimeToken>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
