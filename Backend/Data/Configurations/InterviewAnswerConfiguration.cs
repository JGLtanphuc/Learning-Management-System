using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class InterviewAnswerConfiguration : IEntityTypeConfiguration<InterviewAnswer>
    {
        public void Configure(EntityTypeBuilder<InterviewAnswer> entity)
        {
            entity.ToTable("interview_answers");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AnswerText).IsRequired().HasColumnName("answer_text");
            entity.Property(e => e.AnswerMode).HasMaxLength(20).HasDefaultValue("TEXT").HasColumnName("answer_mode");
            entity.Property(e => e.IsFlagged).HasDefaultValue(false).HasColumnName("is_flagged");
            entity.Property(e => e.SelectedOptionId).HasMaxLength(20).HasColumnName("selected_option_id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.TeacherNote).HasMaxLength(1000).HasColumnName("teacher_note");
            entity.Property(e => e.TeacherScore).HasColumnType("decimal(4, 2)").HasColumnName("teacher_score");
            entity.Property(e => e.Score).HasColumnType("decimal(4, 2)").HasColumnName("score");
            entity.Property(e => e.AiScore).HasColumnType("decimal(4, 2)").HasColumnName("ai_score");
            entity.Property(e => e.AiFeedback).HasColumnName("ai_feedback");
            entity.Property(e => e.AiVerdict).HasMaxLength(50).HasColumnName("ai_verdict");
            entity.Property(e => e.GradedBy).HasMaxLength(50).HasColumnName("graded_by");
            entity.Property(e => e.GradedAt).HasColumnName("graded_at");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Question)
                .WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_interview_answers_questions")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_interview_answers_users");

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Answers)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_interview_answers_sessions")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
