using System;
using System.Collections.Generic;

namespace Backend.DTOs.Interview
{
    public class InterviewSessionResponseDto
    {
        public long SessionId { get; set; }
        public long AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public decimal PassingScore { get; set; }
        public long UserId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string? ClassName { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int RemainingSeconds { get; set; }
        public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, SUBMITTED, REVIEWED, PUBLISHED

        public decimal? TotalScore { get; set; }
        public decimal? AiAverageScore { get; set; }
        public decimal? TeacherScore { get; set; }
        public string? OverallVerdict { get; set; }
        public string? OverallSummary { get; set; }
        public string? TeacherFeedback { get; set; }
        public long? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public List<InterviewSessionQuestionDto> Questions { get; set; } = new();
        public CandidateInterviewReportDto? Report { get; set; }
    }
}
