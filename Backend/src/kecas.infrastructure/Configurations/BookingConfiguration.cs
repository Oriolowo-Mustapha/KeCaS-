namespace kecas.infrastructure.Configurations;

using kecas.domain.bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", table =>
        {
            // Lifecycle per §5.2: REQUESTED → CONFIRMED → COMPLETED, or
            // REJECTED / CANCELLED / NO_SHOW.
            table.HasCheckConstraint(
                "ck_bookings_status",
                "status IN ('REQUESTED', 'CONFIRMED', 'COMPLETED', 'REJECTED', 'CANCELLED', 'NO_SHOW')");

            // BR-09 sanity bound at the column level; the handler enforces
            // the real capacity against the vehicle.
            table.HasCheckConstraint(
                "ck_bookings_party_size",
                "party_size BETWEEN 1 AND 20");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingCode)
            // BR-01: unique human-facing code, e.g. BKG-3F91A2C7.
            .HasColumnName("booking_code")
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(b => b.BookingCode).IsUnique();

        builder.Property(b => b.PassengerId)
            .HasColumnName("passenger_id")
            .IsRequired();

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(b => b.PassengerId);

        builder.Property(b => b.TripId)
            .IsRequired();

        builder.HasOne<kecas.domain.trips.Trip>()
            .WithMany()
            .HasForeignKey(b => b.TripId);

        builder.Property(b => b.PickupStopId)
            .HasColumnName("pickup_stop_id");

        builder.HasOne<kecas.domain.network.Stop>()
            .WithMany()
            .HasForeignKey(b => b.PickupStopId);

        builder.Property(b => b.DropoffStopId)
            .HasColumnName("dropoff_stop_id");

        builder.HasOne<kecas.domain.network.Stop>()
            .WithMany()
            .HasForeignKey(b => b.DropoffStopId);

        builder.Property(b => b.PartySize)
            .HasColumnName("party_size")
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion(
                status => status.ToStorageValue(),
                value => BookingStatusExtensions.BookingStatusFrom(value))
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(b => b.RideCode)
            .HasColumnName("ride_code")
            .HasMaxLength(16);

        builder.HasIndex(b => b.RideCode)
            // §7.4: ride codes exist only on confirmed bookings.
            .IsUnique()
            .HasFilter("ride_code IS NOT NULL");

        builder.Property(b => b.DecidedBy)
            .HasColumnName("decided_by");

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(b => b.DecidedBy);

        builder.Property(b => b.DecisionReason)
            .HasColumnName("decision_reason")
            .HasMaxLength(500);

        builder.Property(b => b.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Board views: by status over time, and per trip.
        builder.HasIndex(b => new { b.Status, b.CreatedAtUtc });
        builder.HasIndex(b => b.TripId);
    }
}
