namespace kecas.infrastructure.Configurations;

using kecas.domain.network;
using kecas.domain.trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips", table =>
        {
            // Status matches the design doc DDL.
            table.HasCheckConstraint(
                "ck_trips_status",
                "status IN ('SCHEDULED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')");

            table.HasCheckConstraint(
                "ck_trips_source",
                "source IN ('LOGGED', 'SCHEDULED', 'BOOKING')");

            // A trip cannot end before it starts.
            table.HasCheckConstraint(
                "ck_trips_end_after_start",
                "ends_at > starts_at");

            // A cancelled trip must carry a reason; live trips must not.
            table.HasCheckConstraint(
                "ck_trips_cancel_reason",
                "(status = 'CANCELLED' AND cancel_reason IS NOT NULL) "
                + "OR (status <> 'CANCELLED' AND cancel_reason IS NULL)");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TripCode)
            // BR-01: unique human-facing code, e.g. TRP-3F91A2C7.
            .HasColumnName("trip_code")
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(t => t.TripCode).IsUnique();

        builder.Property(t => t.RouteId)
            .IsRequired();

        builder.HasOne<Route>()
            .WithMany()
            .HasForeignKey(t => t.RouteId);

        builder.Property(t => t.VehicleId);

        builder.HasOne<kecas.domain.fleet.Vehicle>()
            .WithMany()
            .HasForeignKey(t => t.VehicleId);

        builder.Property(t => t.DriverId);

        builder.HasOne<kecas.domain.fleet.Driver>()
            .WithMany()
            .HasForeignKey(t => t.DriverId);

        builder.Property(t => t.Source)
            // Storage values match the design doc: LOGGED, SCHEDULED, BOOKING.
            .HasConversion(
                source => EnumStorage.TripSourceTo(source),
                value => EnumStorage.TripSourceFrom(value))
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(t => t.Status)
            // Storage values match the CHECK constraint: SCHEDULED,
            // IN_PROGRESS, COMPLETED, CANCELLED.
            .HasConversion(
                status => EnumStorage.TripStatusTo(status),
                value => EnumStorage.TripStatusFrom(value))
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(t => t.StartsAt)
            .IsRequired();

        builder.Property(t => t.EndsAt)
            .IsRequired();

        builder.Property(t => t.CancelReason)
            .HasMaxLength(500);

        // Concurrency token for two-session edits (§4.2, 409 on staleness).
        builder.Property(t => t.RowVersion)
            .IsConcurrencyToken();

        // List views: by route over time, and by status over time.
        builder.HasIndex(t => new { t.RouteId, t.StartsAt });
        builder.HasIndex(t => new { t.Status, t.StartsAt });
    }
}
