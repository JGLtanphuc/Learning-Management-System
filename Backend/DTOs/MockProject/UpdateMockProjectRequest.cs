using System.Collections.Generic;

namespace Backend.DTOs.MockProject
{
    public class UpdateMockProjectRequest
    {
        public string ProjectName { get; set; } = null!;
        public List<long> StudentIds { get; set; } = new();
        public string? MentorName { get; set; }
        public string? TechStack { get; set; }
        public string? RepoUrl { get; set; }
        public string Status { get; set; } = "PLANNING";
        public decimal? FinalScore { get; set; }
        public bool IsActive { get; set; }
    }
}

