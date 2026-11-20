using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class MockProjectConfiguration : IEntityTypeConfiguration<MockProject>
    {
        public void Configure(EntityTypeBuilder<MockProject> entity)
        {
            entity.ToTable("mock_projects");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectName).HasMaxLength(150).HasColumnName("project_name");
            entity.Property(e => e.MentorName).HasMaxLength(100).HasColumnName("mentor_name");
            entity.Property(e => e.TechStack).HasMaxLength(255).HasColumnName("tech_stack");
            entity.Property(e => e.RepoUrl).HasMaxLength(500).HasColumnName("repo_url");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("PLANNING").HasColumnName("status");
            entity.Property(e => e.FinalScore).HasColumnType("decimal(4, 2)").HasColumnName("final_score");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        }
    }
}

