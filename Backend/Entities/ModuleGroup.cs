using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class ModuleGroup : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}
