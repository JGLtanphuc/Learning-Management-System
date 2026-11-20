using System;

namespace Backend.Entities;

public partial class Book : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
