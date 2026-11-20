using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class InterviewSessionConfiguration : IEntityTypeConfiguration<InterviewSession>
    {
        public void Configure(EntityTypeBuilder<InterviewSession> entity)
        {
            entity.ToTable("interview_sessions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("IN_PROGRESS").HasColumnName("status");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(4, 2)").HasColumnName("total_score");
            entity.Property(e => e.AiAverageScore).HasColumnType("decimal(4, 2)").HasColumnName("ai_average_score");
            entity.Property(e => e.TeacherScore).HasColumnType("decimal(4, 2)").HasColumnName("teacher_score");
            entity.Property(e => e.OverallVerdict).HasMaxLength(50).HasColumnName("overall_verdict");
            entity.Property(e => e.OverallSummary).HasColumnName("overall_summary");
            entity.Property(e => e.TeacherFeedback).HasColumnName("teacher_feedback");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Assignment)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("FK_interview_sessions_assignments")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_interview_sessions_users")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
