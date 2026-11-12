namespace kecas.infrastructure.Seeding;

using System.Diagnostics;
using kecas.application;
using kecas.application.abstractions.authentication;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.notices;
using kecas.domain.users;
using kecas.infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Idempotent seed data (design doc Appendix A + §8.1 permission matrix).
/// Run with:  dotnet run --project src/kecas.api -- seed
/// Safe to run repeatedly: every insert first checks whether the row exists.
/// The test accounts' password comes from the SEED_USERS_PASSWORD environment
/// variable and is never committed.
/// </summary>
public static class SeedRunner
{
    public static async Task RunAsync()
    {
        Console.WriteLine("KeCaS seed starting...");
        var stopwatch = Stopwatch.StartNew();

        DotEnv.Load();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider();

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();

        Console.WriteLine("Applying pending migrations (if any)...");
        await context.Database.MigrateAsync();

        var permissions = await SeedPermissionsAsync(context);
        var roles = await SeedRolesAsync(context, permissions);
        await SeedOperatorsAsync(context);
        await SeedUsersAsync(context, hasher, roles);
        await SeedNetworkAsync(context);
        await SeedVehiclesAsync(context);
        await SeedNoticesAsync(context);

        stopwatch.Stop();
        Console.WriteLine($"KeCaS seed finished in {stopwatch.Elapsed.TotalSeconds:F1}s.");
    }

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(
        ApplicationDbContext context)
    {
        Console.WriteLine("Seeding permissions (§8.1)...");
        var definitions = new (string Code, string Description)[]
        {
            ("vehicles:view", "View vehicles and drivers"),
            ("vehicles:manage", "Create or edit vehicles and drivers; change vehicle status"),
            ("drivers:checkin", "Check drivers in or out on their behalf"),
            ("network:manage", "Create or edit stops and routes"),
            ("fares:propose", "Propose a fare"),
            ("fares:approve", "Approve a fare"),
            ("trips:manage", "Create, edit, cancel and assign trips"),
            ("bookings:manage", "Confirm or reject bookings; publish notices"),
            ("payments:record", "Record payments"),
            ("reports:view", "View and export reports"),
            ("users:manage", "Manage users and roles; read the audit log"),
        };

        var existing = await context.Permissions.ToListAsync();
        var byCode = existing.ToDictionary(p => p.Code, p => p);

        foreach (var (code, description) in definitions)
        {
            if (byCode.ContainsKey(code))
            {
                continue;
            }

            var created = Permission.Create(code, description);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: permission '{code}' failed validation ({created.Error.Code}).");
            }

            byCode[code] = created.Value;
            context.Permissions.Add(created.Value);
        }

        await context.SaveChangesAsync();
        return byCode;
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(
        ApplicationDbContext context,
        Dictionary<string, Permission> permissions)
    {
        Console.WriteLine("Seeding roles (§8.1)...");
        var definitions = new (string Code, string Name, string[] Permissions)[]
        {
            ("ADMIN", "Administrator",
                ["vehicles:view", "vehicles:manage", "drivers:checkin", "network:manage",
                 "fares:propose", "fares:approve", "trips:manage", "bookings:manage",
                 "payments:record", "reports:view", "users:manage"]),
            ("MANAGER", "Transport Manager",
                ["vehicles:view", "vehicles:manage", "drivers:checkin", "network:manage",
                 "fares:propose", "fares:approve", "trips:manage", "bookings:manage",
                 "payments:record", "reports:view"]),
            ("OPERATOR", "Transport Operator",
                ["vehicles:view", "vehicles:manage", "drivers:checkin", "payments:record",
                 "reports:view"]),
            ("DRIVER", "Driver", []),
            ("PASSENGER", "Passenger", []),
        };

        var existing = await context.Roles.Include(r => r.Permissions).ToListAsync();
        var byCode = existing.ToDictionary(r => r.Code, r => r);

        foreach (var (code, name, granted) in definitions)
        {
            if (!byCode.TryGetValue(code, out var role))
            {
                var created = Role.Create(code, name);
                if (created.IsFailure)
                {
                    throw new InvalidOperationException(
                        $"Seed data error: role '{code}' failed validation ({created.Error.Code}).");
                }

                role = created.Value;
                context.Roles.Add(role);
                byCode[code] = role;
            }

            foreach (var permissionCode in granted)
            {
                var permission = permissions[permissionCode];
                if (!role.HasPermission(permission.Code))
                {
                    role.GrantPermission(permission);
                }
            }
        }

        await context.SaveChangesAsync();
        return byCode;
    }

    private static async Task SeedOperatorsAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding operators (Appendix A)...");
        var definitions = new (string Name, OperatorKind Kind)[]
        {
            ("Operator A", OperatorKind.University),
            ("Operator B", OperatorKind.Cooperative),
            ("Operator C", OperatorKind.Independent),
        };

        foreach (var (name, kind) in definitions)
        {
            var exists = await context.Operators.AnyAsync(o => o.Name == name);
            if (exists)
            {
                continue;
            }

            var created = Operator.Create(name, kind);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: operator '{name}' failed validation ({created.Error.Code}).");
            }

            context.Operators.Add(created.Value);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher hasher,
        Dictionary<string, Role> roles)
    {
        Console.WriteLine("Seeding test users (Appendix A)...");

        var password = Environment.GetEnvironmentVariable("SEED_USERS_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SEED_USERS_PASSWORD is not set. Add it to the .env file before seeding "
                + "(it is the shared password for the kecas.test accounts).");
        }

        var definitions = new (string Email, string FullName, string RoleCode)[]
        {
            ("admin@kecas.test", "KeCaS Administrator", "ADMIN"),
            ("manager@kecas.test", "KeCaS Manager", "MANAGER"),
            ("operator@kecas.test", "Operator A Account", "OPERATOR"),
            ("driver@kecas.test", "KeCaS Driver", "DRIVER"),
            ("passenger@kecas.test", "KeCaS Passenger", "PASSENGER"),
        };

        foreach (var (email, fullName, roleCode) in definitions)
        {
            var emailResult = Email.Create(email);
            if (emailResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: email '{email}' failed validation.");
            }

            var exists = await context.Users.AnyAsync(u => u.Email == emailResult.Value);
            if (exists)
            {
                continue;
            }

            var created = User.Create(fullName, email, hasher.Hash(password));
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: user '{email}' failed validation ({created.Error.Code}).");
            }

            created.Value.AssignRole(roles[roleCode]);
            context.Users.Add(created.Value);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Zones, stops and routes from Appendix A. All placeholders for demos;
    /// zone membership of stops and the route stop sequences are sensible
    /// sample choices. Idempotent by code.
    /// </summary>
    private static async Task SeedNetworkAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding network: zones, stops, routes (Appendix A)...");

        // ---- zones ----
        var zoneDefinitions = new (string Code, string Name)[]
        {
            ("Z1", "Main gate area"),
            ("Z2", "Academic area"),
            ("Z3", "Hostel area"),
            ("Z4", "Staff quarters"),
            ("Z5", "Town side"),
        };

        var zonesByCode = new Dictionary<string, Zone>();
        foreach (var (code, name) in zoneDefinitions)
        {
            var existing = await context.Zones.FirstOrDefaultAsync(z => z.Code == code);
            if (existing is not null)
            {
                zonesByCode[code] = existing;
                continue;
            }

            var created = Zone.Create(code, name);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: zone '{code}' failed validation ({created.Error.Code}).");
            }

            zonesByCode[code] = created.Value;
            context.Zones.Add(created.Value);
        }

        await context.SaveChangesAsync();

        // ---- stops ----
        var stopDefinitions = new (string Code, string Name, string ZoneCode, bool IsRank)[]
        {
            ("S01", "Main Gate", "Z1", true),
            ("S02", "Library", "Z2", false),
            ("S03", "Lecture theatres", "Z2", false),
            ("S04", "Hostel A", "Z3", false),
            ("S05", "Hostel B", "Z3", false),
            ("S06", "Staff quarters", "Z4", false),
            ("S07", "Town park", "Z5", true),
        };

        var stopsByCode = new Dictionary<string, Stop>();
        foreach (var (code, name, zoneCode, isRank) in stopDefinitions)
        {
            var existing = await context.Stops.FirstOrDefaultAsync(s => s.Code == code);
            if (existing is not null)
            {
                stopsByCode[code] = existing;
                continue;
            }

            var created = Stop.Create(code, name, zonesByCode[zoneCode].Id, isRank);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: stop '{code}' failed validation ({created.Error.Code}).");
            }

            stopsByCode[code] = created.Value;
            context.Stops.Add(created.Value);
        }

        await context.SaveChangesAsync();

        // ---- routes (with ordered stop lists) ----
        var routeDefinitions = new (string Code, string Name, RouteCategory Category, string? Description, (string StopCode, int MinsFromStart)[] Stops)[]
        {
            ("KK-1", "Hostel A to Main Gate", RouteCategory.Keke, "Keke shuttle between Hostel A and the Main Gate.",
                [("S04", 0), ("S01", 8)]),
            ("SH-1", "Campus loop", RouteCategory.Shuttle, "Campus shuttle loop serving the main stops.",
                [("S01", 0), ("S02", 3), ("S03", 5), ("S04", 9), ("S05", 11), ("S06", 14)]),
            ("CB-1", "Main Gate to Town park", RouteCategory.Cab, "Cab service from the Main Gate into town.",
                [("S01", 0), ("S07", 15)]),
        };

        foreach (var (code, name, category, description, stops) in routeDefinitions)
        {
            var existing = await context.Routes
                .Include(r => r.Stops)
                .FirstOrDefaultAsync(r => r.Code == code);
            if (existing is not null)
            {
                continue;
            }

            var created = Route.Create(code, name, category, description);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: route '{code}' failed validation ({created.Error.Code}).");
            }

            var setStops = created.Value.SetStops(stops
                .Select((s, index) => (stopsByCode[s.StopCode].Id, index + 1, s.MinsFromStart))
                .ToList());
            if (setStops.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: route '{code}' stop list failed ({setStops.Error.Code}).");
            }

            context.Routes.Add(created.Value);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// The Appendix A fleet: 12 kekes (capacity 3) for Operator A, 5 cabs
    /// (capacity 4) for Operator B and 2 shuttles (capacity 18) for
    /// Operator C. Plates are test values (e.g. KK001FN). Idempotent by code.
    /// </summary>
    private static async Task SeedVehiclesAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding vehicles (Appendix A)...");

        var operatorsByName = (await context.Operators.ToListAsync())
            .ToDictionary(o => o.Name);

        (string Code, VehicleCategory Category, int Capacity, string OperatorName)[] definitions =
        [
            .. Enumerable.Range(1, 12)
                .Select(i => ($"KK-{i:000}", VehicleCategory.Keke, 3, "Operator A")),
            .. Enumerable.Range(1, 5)
                .Select(i => ($"CB-{i:000}", VehicleCategory.Cab, 4, "Operator B")),
            .. Enumerable.Range(1, 2)
                .Select(i => ($"SH-{i:000}", VehicleCategory.Shuttle, 18, "Operator C")),
        ];

        foreach (var (code, category, capacity, operatorName) in definitions)
        {
            var exists = await context.Vehicles.AnyAsync(v => v.Code == new VehicleCode(code));
            if (exists)
            {
                continue;
            }

            // Test plate derived from the code, e.g. KK-001 → KK001FN.
            var plate = code.Replace("-", string.Empty) + "FN";

            var created = Vehicle.Create(code, plate, category, capacity, operatorsByName[operatorName].Id);
            if (created.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Seed data error: vehicle '{code}' failed validation ({created.Error.Code}).");
            }

            context.Vehicles.Add(created.Value);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedNoticesAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding sample notice (FR-22)...");

        var admin = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == new Email("admin@kecas.test"));
        if (admin is null)
        {
            return;
        }

        var exists = await context.Notices
            .AnyAsync(n => n.Title == "Welcome to the KeCaS pilot");
        if (exists)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var created = Notice.Create(
            "Welcome to the KeCaS pilot",
            "The campus transport service is now running on the KeCaS system. "
            + "Check available kekes, cabs and shuttles on the public board, and "
            + "watch this page for schedule changes.",
            NoticeAudience.Everyone,
            NoticeCategory.General,
            publishAtUtc: nowUtc,
            expiresAtUtc: nowUtc.AddDays(7),
            createdBy: admin.Id,
            createdAtUtc: nowUtc);
        if (created.IsFailure)
        {
            throw new InvalidOperationException(
                $"Seed data error: notice failed validation ({created.Error.Code}).");
        }

        created.Value.Publish();

        context.Notices.Add(created.Value);
        await context.SaveChangesAsync();
    }
}
