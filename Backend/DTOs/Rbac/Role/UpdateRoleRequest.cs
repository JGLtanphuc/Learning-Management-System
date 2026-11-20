using System.Collections.Generic;

namespace Backend.DTOs.Rbac
{
    public class UpdateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<long> PermissionIds { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
