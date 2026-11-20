using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class ClassConfiguration : IEntityTypeConfiguration<TrainingClass>
    {
        public void Configure(EntityTypeBuilder<TrainingClass> entity)
        {
            entity.ToTable("classes");
            entity.HasIndex(e => e.ClassCode, "UQ_classes_class_code").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassCode).HasMaxLength(50).HasColumnName("class_code");
            entity.Property(e => e.ClassName).HasMaxLength(150).HasColumnName("class_name");
            entity.Property(e => e.ProgramName).HasMaxLength(100).HasColumnName("program_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Capacity).HasDefaultValue(25).HasColumnName("capacity");
            entity.Property(e => e.Location).HasMaxLength(100).HasColumnName("location");
            entity.Property(e => e.Room).HasMaxLength(50).HasColumnName("room");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("PLANNING").HasColumnName("status");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

            entity.HasOne(d => d.Teacher).WithMany()
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_classes_teacher");
        }
    }
}
