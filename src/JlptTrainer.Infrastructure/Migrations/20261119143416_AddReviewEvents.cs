using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "review_cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "review_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    interval_days = table.Column<int>(type: "integer", nullable: false),
                    next_review_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_review_events_user_id_request_id",
                table: "review_events",
                columns: new[] { "user_id", "request_id" },
                unique: true,
                filter: "request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_review_events_user_id_reviewed_at",
                table: "review_events",
                columns: new[] { "user_id", "reviewed_at" });

            migrationBuilder.Sql("""
                INSERT INTO review_events
                    (id, user_id, review_card_id, grade, reviewed_at, request_id, interval_days, next_review_date)
                SELECT id, user_id, id, NULL, last_reviewed_at, NULL, interval_days, next_review_date
                FROM review_cards
                WHERE last_reviewed_at IS NOT NULL;

                CREATE OR REPLACE FUNCTION get_study_heatmap(p_user_id uuid, p_from_date timestamptz)
                RETURNS TABLE(study_date date, review_count bigint)
                LANGUAGE sql STABLE
                AS $$
                    SELECT (e.reviewed_at AT TIME ZONE 'UTC')::date, COUNT(*)
                    FROM review_events e
                    WHERE e.user_id = p_user_id AND e.reviewed_at >= p_from_date
                    GROUP BY (e.reviewed_at AT TIME ZONE 'UTC')::date
                    ORDER BY 1;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION get_study_heatmap(p_user_id uuid, p_from_date timestamptz)
                RETURNS TABLE(study_date date, review_count bigint)
                LANGUAGE sql STABLE
                AS $$
                    SELECT (rc.last_reviewed_at AT TIME ZONE 'UTC')::date, COUNT(*)
                    FROM review_cards rc
                    WHERE rc.user_id = p_user_id AND rc.last_reviewed_at >= p_from_date
                    GROUP BY (rc.last_reviewed_at AT TIME ZONE 'UTC')::date
                    ORDER BY 1;
                $$;
                """);

            migrationBuilder.DropTable(
                name: "review_events");

            migrationBuilder.DropColumn(
                name: "version",
                table: "review_cards");
        }
    }
}
