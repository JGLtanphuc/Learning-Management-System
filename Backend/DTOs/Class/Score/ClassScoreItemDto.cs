using System.Collections.Generic;

namespace Backend.DTOs.Class.Score
{
    public class ClassScoreItemDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long? ScoreId { get; set; }
        public Dictionary<string, decimal?> Scores { get; set; } = new();
        public decimal? OverallScore { get; set; }
    }
}
