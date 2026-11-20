using System.Collections.Generic;

namespace Backend.Services.Interfaces
{
    public interface ICurrentUserService
    {
        long? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsTeacher { get; }
        bool IsStudent { get; }
        IEnumerable<string> Roles { get; }
    }
}

