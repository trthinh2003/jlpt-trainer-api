using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenLearningDataConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_study_sessions_valid_end_time",
                table: "study_sessions",
                sql: "ended_at IS NULL OR ended_at >= started_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_sessions_valid_review_counts",
                table: "study_sessions",
                sql: "correct_count >= 0 AND correct_count <= cards_reviewed");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_cards_nonnegative_interval",
                table: "review_cards",
                sql: "interval_days >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_cards_nonnegative_repetitions",
                table: "review_cards",
                sql: "repetitions >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_mock_tests_valid_answer_counts",
                table: "mock_tests",
                sql: "correct_answers >= 0 AND correct_answers <= total_questions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_kanjis_positive_stroke_count",
                table: "kanjis",
                sql: "stroke_count > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_mock_tests_users_user_id",
                table: "mock_tests",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_review_cards_users_user_id",
                table: "review_cards",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_study_sessions_users_user_id",
                table: "study_sessions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mock_tests_users_user_id",
                table: "mock_tests");

            migrationBuilder.DropForeignKey(
                name: "fk_review_cards_users_user_id",
                table: "review_cards");

            migrationBuilder.DropForeignKey(
                name: "fk_study_sessions_users_user_id",
                table: "study_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_study_sessions_valid_end_time",
                table: "study_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_study_sessions_valid_review_counts",
                table: "study_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_cards_nonnegative_interval",
                table: "review_cards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_cards_nonnegative_repetitions",
                table: "review_cards");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mock_tests_valid_answer_counts",
                table: "mock_tests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_kanjis_positive_stroke_count",
                table: "kanjis");
        }
    }
}
