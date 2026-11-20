using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class StudentScoreConfiguration : IEntityTypeConfiguration<StudentScore>
    {
        public void Configure(EntityTypeBuilder<StudentScore> entity)
        {
            entity.ToTable("student_scores");

            entity.HasIndex(e => e.StudentCode, "UQ_student_scores_student_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(60)
                .HasColumnName("class_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.OverallScore)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("overall_score");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(30)
                .HasColumnName("student_code");
            entity.Property(e => e.StudentName)
                .HasMaxLength(120)
                .HasColumnName("student_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        }
    }
}

