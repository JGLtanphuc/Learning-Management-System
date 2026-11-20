namespace Backend.Entities
{
    public class InterviewAnswer : BaseEntity
    {
        public long QuestionId { get; set; }
        public long UserId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public string AnswerMode { get; set; } = "TEXT";
        public string? TeacherNote { get; set; }
        public decimal? Score { get; set; }
        public decimal? AiScore { get; set; }
        public string? AiFeedback { get; set; }
        public string? AiVerdict { get; set; }
        public string? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }

        public long? SessionId { get; set; }
        public bool IsFlagged { get; set; } = false;
        public string? SelectedOptionId { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal? TeacherScore { get; set; }

        public virtual InterviewQuestion Question { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual InterviewSession? Session { get; set; }
    }
}
