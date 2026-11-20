using Backend.DTOs.Common;
using System;
using System.Collections.Generic;

namespace Backend.DTOs.Student.Score
{
    public class StudentScoreResponseDto : IHasId
    {
        public long Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal? OverallScore { get; set; }
        public Dictionary<string, decimal?> Scores { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }
}
