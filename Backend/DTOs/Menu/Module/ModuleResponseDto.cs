using System;
using Backend.DTOs.Common;

namespace Backend.DTOs.Menu
{
    public class ModuleResponseDto : IHasId
    {
        public long Id { get; set; }
        public long ModuleGroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public string? RequiredPermission { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }
}
