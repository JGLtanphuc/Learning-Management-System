using Backend.DTOs.Common;
using System;

namespace Backend.DTOs.Interview
{
    public class InterviewQuestionResponseDto : IHasId
    {
        public long Id { get; set; }
        public long TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public string? Keywords { get; set; }
        public string Difficulty { get; set; } = "MEDIUM";
        public string QuestionType { get; set; } = "WRITTEN"; // WRITTEN, MCQ
        public string? OptionsJson { get; set; }
        public string? CorrectOptionId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }
}
