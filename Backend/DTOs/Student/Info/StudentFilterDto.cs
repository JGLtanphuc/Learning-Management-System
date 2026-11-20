namespace Backend.DTOs.Student
{
    public class StudentFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public long? ClassId { get; set; }
        public string? Status { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public decimal? MinGpa { get; set; }
        public decimal? MaxGpa { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}

