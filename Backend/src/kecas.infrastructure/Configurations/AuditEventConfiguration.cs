namespace kecas.infrastructure.Configurations;

using kecas.domain.audit;
using kecas.domain.users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// DD-11: append-only audit table. No update or delete paths exist anywhere in
/// the codebase; rows are written in the same transaction as the change.
/// </summary>
internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(64);

        // JSON snapshots (before / after).
        builder.Property(a => a.Before)
            .HasColumnType("jsonb");

        builder.Property(a => a.After)
            .HasColumnType("jsonb");

        builder.Property(a => a.OccurredAtUtc);

        // The admin viewer filters by actor, entity and date.
        builder.HasIndex(a => a.OccurredAtUtc);

        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
