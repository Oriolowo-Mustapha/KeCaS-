namespace kecas.infrastructure.Configurations;

using kecas.domain.common;
using kecas.domain.fares;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class FareConfiguration : IEntityTypeConfiguration<Fare>
{
    public void Configure(EntityTypeBuilder<Fare> builder)
    {
        builder.ToTable("fares", table =>
        {
            // §5.2: only the five lifecycle states are valid.
            table.HasCheckConstraint(
                "ck_fares_status",
                "status IN ('DRAFT', 'PENDING_APPROVAL', 'PUBLISHED', 'REJECTED', 'SUPERSEDED')");

            // Scope: a whole route XOR a category-tagged from/to zone pair
            // (§4.2, FR-08; §6.3 shows the category on the fare).
            table.HasCheckConstraint(
                "ck_fares_scope",
                "(route_id IS NOT NULL AND category IS NULL AND from_zone_id IS NULL AND to_zone_id IS NULL) "
                + "OR (route_id IS NULL AND from_zone_id IS NOT NULL AND to_zone_id IS NOT NULL AND category IS NOT NULL)");

            table.HasCheckConstraint(
                "ck_fares_category",
                "category IS NULL OR category IN ('KEKE', 'CAB', 'SHUTTLE')");

            // Zone-pair fares travel between two DIFFERENT zones.
            table.HasCheckConstraint(
                "ck_fares_zone_pair",
                "from_zone_id IS NULL OR from_zone_id <> to_zone_id");

            table.HasCheckConstraint(
                "ck_fares_amount",
                "amount_kobo > 0");

            // Inclusive end date cannot precede the start date.
            table.HasCheckConstraint(
                "ck_fares_effective",
                "effective_to IS NULL OR effective_to >= effective_from");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.RouteId);

        builder.HasOne<kecas.domain.network.Route>()
            .WithMany()
            .HasForeignKey(f => f.RouteId);

        builder.Property(f => f.Category)
            // Zone-pair fares carry the category they price (§6.3).
            .HasConversion(
                category => EnumStorage.RouteCategoryTo(category!.Value),
                value => EnumStorage.RouteCategoryFrom(value))
            .HasColumnName("category")
            .HasMaxLength(10);

        builder.Property(f => f.FromZoneId);

        builder.HasOne<kecas.domain.network.Zone>()
            .WithMany()
            .HasForeignKey(f => f.FromZoneId);

        builder.Property(f => f.ToZoneId);

        builder.HasOne<kecas.domain.network.Zone>()
            .WithMany()
            .HasForeignKey(f => f.ToZoneId);

        builder.Property(f => f.Amount)
            // Money is stored as whole kobo (never floating point, §3.1).
            .HasConversion(
                money => money.AmountInKobo,
                value => new Money(value))
            .HasColumnName("amount_kobo")
            .IsRequired();

        builder.Property(f => f.EffectiveFrom)
            .IsRequired();

        builder.Property(f => f.EffectiveTo);

        builder.Property(f => f.ProposedBy)
            .IsRequired();

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(f => f.ProposedBy);

        builder.Property(f => f.ApprovedBy);

        builder.HasOne<kecas.domain.users.User>()
            .WithMany()
            .HasForeignKey(f => f.ApprovedBy);

        builder.Property(f => f.ApprovedAtUtc);

        builder.Property(f => f.Status)
            // Storage matches the CHECK constraint (§5.2 uppercase values).
            .HasConversion(
                status => EnumStorage.FareStatusTo(status),
                value => EnumStorage.FareStatusFrom(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.SupersedesId);

        builder.HasOne<Fare>()
            .WithMany()
            .HasForeignKey(f => f.SupersedesId);

        // Only one PUBLISHED fare per scope at a time (§5.3): zone-pair fares
        // compete per category (a KEKE Z3→Z1 fare and a CAB Z3→Z1 fare can
        // both be published); route fares compete per route.
        builder.HasIndex(f => f.RouteId)
            .IsUnique()
            .HasFilter("\"status\" = 'PUBLISHED'");

        builder.HasIndex(f => new { f.FromZoneId, f.ToZoneId, f.Category })
            .IsUnique()
            .HasFilter("\"status\" = 'PUBLISHED'");

        builder.HasIndex(f => f.Status);
    }
}
