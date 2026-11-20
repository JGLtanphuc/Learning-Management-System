namespace Backend.DTOs.Interview
{
    public class InterviewAssignmentFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public long? TopicId { get; set; }
        public long? ClassId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}
