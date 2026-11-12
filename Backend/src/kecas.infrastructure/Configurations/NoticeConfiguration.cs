namespace kecas.infrastructure.Configurations;

using kecas.domain.notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.ToTable("notices", table =>
        {
            // Status matches the lifecycle DRAFT → PUBLISHED → EXPIRED.
            table.HasCheckConstraint(
                "ck_notices_status",
                "status IN ('DRAFT', 'PUBLISHED', 'EXPIRED')");

            table.HasCheckConstraint(
                "ck_notices_audience",
                "audience IN ('EVERYONE', 'PASSENGERS', 'DRIVERS', 'STAFF')");

            table.HasCheckConstraint(
                "ck_notices_category",
                "category IN ('GENERAL', 'SERVICE', 'SCHEDULE', 'ROUTE')");

            // A notice cannot expire before it is published.
            table.HasCheckConstraint(
                "ck_notices_expiry_after_publish",
                "expires_at_utc IS NULL OR expires_at_utc > publish_at_utc");
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.Audience)
            .HasConversion(
                audience => audience.ToStorageValue(),
                value => NoticeEnums.NoticeAudienceFrom(value))
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(n => n.Category)
            .HasConversion(
                category => category.ToStorageValue(),
                value => NoticeEnums.NoticeCategoryFrom(value))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(n => n.PublishAtUtc)
            .HasColumnName("publish_at_utc")
            .IsRequired();

        builder.Property(n => n.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(n => n.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(n => n.CreatedBy);

        builder.Property(n => n.Status)
            .HasConversion(
                status => status.ToStorageValue(),
                value => NoticeStatusExtensions.NoticeStatusFrom(value))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(n => n.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Board views: by status over publish time, and the active window.
        builder.HasIndex(n => new { n.Status, n.PublishAtUtc });
    }
}
