using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
    {
        public void Configure(EntityTypeBuilder<InterviewQuestion> entity)
        {
            entity.ToTable("interview_questions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.QuestionText).HasMaxLength(1000).IsRequired().HasColumnName("question_text");
            entity.Property(e => e.SampleAnswer).HasColumnName("sample_answer");
            entity.Property(e => e.Keywords).HasMaxLength(500).HasColumnName("keywords");
            entity.Property(e => e.Difficulty).HasMaxLength(20).HasDefaultValue("MEDIUM").HasColumnName("difficulty");
            entity.Property(e => e.QuestionType).HasMaxLength(30).HasDefaultValue("WRITTEN").HasColumnName("question_type");
            entity.Property(e => e.OptionsJson).HasColumnName("options_json");
            entity.Property(e => e.CorrectOptionId).HasMaxLength(20).HasColumnName("correct_option_id");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0).HasColumnName("display_order");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Topic)
                .WithMany(p => p.Questions)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_interview_questions_topics")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
