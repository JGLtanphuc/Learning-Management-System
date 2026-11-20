using System;
using System.Collections.Generic;
using Backend.DTOs.Common;

namespace Backend.DTOs.Rbac
{
    public class RoleResponseDto : IHasId
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<long> PermissionIds { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }
}
