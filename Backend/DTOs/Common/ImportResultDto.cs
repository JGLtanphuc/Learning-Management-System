namespace Backend.DTOs.Common
{
    public class ImportResultDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new();
    }

    public class ImportErrorDto
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

