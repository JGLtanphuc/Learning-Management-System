namespace Backend.DTOs.Menu
{
    public class UpdateModuleRequest
    {
        public long ModuleGroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; } = 0;
        public string? RequiredPermission { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
