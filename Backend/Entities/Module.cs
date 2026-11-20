using System;
using System.Collections.Generic;

namespace Backend.Entities;

public partial class Module : BaseEntity
{
    public long ModuleGroupId { get; set; }

    public string Title { get; set; } = null!;

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public string? RequiredPermission { get; set; }

    public virtual ModuleGroup ModuleGroup { get; set; } = null!;
}
