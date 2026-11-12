using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kecas.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _005_Trips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                    table.CheckConstraint("ck_trips_cancel_reason", "(status = 'CANCELLED' AND cancel_reason IS NOT NULL) OR (status <> 'CANCELLED' AND cancel_reason IS NULL)");
                    table.CheckConstraint("ck_trips_end_after_start", "ends_at > starts_at");
                    table.CheckConstraint("ck_trips_source", "source IN ('LOGGED', 'SCHEDULED', 'BOOKING')");
                    table.CheckConstraint("ck_trips_status", "status IN ('SCHEDULED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_trips_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trips_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_trips_driver_id",
                table: "trips",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_route_id_starts_at",
                table: "trips",
                columns: new[] { "route_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_status_starts_at",
                table: "trips",
                columns: new[] { "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_trip_code",
                table: "trips",
                column: "trip_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_vehicle_id",
                table: "trips",
                column: "vehicle_id");

            // BR-03 / DD-02: no overlapping live trips for a driver or a
            // vehicle. EF cannot express exclusion constraints, so they are
            // added as raw SQL; the friendly 409 TRIP_OVERLAP is raised by the
            // handler first, these remain the final guard.
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE trips ADD CONSTRAINT ex_trips_driver_overlap
                    EXCLUDE USING gist
                        (driver_id WITH =, tstzrange(starts_at, ends_at) WITH &&)
                    WHERE (status IN ('SCHEDULED', 'IN_PROGRESS'));

                ALTER TABLE trips ADD CONSTRAINT ex_trips_vehicle_overlap
                    EXCLUDE USING gist
                        (vehicle_id WITH =, tstzrange(starts_at, ends_at) WITH &&)
                    WHERE (vehicle_id IS NOT NULL AND status IN ('SCHEDULED', 'IN_PROGRESS'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE trips DROP CONSTRAINT IF EXISTS ex_trips_vehicle_overlap;");
            migrationBuilder.Sql("ALTER TABLE trips DROP CONSTRAINT IF EXISTS ex_trips_driver_overlap;");

            migrationBuilder.DropTable(
                name: "trips");
        }
    }
}
