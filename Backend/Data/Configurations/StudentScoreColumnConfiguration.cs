using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class StudentScoreColumnConfiguration : IEntityTypeConfiguration<StudentScoreColumn>
    {
        public void Configure(EntityTypeBuilder<StudentScoreColumn> entity)
        {
            entity.HasKey(e => new { e.StudentScoreId, e.ScoreKey });

            entity.ToTable("student_score_columns");

            entity.Property(e => e.StudentScoreId).HasColumnName("student_score_id");
            entity.Property(e => e.ScoreKey)
                .HasMaxLength(80)
                .HasColumnName("score_key");
            entity.Property(e => e.ScoreValue)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("score_value");

            entity.HasOne(d => d.StudentScore).WithMany(p => p.StudentScoreColumns)
                .HasForeignKey(d => d.StudentScoreId)
                .HasConstraintName("FK_student_score_columns_scores");
        }
    }
}

