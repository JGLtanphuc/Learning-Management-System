using System.Collections.Generic;

namespace Backend.DTOs.Class.Score
{
    public class SaveClassScoreBatchRequest
    {
        public long ClassId { get; set; }
        public string? ClassName { get; set; }
        public List<ClassScoreItemDto> Items { get; set; } = new();
    }
}
