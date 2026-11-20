using System;
using System.Collections.Generic;

namespace Backend.Entities
{
    public class InterviewAssignment : BaseEntity
    {
        public long TopicId { get; set; }
        public long? ClassId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; } = 45;
        public int TotalQuestions { get; set; } = 10;
        public int EasyCount { get; set; } = 4;
        public int MediumCount { get; set; } = 4;
        public int HardCount { get; set; } = 2;
        public decimal PassingScore { get; set; } = 7.0m;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "PUBLISHED"; // DRAFT, PUBLISHED, CLOSED

        public virtual InterviewTopic Topic { get; set; } = null!;
        public virtual TrainingClass? Class { get; set; }
        public virtual ICollection<InterviewSession> Sessions { get; set; } = new List<InterviewSession>();
    }
}
