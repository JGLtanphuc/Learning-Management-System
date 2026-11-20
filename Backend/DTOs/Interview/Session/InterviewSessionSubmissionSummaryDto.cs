using System;

namespace Backend.DTOs.Interview
{
    public class InterviewSessionSubmissionSummaryDto
    {
        public long SessionId { get; set; }
        public long AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = "IN_PROGRESS";
        public decimal? TotalScore { get; set; }
        public decimal? AiAverageScore { get; set; }
        public decimal? TeacherScore { get; set; }
        public string? OverallVerdict { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
    }
}
