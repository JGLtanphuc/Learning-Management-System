using System.Collections.Generic;

namespace Backend.DTOs.Interview
{
    public class ReviewSessionRequest
    {
        public decimal? SessionTeacherScore { get; set; }
        public string? TeacherFeedback { get; set; }
        public string? OverallVerdict { get; set; } // PASS, FAIL
        public bool PublishResult { get; set; } = true;
        public List<ReviewQuestionScoreDto>? QuestionScores { get; set; }
    }
}
