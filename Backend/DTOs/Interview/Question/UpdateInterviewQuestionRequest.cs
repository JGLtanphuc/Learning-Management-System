namespace Backend.DTOs.Interview
{
    public class UpdateInterviewQuestionRequest
    {
        public long TopicId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public string? Keywords { get; set; }
        public string Difficulty { get; set; } = "MEDIUM";
        public string QuestionType { get; set; } = "WRITTEN";
        public string? OptionsJson { get; set; }
        public string? CorrectOptionId { get; set; }
        public int? DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
