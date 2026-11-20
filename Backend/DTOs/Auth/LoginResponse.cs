using System.Collections.Generic;

namespace Backend.DTOs.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
        public string? RefreshToken { get; set; }
    }
}
