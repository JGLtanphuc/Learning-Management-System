using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class Permission : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Resource { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
