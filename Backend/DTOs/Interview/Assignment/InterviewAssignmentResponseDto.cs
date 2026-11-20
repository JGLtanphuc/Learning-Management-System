using Backend.DTOs.Common;
using System;

namespace Backend.DTOs.Interview
{
    public class InterviewAssignmentResponseDto : IHasId
    {
        public long Id { get; set; }
        public long TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public long? ClassId { get; set; }
        public string? ClassCode { get; set; }
        public string? ClassName { get; set; }
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
        public string Status { get; set; } = "PUBLISHED";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }

        // Dashboard aggregations for teachers/admin
        public int TotalSubmissions { get; set; }
        public int PendingReviewCount { get; set; }
        public decimal? AverageScore { get; set; }

        // Student-specific session status (if requested in student context)
        public string? MySessionStatus { get; set; }
        public long? MySessionId { get; set; }
        public decimal? MyTotalScore { get; set; }
    }
}
