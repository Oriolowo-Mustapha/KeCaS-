namespace kecas.infrastructure.Configurations;

using kecas.domain.network;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("zones");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.Code)
            .HasMaxLength(10)
            .IsRequired();

        // Zone codes are the stable public identifier (Appendix A: Z1..Z5).
        builder.HasIndex(z => z.Code).IsUnique();

        builder.Property(z => z.Name)
            .HasMaxLength(120)
            .IsRequired();
    }
}

internal sealed class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("stops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(s => s.Code).IsUnique();

        builder.Property(s => s.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.ZoneId)
            .IsRequired();

        builder.HasOne<Zone>()
            .WithMany()
            .HasForeignKey(s => s.ZoneId);

        builder.Property(s => s.IsRank)
            .IsRequired();
    }
}

internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes", table =>
        {
            table.HasCheckConstraint(
                "ck_routes_category",
                "category IN ('KEKE', 'CAB', 'SHUTTLE')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(r => r.Category)
            // Storage matches the vehicle categories and the CHECK constraint.
            .HasConversion(
                category => EnumStorage.RouteCategoryTo(category),
                value => EnumStorage.RouteCategoryFrom(value))
            .HasColumnName("category")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(300);

        builder.Property(r => r.IsActive)
            .IsRequired();

        // Ordered stop list; the (route_id, seq) uniqueness is on the join
        // entity configuration.
        builder.HasMany(r => r.Stops)
            .WithOne()
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stops are loaded with the route for public projections (DD-10).
        builder.Navigation(r => r.Stops).AutoInclude();
    }
}

internal sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops", table =>
        {
            table.HasCheckConstraint(
                "ck_route_stops_seq",
                "seq >= 1");

            table.HasCheckConstraint(
                "ck_route_stops_mins",
                "mins_from_start >= 0");
        });

        // Composite key: a stop appears at most once per route.
        builder.HasKey(s => new { s.RouteId, s.StopId });

        builder.Property(s => s.StopId)
            .IsRequired();

        builder.HasOne<Stop>()
            .WithMany()
            .HasForeignKey(s => s.StopId);

        builder.Property(s => s.Seq)
            .IsRequired();

        // A route cannot have two stops at the same position.
        builder.HasIndex(s => new { s.RouteId, s.Seq })
            .IsUnique();

        builder.Property(s => s.MinsFromStart)
            .IsRequired();
    }
}
