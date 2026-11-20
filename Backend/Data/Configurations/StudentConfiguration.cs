using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> entity)
        {
            entity.ToTable("students");
            entity.HasIndex(e => e.StudentCode, "UQ_students_student_code").IsUnique();
            entity.HasIndex(e => e.Email, "UQ_students_email").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentCode).HasMaxLength(30).HasColumnName("student_code");
            entity.Property(e => e.FullName).HasMaxLength(120).HasColumnName("full_name");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.University).HasMaxLength(150).HasColumnName("university");
            entity.Property(e => e.Major).HasMaxLength(100).HasColumnName("major");
            entity.Property(e => e.Gpa).HasColumnType("decimal(4, 2)").HasColumnName("gpa");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("ACTIVE").HasColumnName("status");
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("joined_at");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.MockProjectId).HasColumnName("mock_project_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_students_user");

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_students_classes");

            entity.HasOne(d => d.MockProject).WithMany(p => p.Students)
                .HasForeignKey(d => d.MockProjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_students_mock_projects");
        }
    }
}
