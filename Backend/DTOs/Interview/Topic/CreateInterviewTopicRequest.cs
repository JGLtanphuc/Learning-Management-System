namespace Backend.DTOs.Interview
{
    public class CreateInterviewTopicRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
