namespace kecas.infrastructure.Configurations;

using kecas.domain.fleet;
using kecas.domain.users;
using kecas.infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("operators");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(o => o.Kind)
            // Storage values match the design doc: UNIVERSITY, COOPERATIVE,
            // INDEPENDENT.
            .HasConversion(
                kind => EnumStorage.OperatorKindTo(kind),
                value => EnumStorage.OperatorKindFrom(value))
            .HasMaxLength(20);

        builder.Property(o => o.Phone)
            .HasMaxLength(20);

        builder.Property(o => o.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", table =>
        {
            table.HasCheckConstraint(
                "ck_vehicles_category",
                "vehicle_category IN ('KEKE', 'CAB', 'SHUTTLE')");

            table.HasCheckConstraint(
                "ck_vehicles_status",
                "status IN ('ACTIVE', 'INACTIVE', 'MAINTENANCE', 'RETIRED')");

            table.HasCheckConstraint(
                "ck_vehicles_capacity",
                "capacity BETWEEN 1 AND 60");
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code)
            .HasConversion(code => code.Value, value => new VehicleCode(value))
            .HasColumnName("vehicle_code")
            .HasMaxLength(16)
            .IsRequired();

        // BR-01: every vehicle has a unique identifier and unique plate.
        builder.HasIndex(v => v.Code).IsUnique();

        builder.Property(v => v.PlateNumber)
            .HasConversion(plate => plate.Value, value => new PlateNumber(value))
            .HasColumnName("plate_no")
            .HasMaxLength(12)
            .IsRequired();

        builder.HasIndex(v => v.PlateNumber).IsUnique();

        builder.Property(v => v.Category)
            // Storage values match the design doc and the CHECK constraint:
            // KEKE, CAB, SHUTTLE.
            .HasConversion(
                category => EnumStorage.VehicleCategoryTo(category),
                value => EnumStorage.VehicleCategoryFrom(value))
            .HasColumnName("vehicle_category")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(v => v.Capacity)
            .IsRequired();

        builder.Property(v => v.OperatorId)
            .IsRequired();

        builder.HasOne<Operator>()
            .WithMany()
            .HasForeignKey(v => v.OperatorId);

        builder.Property(v => v.Status)
            // Storage values match the CHECK constraint: ACTIVE, INACTIVE,
            // MAINTENANCE, RETIRED.
            .HasConversion(
                status => EnumStorage.VehicleStatusTo(status),
                value => EnumStorage.VehicleStatusFrom(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.StatusChangedAt)
            .IsRequired();

        // Concurrency token for two-session edits (NFR-07, design doc §4.2).
        builder.Property(v => v.RowVersion)
            .IsConcurrencyToken();
    }
}

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId);

        builder.Property(d => d.OperatorId)
            .IsRequired();

        builder.HasOne<Operator>()
            .WithMany()
            .HasForeignKey(d => d.OperatorId);

        // NFR-05: AES-256-GCM encrypted at rest; masked in API responses (FR-06).
        // The licence number column now holds "enc1:..." ciphertext.
        var licenceProtector = new AesGcmLicenceProtector();
        builder.Property(d => d.LicenceNumber)
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                licence => licenceProtector.Encrypt(licence),
                stored => licenceProtector.Decrypt(stored));

        builder.Property(d => d.LicenceExpiry)
            .IsRequired();

        builder.Property(d => d.Status)
            // Storage values match the design doc: ACTIVE, SUSPENDED, RETIRED.
            .HasConversion(
                status => EnumStorage.DriverStatusTo(status),
                value => EnumStorage.DriverStatusFrom(value))
            .HasMaxLength(20);

        builder.HasIndex(d => d.LicenceExpiry);
    }
}

internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts", table =>
        {
            // A shift cannot end before it started.
            table.HasCheckConstraint(
                "ck_shifts_end_after_start",
                "ended_at IS NULL OR ended_at > started_at");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DriverId)
            .IsRequired();

        builder.HasOne<Driver>()
            .WithMany()
            .HasForeignKey(s => s.DriverId);

        builder.Property(s => s.VehicleId)
            .IsRequired();

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(s => s.VehicleId);

        // Rank stop references a stop in the network module (FR-07).
        builder.Property(s => s.RankStopId);

        builder.HasOne<kecas.domain.network.Stop>()
            .WithMany()
            .HasForeignKey(s => s.RankStopId);

        builder.Property(s => s.StartedAt)
            .IsRequired();

        builder.Property(s => s.EndedAt);

        builder.Property(s => s.ClosedBy)
            // Storage values match the design doc: USER or AUTO.
            .HasConversion(
                closedBy => closedBy.HasValue ? EnumStorage.ShiftClosedByTo(closedBy.Value) : null,
                value => value == null ? (ShiftClosedBy?)null : EnumStorage.ShiftClosedByFrom(value))
            .HasMaxLength(10);

        // One open shift per driver and per vehicle (design doc §4.2).
        // The exclusion/overlap guard for BR-03 lives on trips later.
        builder.HasIndex(s => s.DriverId)
            .IsUnique()
            .HasFilter("\"ended_at\" IS NULL");

        builder.HasIndex(s => s.VehicleId)
            .IsUnique()
            .HasFilter("\"ended_at\" IS NULL");

        builder.HasIndex(s => s.StartedAt);
    }
}
