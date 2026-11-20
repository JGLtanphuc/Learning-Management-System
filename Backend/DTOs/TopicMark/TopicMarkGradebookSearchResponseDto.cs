using System.Collections.Generic;

namespace Backend.DTOs.TopicMark
{
    public class TopicMarkGradebookSearchResponseDto
    {
        public List<TopicMarkGradebookColumnDto> Columns { get; set; } = new();
        public TopicMarkPageResponse<TopicMarkGradebookRowDto> Rows { get; set; } = new();
    }
}
