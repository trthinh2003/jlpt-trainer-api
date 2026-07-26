using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grammar_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    meaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    example_sentence = table.Column<string>(type: "text", nullable: true),
                    example_sentence_meaning = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grammar_points", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kanjis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    on_yomi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kun_yomi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    meaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    stroke_count = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kanjis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mock_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mock_tests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "review_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_type = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ease_factor = table.Column<double>(type: "double precision", precision: 4, scale: 2, nullable: false),
                    interval_days = table.Column<int>(type: "integer", nullable: false),
                    repetitions = table.Column<int>(type: "integer", nullable: false),
                    next_review_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cards_reviewed = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vocabs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reading = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    meaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    example_sentence = table.Column<string>(type: "text", nullable: true),
                    example_sentence_meaning = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocabs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grammar_points_level",
                table: "grammar_points",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_kanjis_character",
                table: "kanjis",
                column: "character",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kanjis_level",
                table: "kanjis",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_mock_tests_user_id_taken_at",
                table: "mock_tests",
                columns: new[] { "user_id", "taken_at" });

            migrationBuilder.CreateIndex(
                name: "ix_review_cards_user_id_card_type_reference_id",
                table: "review_cards",
                columns: new[] { "user_id", "card_type", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_cards_user_id_next_review_date",
                table: "review_cards",
                columns: new[] { "user_id", "next_review_date" });

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_user_id_started_at",
                table: "study_sessions",
                columns: new[] { "user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vocabs_level",
                table: "vocabs",
                column: "level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grammar_points");

            migrationBuilder.DropTable(
                name: "kanjis");

            migrationBuilder.DropTable(
                name: "mock_tests");

            migrationBuilder.DropTable(
                name: "review_cards");

            migrationBuilder.DropTable(
                name: "study_sessions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "vocabs");
        }
    }
}
