namespace Backend.DTOs.Interview
{
    public class InterviewSessionQuestionDto
    {
        public long QuestionId { get; set; }
        public int DisplayOrder { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "MEDIUM";
        public string QuestionType { get; set; } = "WRITTEN";
        public string? OptionsJson { get; set; }
        public string? SampleAnswer { get; set; }
        public string? CorrectOptionId { get; set; }

        // Candidate response
        public string? AnswerText { get; set; }
        public string? SelectedOptionId { get; set; }
        public string AnswerMode { get; set; } = "TEXT";
        public bool IsFlagged { get; set; }

        // Evaluation
        public bool? IsCorrect { get; set; }
        public decimal? Score { get; set; }
        public decimal? AiScore { get; set; }
        public string? AiVerdict { get; set; }
        public string? AiFeedback { get; set; }
        public decimal? TeacherScore { get; set; }
        public string? TeacherNote { get; set; }
    }
}
