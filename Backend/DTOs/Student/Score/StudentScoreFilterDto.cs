namespace Backend.DTOs.Student.Score
{
    public class StudentScoreFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? ClassName { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}

