namespace kecas.infrastructure.Configurations;

using kecas.domain.users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => new Email(value))
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        // NFR-07: a user's email is unique.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.Status)
            // Storage values match the design doc: ACTIVE, DISABLED.
            .HasConversion(
                status => EnumStorage.UserStatusTo(status),
                value => EnumStorage.UserStatusFrom(value))
            .HasMaxLength(20);

        builder.Property(u => u.LastLoginAt);

        // §8.2 lockout policy: 5 failed sign-ins within 15 minutes.
        builder.Property(u => u.FailedLoginCount);

        builder.Property(u => u.LockoutUntil);

        // One user can hold more than one role (design doc §4.2).
        builder.HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity("user_roles");
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasMany(r => r.Permissions)
            .WithMany()
            .UsingEntity("role_permissions");
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(300);
    }
}
