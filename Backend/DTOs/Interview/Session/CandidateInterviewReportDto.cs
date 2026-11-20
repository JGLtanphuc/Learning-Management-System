using System.Collections.Generic;

namespace Backend.DTOs.Interview
{
    public class CandidateInterviewReportDto
    {
        // 1. Quantitative Core Metrics
        public decimal TotalScorePercentage { get; set; }
        public decimal CompletionRate { get; set; } 
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }

        // 2. Proficiency & Competency Level
        public string ProficiencyRank { get; set; } = "COMPETENT"; // ADVANCED, PROFICIENT, COMPETENT, DEVELOPING, NOVICE
        public string ProficiencyRankTitle { get; set; } = string.Empty;
        public string ProficiencySummary { get; set; } = string.Empty;
        public string OjtRecommendation { get; set; } = string.Empty;

        // 3. Difficulty Breakdown
        public int EasyCount { get; set; }
        public decimal EasyScorePercent { get; set; }
        public int MediumCount { get; set; }
        public decimal MediumScorePercent { get; set; }
        public int HardCount { get; set; }
        public decimal HardScorePercent { get; set; }

        // 4. Format Breakdown
        public int McqTotal { get; set; }
        public int McqCorrect { get; set; }
        public decimal McqAccuracyRate { get; set; }

        public int SubjectiveTotal { get; set; }
        public decimal SubjectiveScorePercent { get; set; }

        // 5. Strengths & Growth Areas
        public List<string> Strengths { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
