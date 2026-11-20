using System.Collections.Generic;

namespace Backend.Entities
{
    public class InterviewQuestion : BaseEntity
    {
        public long TopicId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public string? Keywords { get; set; }
        public string Difficulty { get; set; } = "MEDIUM";
        public string QuestionType { get; set; } = "WRITTEN"; // WRITTEN, MCQ
        public string? OptionsJson { get; set; }
        public string? CorrectOptionId { get; set; }
        public int DisplayOrder { get; set; }

        public virtual InterviewTopic Topic { get; set; } = null!;
        public virtual ICollection<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
    }
}
