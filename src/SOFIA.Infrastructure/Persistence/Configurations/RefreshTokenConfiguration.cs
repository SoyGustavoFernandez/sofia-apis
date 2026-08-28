using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        _ = builder.ToTable("RefreshTokens");

        _ = builder.HasKey(rt => rt.Id);

        _ = builder.Property(rt => rt.CuentaId).IsRequired();

        _ = builder.Property(rt => rt.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        _ = builder.Property(rt => rt.ExpiresAt).IsRequired();

        _ = builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(rt => rt.RevokedAt);

        _ = builder.HasIndex(rt => rt.TokenHash).IsUnique();
        _ = builder.HasIndex(rt => rt.CuentaId);

        _ = builder.HasOne<Cuenta>()
            .WithMany()
            .HasForeignKey(rt => rt.CuentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
