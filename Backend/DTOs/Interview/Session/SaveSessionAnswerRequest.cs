namespace Backend.DTOs.Interview
{
    public class SaveSessionAnswerRequest
    {
        public long QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public string? SelectedOptionId { get; set; }
        public string AnswerMode { get; set; } = "TEXT";
        public bool IsFlagged { get; set; }
    }
}
