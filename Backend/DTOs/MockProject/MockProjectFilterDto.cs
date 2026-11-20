namespace Backend.DTOs.MockProject
{
    public class MockProjectFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? Status { get; set; }
        public string? MentorName { get; set; }
        public string? TechStack { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}

