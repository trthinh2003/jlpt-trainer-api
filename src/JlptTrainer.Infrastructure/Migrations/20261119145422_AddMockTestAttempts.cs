using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMockTestAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "attempt_id",
                table: "mock_tests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int?[]>(
                name: "selected_choices",
                table: "mock_tests",
                type: "integer[]",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mock_test_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mock_test_attempts", x => x.id);
                    table.CheckConstraint("ck_mock_test_attempts_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_mock_test_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mock_test_attempt_questions",
                columns: table => new
                {
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reading = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    choices = table.Column<string[]>(type: "text[]", nullable: false),
                    correct_choice_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mock_test_attempt_questions", x => new { x.attempt_id, x.position });
                    table.CheckConstraint("ck_mock_test_attempt_questions_choices", "cardinality(choices) = 4 AND correct_choice_index >= 0 AND correct_choice_index < cardinality(choices)");
                    table.ForeignKey(
                        name: "fk_mock_test_attempt_questions_mock_test_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "mock_test_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mock_tests_attempt_id",
                table: "mock_tests",
                column: "attempt_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_mock_tests_attempt_answers",
                table: "mock_tests",
                sql: "(attempt_id IS NULL AND selected_choices IS NULL) OR (attempt_id IS NOT NULL AND selected_choices IS NOT NULL AND cardinality(selected_choices) = total_questions)");

            migrationBuilder.CreateIndex(
                name: "ix_mock_test_attempts_user_id",
                table: "mock_test_attempts",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mock_tests_mock_test_attempts_attempt_id",
                table: "mock_tests",
                column: "attempt_id",
                principalTable: "mock_test_attempts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mock_tests_mock_test_attempts_attempt_id",
                table: "mock_tests");

            migrationBuilder.DropTable(
                name: "mock_test_attempt_questions");

            migrationBuilder.DropTable(
                name: "mock_test_attempts");

            migrationBuilder.DropIndex(
                name: "ix_mock_tests_attempt_id",
                table: "mock_tests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mock_tests_attempt_answers",
                table: "mock_tests");

            migrationBuilder.DropColumn(
                name: "attempt_id",
                table: "mock_tests");

            migrationBuilder.DropColumn(
                name: "selected_choices",
                table: "mock_tests");
        }
    }
}
