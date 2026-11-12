namespace kecas.infrastructure.Configurations;

using kecas.domain.payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table =>
        {
            // BR-08 statuses, and DD-08 methods (no gateway).
            table.HasCheckConstraint(
                "ck_payments_status",
                "status IN ('PENDING', 'SUCCESSFUL', 'FAILED', 'REFUNDED')");

            table.HasCheckConstraint(
                "ck_payments_method",
                "method IN ('CASH', 'TRANSFER', 'POS', 'OTHER')");

            // Payments are real money.
            table.HasCheckConstraint(
                "ck_payments_amount_positive",
                "amount_kobo > 0");

            // DD-08: exactly one of booking_id / trip_id must be set.
            table.HasCheckConstraint(
                "ck_payments_scope",
                "(booking_id IS NULL) <> (trip_id IS NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BookingId)
            .HasColumnName("booking_id");

        builder.HasOne<kecas.domain.bookings.Booking>()
            .WithMany()
            .HasForeignKey(p => p.BookingId);

        builder.Property(p => p.TripId);

        builder.HasOne<kecas.domain.trips.Trip>()
            .WithMany()
            .HasForeignKey(p => p.TripId);

        builder.Property(p => p.Amount)
            .HasConversion(
                money => money.AmountInKobo,
                value => new Money(value))
            .HasColumnName("amount_kobo")
            .IsRequired();

        builder.Property(p => p.Method)
            .HasConversion(
                method => method.ToStorageValue(),
                value => PaymentEnums.PaymentMethodFrom(value))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion(
                status => status.ToStorageValue(),
                value => PaymentEnums.PaymentStatusFrom(value))
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(p => p.Reference)
            .HasMaxLength(100);

        builder.Property(p => p.RecordedBy)
            .HasColumnName("recorded_by");

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(p => p.RecordedBy);

        builder.Property(p => p.RecordedAtUtc)
            .HasColumnName("recorded_at_utc")
            .IsRequired();

        // Ledger views: by status over time, and per booking/trip.
        builder.HasIndex(p => new { p.Status, p.RecordedAtUtc });
        builder.HasIndex(p => p.BookingId);
        builder.HasIndex(p => p.TripId);
    }
}
