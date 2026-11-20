namespace Backend.DTOs.Student
{
    public class UpdateStudentRequest
    {
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public long? ClassId { get; set; }
        public long? MockProjectId { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public decimal? Gpa { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public bool IsActive { get; set; }
    }
}

