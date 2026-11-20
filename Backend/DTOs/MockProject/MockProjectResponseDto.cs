using System;
using System.Collections.Generic;
using Backend.DTOs.Common;

namespace Backend.DTOs.MockProject
{
    public class MockProjectResponseDto : IHasId
    {
        public long Id { get; set; }
        public string ProjectName { get; set; } = null!;
        public List<long> StudentIds { get; set; } = new();
        public List<string> StudentNames { get; set; } = new();
        public List<StudentSimpleDto> Members { get; set; } = new();
        public string? MentorName { get; set; }
        public string? TechStack { get; set; }
        public string? RepoUrl { get; set; }
        public string Status { get; set; } = "PLANNING";
        public decimal? FinalScore { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

