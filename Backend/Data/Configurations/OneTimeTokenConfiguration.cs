using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations
{
    public class OneTimeTokenConfiguration : IEntityTypeConfiguration<OneTimeToken>
    {
        public void Configure(EntityTypeBuilder<OneTimeToken> entity)
        {
            entity.ToTable("one_time_tokens");

            entity.HasIndex(e => e.ExpiryDate, "IX_one_time_tokens_expiry");

            entity.HasIndex(e => e.UserId, "IX_one_time_tokens_user_id");

            entity.HasIndex(e => e.TokenHash, "UQ_one_time_tokens_hash").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(256)
                .HasColumnName("token_hash");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.Used).HasColumnName("used");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.OneTimeTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_one_time_tokens_users");
        }
    }
}

