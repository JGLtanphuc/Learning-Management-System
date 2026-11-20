using System.Collections.Generic;

namespace Backend.DTOs.TopicMark
{
    public class TopicMarkPageResponse<T>
    {
        public List<T> Content { get; set; } = new();
        public int Page { get; set; }
        public int Size { get; set; }
        public long TotalElements { get; set; }
        public int TotalPages { get; set; }
        public string? Sort { get; set; }
    }
}
