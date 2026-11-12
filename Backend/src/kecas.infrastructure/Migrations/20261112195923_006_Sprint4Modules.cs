using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kecas.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _006_Sprint4Modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dropoff_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    ride_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_party_size", "party_size BETWEEN 1 AND 20");
                    table.CheckConstraint("ck_bookings_status", "status IN ('REQUESTED', 'CONFIRMED', 'COMPLETED', 'REJECTED', 'CANCELLED', 'NO_SHOW')");
                    table.ForeignKey(
                        name: "fk_bookings_stops_dropoff_stop_id",
                        column: x => x.dropoff_stop_id,
                        principalTable: "stops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bookings_stops_pickup_stop_id",
                        column: x => x.pickup_stop_id,
                        principalTable: "stops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bookings_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_users_decided_by",
                        column: x => x.decided_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bookings_users_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    contact = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback", x => x.id);
                    table.CheckConstraint("ck_feedback_kind", "kind IN ('COMPLAINT', 'SUGGESTION', 'PRAISE')");
                    table.CheckConstraint("ck_feedback_rating", "rating IS NULL OR rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_feedback_status", "status IN ('NEW', 'REVIEWED', 'RESOLVED')");
                    table.ForeignKey(
                        name: "fk_feedback_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    audience = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    publish_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notices", x => x.id);
                    table.CheckConstraint("ck_notices_audience", "audience IN ('EVERYONE', 'PASSENGERS', 'DRIVERS', 'STAFF')");
                    table.CheckConstraint("ck_notices_category", "category IN ('GENERAL', 'SERVICE', 'SCHEDULE', 'ROUTE')");
                    table.CheckConstraint("ck_notices_expiry_after_publish", "expires_at_utc IS NULL OR expires_at_utc > publish_at_utc");
                    table.CheckConstraint("ck_notices_status", "status IN ('DRAFT', 'PUBLISHED', 'EXPIRED')");
                    table.ForeignKey(
                        name: "fk_notices_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_kobo = table.Column<int>(type: "integer", nullable: false),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount_positive", "amount_kobo > 0");
                    table.CheckConstraint("ck_payments_method", "method IN ('CASH', 'TRANSFER', 'POS', 'OTHER')");
                    table.CheckConstraint("ck_payments_scope", "(booking_id IS NULL) <> (trip_id IS NULL)");
                    table.CheckConstraint("ck_payments_status", "status IN ('PENDING', 'SUCCESSFUL', 'FAILED', 'REFUNDED')");
                    table.ForeignKey(
                        name: "fk_payments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_booking_code",
                table: "bookings",
                column: "booking_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_decided_by",
                table: "bookings",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_dropoff_stop_id",
                table: "bookings",
                column: "dropoff_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_passenger_id",
                table: "bookings",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_pickup_stop_id",
                table: "bookings",
                column: "pickup_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_ride_code",
                table: "bookings",
                column: "ride_code",
                unique: true,
                filter: "ride_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_created_at_utc",
                table: "bookings",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_trip_id",
                table: "bookings",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_status_created_at_utc",
                table: "feedback",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_trip_id",
                table: "feedback",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_notices_created_by",
                table: "notices",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_notices_status_publish_at_utc",
                table: "notices",
                columns: new[] { "status", "publish_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_booking_id",
                table: "payments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_recorded_by",
                table: "payments",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status_recorded_at_utc",
                table: "payments",
                columns: new[] { "status", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_trip_id",
                table: "payments",
                column: "trip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "notices");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
