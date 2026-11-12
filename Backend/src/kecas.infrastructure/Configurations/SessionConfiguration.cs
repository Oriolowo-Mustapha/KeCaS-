namespace kecas.infrastructure.Configurations;

using kecas.domain.users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Sessions live in the database (§8.2) so deactivation and password changes
/// can end them immediately. Only the token's SHA-256 hash is ever stored.
/// </summary>
internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        // SHA-256 hex digest length.
        builder.Property(s => s.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Token lookup happens on every authenticated request.
        builder.HasIndex(s => s.TokenHash).IsUnique();

        builder.Property(s => s.CreatedAtUtc);

        builder.Property(s => s.ExpiresAtUtc);

        builder.Property(s => s.IdleExpiresAtUtc);

        builder.Property(s => s.RevokedAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
