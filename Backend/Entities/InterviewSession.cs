using System;
using System.Collections.Generic;

namespace Backend.Entities
{
    public class InterviewSession : BaseEntity
    {
        public long AssignmentId { get; set; }
        public long UserId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, SUBMITTED, REVIEWED, PUBLISHED
        public decimal? TotalScore { get; set; }
        public decimal? AiAverageScore { get; set; }
        public decimal? TeacherScore { get; set; }
        public string? OverallVerdict { get; set; }
        public string? OverallSummary { get; set; }
        public string? TeacherFeedback { get; set; }
        public long? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public virtual InterviewAssignment Assignment { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
    }
}
