using System.Collections.Generic;

namespace Backend.DTOs.TopicMark
{
    public class TopicMarkGradebookRowDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Dictionary<string, object?> Values { get; set; } = new();
    }
}
