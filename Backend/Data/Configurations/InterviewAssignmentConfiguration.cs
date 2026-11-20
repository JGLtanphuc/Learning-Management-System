using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class InterviewAssignmentConfiguration : IEntityTypeConfiguration<InterviewAssignment>
    {
        public void Configure(EntityTypeBuilder<InterviewAssignment> entity)
        {
            entity.ToTable("interview_assignments");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
            entity.Property(e => e.Description).HasMaxLength(1000).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasDefaultValue(45).HasColumnName("duration_minutes");
            entity.Property(e => e.TotalQuestions).HasDefaultValue(10).HasColumnName("total_questions");
            entity.Property(e => e.EasyCount).HasDefaultValue(4).HasColumnName("easy_count");
            entity.Property(e => e.MediumCount).HasDefaultValue(4).HasColumnName("medium_count");
            entity.Property(e => e.HardCount).HasDefaultValue(2).HasColumnName("hard_count");
            entity.Property(e => e.PassingScore).HasColumnType("decimal(4, 2)").HasDefaultValue(7.0m).HasColumnName("passing_score");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("PUBLISHED").HasColumnName("status");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Topic)
                .WithMany()
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_interview_assignments_topics")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Class)
                .WithMany()
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_interview_assignments_classes")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
