using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kecas.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _004_NetworkFares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                    table.CheckConstraint("ck_routes_category", "category IN ('KEKE', 'CAB', 'SHUTTLE')");
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    from_zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_kobo = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    proposed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supersedes_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fares", x => x.id);
                    table.CheckConstraint("ck_fares_amount", "amount_kobo > 0");
                    table.CheckConstraint("ck_fares_category", "category IS NULL OR category IN ('KEKE', 'CAB', 'SHUTTLE')");
                    table.CheckConstraint("ck_fares_effective", "effective_to IS NULL OR effective_to >= effective_from");
                    table.CheckConstraint("ck_fares_scope", "(route_id IS NOT NULL AND category IS NULL AND from_zone_id IS NULL AND to_zone_id IS NULL) OR (route_id IS NULL AND from_zone_id IS NOT NULL AND to_zone_id IS NOT NULL AND category IS NOT NULL)");
                    table.CheckConstraint("ck_fares_status", "status IN ('DRAFT', 'PENDING_APPROVAL', 'PUBLISHED', 'REJECTED', 'SUPERSEDED')");
                    table.CheckConstraint("ck_fares_zone_pair", "from_zone_id IS NULL OR from_zone_id <> to_zone_id");
                    table.ForeignKey(
                        name: "fk_fares_fares_supersedes_id",
                        column: x => x.supersedes_id,
                        principalTable: "fares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fares_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fares_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fares_users_proposed_by",
                        column: x => x.proposed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fares_zones_from_zone_id",
                        column: x => x.from_zone_id,
                        principalTable: "zones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fares_zones_to_zone_id",
                        column: x => x.to_zone_id,
                        principalTable: "zones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_rank = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stops", x => x.id);
                    table.ForeignKey(
                        name: "fk_stops_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                columns: table => new
                {
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    mins_from_start = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_stops", x => new { x.route_id, x.stop_id });
                    table.CheckConstraint("ck_route_stops_mins", "mins_from_start >= 0");
                    table.CheckConstraint("ck_route_stops_seq", "seq >= 1");
                    table.ForeignKey(
                        name: "fk_route_stops_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_route_stops_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shifts_rank_stop_id",
                table: "shifts",
                column: "rank_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_fares_approved_by",
                table: "fares",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_fares_from_zone_id_to_zone_id_category",
                table: "fares",
                columns: new[] { "from_zone_id", "to_zone_id", "category" },
                unique: true,
                filter: "\"status\" = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "ix_fares_proposed_by",
                table: "fares",
                column: "proposed_by");

            migrationBuilder.CreateIndex(
                name: "ix_fares_route_id",
                table: "fares",
                column: "route_id",
                unique: true,
                filter: "\"status\" = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "ix_fares_status",
                table: "fares",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_fares_supersedes_id",
                table: "fares",
                column: "supersedes_id");

            migrationBuilder.CreateIndex(
                name: "ix_fares_to_zone_id",
                table: "fares",
                column: "to_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_stops_route_id_seq",
                table: "route_stops",
                columns: new[] { "route_id", "seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_stops_stop_id",
                table: "route_stops",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_code",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stops_code",
                table: "stops",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stops_zone_id",
                table: "stops",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_zones_code",
                table: "zones",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_shifts_stops_rank_stop_id",
                table: "shifts",
                column: "rank_stop_id",
                principalTable: "stops",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shifts_stops_rank_stop_id",
                table: "shifts");

            migrationBuilder.DropTable(
                name: "fares");

            migrationBuilder.DropTable(
                name: "route_stops");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "zones");

            migrationBuilder.DropIndex(
                name: "ix_shifts_rank_stop_id",
                table: "shifts");
        }
    }
}
