using System.Collections.Generic;

namespace Backend.DTOs.Student.Score
{
    public class CreateStudentScoreRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal? OverallScore { get; set; }
        public Dictionary<string, decimal?>? Scores { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
