namespace Backend.DTOs.Teacher
{
    public class TeacherFilterDto
    {
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
