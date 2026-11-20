using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
    {
        public void Configure(EntityTypeBuilder<Allocation> entity)
        {
            entity.ToTable("allocations");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TargetUnit).HasMaxLength(100).HasColumnName("target_unit");
            entity.Property(e => e.ProjectName).HasMaxLength(150).HasColumnName("project_name");
            entity.Property(e => e.RoleTitle).HasMaxLength(100).HasDefaultValue("Fresher Developer").HasColumnName("role_title");
            entity.Property(e => e.MatchScore).HasColumnType("decimal(5, 2)").HasColumnName("match_score");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("PROPOSED").HasColumnName("status");
            entity.Property(e => e.InterviewDate).HasColumnName("interview_date");
            entity.Property(e => e.Note).HasMaxLength(500).HasColumnName("note");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Student).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_allocations_students");
        }
    }
}

