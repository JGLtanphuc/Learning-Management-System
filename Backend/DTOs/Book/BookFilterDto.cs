namespace Backend.DTOs.Book
{
    public class BookFilterDto
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 0;
        public int Size { get; set; } = 10;
    }
}
