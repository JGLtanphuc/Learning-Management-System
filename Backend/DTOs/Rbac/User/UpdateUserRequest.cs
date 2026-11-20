using System.Collections.Generic;

namespace Backend.DTOs.Rbac
{
    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public List<long> RoleIds { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
