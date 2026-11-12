namespace kecas.infrastructure.Configurations;

using kecas.domain.feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedback", table =>
        {
            // FR-21 kinds and the triage status chain NEW → REVIEWED → RESOLVED.
            table.HasCheckConstraint(
                "ck_feedback_kind",
                "kind IN ('COMPLAINT', 'SUGGESTION', 'PRAISE')");

            table.HasCheckConstraint(
                "ck_feedback_status",
                "status IN ('NEW', 'REVIEWED', 'RESOLVED')");

            table.HasCheckConstraint(
                "ck_feedback_rating",
                "rating IS NULL OR rating BETWEEN 1 AND 5");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.TripId);

        builder.HasOne<kecas.domain.trips.Trip>()
            .WithMany()
            .HasForeignKey(f => f.TripId);

        builder.Property(f => f.Kind)
            .HasConversion(
                kind => kind.ToStorageValue(),
                value => FeedbackEnums.FeedbackKindFrom(value))
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(f => f.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(f => f.Rating)
            .IsRequired(false);

        builder.Property(f => f.Contact)
            .HasMaxLength(120);

        builder.Property(f => f.Status)
            .HasConversion(
                status => status.ToStorageValue(),
                value => FeedbackEnums.FeedbackStatusFrom(value))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Triage board views.
        builder.HasIndex(f => new { f.Status, f.CreatedAtUtc });
    }
}
