namespace Backend.Common.Configurations
{
    public class GeminiSettings
    {
        public const string SectionName = "GeminiSettings";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.0-flash-exp";
    }
}
