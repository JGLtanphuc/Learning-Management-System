namespace Backend.DTOs.Interview
{
    public class ReviewQuestionScoreDto
    {
        public long QuestionId { get; set; }
        public decimal? TeacherScore { get; set; }
        public string? TeacherNote { get; set; }
    }
}
