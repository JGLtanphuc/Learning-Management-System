namespace Backend.DTOs.Book
{
    public class UpdateBookRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}

