using System.Collections.Generic;

namespace Backend.DTOs.Class.Score
{
    public class ClassScoreSheetDto
    {
        public long ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public List<ClassScoreItemDto> Students { get; set; } = new();
    }
}
