namespace Backend.DTOs.TopicMark
{
    public class TopicMarkGradebookColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? ComponentId { get; set; }
        public int? ComponentIndex { get; set; }
        public string? ComponentType { get; set; }
        public decimal? Weight { get; set; }
        public bool? IsGraded { get; set; }
    }
}
