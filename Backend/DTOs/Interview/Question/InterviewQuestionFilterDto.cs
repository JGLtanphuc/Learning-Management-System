namespace Backend.DTOs.Interview
{
    public class InterviewQuestionFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public long? TopicId { get; set; }
        public string? Difficulty { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}
