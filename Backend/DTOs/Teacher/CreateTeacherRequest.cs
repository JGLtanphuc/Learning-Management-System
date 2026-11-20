using System.Collections.Generic;

namespace Backend.DTOs.Teacher
{
    public class CreateTeacherRequest
    {
        public string? TeacherCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public List<long>? ClassIds { get; set; } = new();
    }
}
