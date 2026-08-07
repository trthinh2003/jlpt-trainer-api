using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION get_study_heatmap(
                    p_user_id uuid,
                    p_from_date timestamptz
                )
                RETURNS TABLE(study_date date, review_count bigint)
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT
                        (rc.last_reviewed_at AT TIME ZONE 'UTC')::date AS study_date,
                        COUNT(*)::bigint AS review_count
                    FROM review_cards rc
                    WHERE rc.user_id = p_user_id
                      AND rc.last_reviewed_at IS NOT NULL
                      AND rc.last_reviewed_at >= p_from_date
                    GROUP BY (rc.last_reviewed_at AT TIME ZONE 'UTC')::date
                    ORDER BY study_date;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION get_card_progress(
                    p_user_id uuid,
                    p_mastery_threshold int
                )
                RETURNS TABLE(card_type int, total_cards bigint, mastered_count bigint)
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT
                        rc.card_type,
                        COUNT(*)::bigint AS total_cards,
                        SUM(CASE WHEN rc.repetitions >= p_mastery_threshold THEN 1 ELSE 0 END)::bigint AS mastered_count
                    FROM review_cards rc
                    WHERE rc.user_id = p_user_id
                    GROUP BY rc.card_type;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION get_mock_test_trend(
                    p_user_id uuid
                )
                RETURNS TABLE(taken_date date, level int, average_score numeric, test_count bigint)
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT
                        (mt.taken_at AT TIME ZONE 'UTC')::date AS taken_date,
                        mt.level,
                        AVG(
                            CASE WHEN mt.total_questions = 0 THEN 0
                                 ELSE (mt.correct_answers::decimal / mt.total_questions) * 100
                            END
                        ) AS average_score,
                        COUNT(*)::bigint AS test_count
                    FROM mock_tests mt
                    WHERE mt.user_id = p_user_id
                    GROUP BY (mt.taken_at AT TIME ZONE 'UTC')::date, mt.level
                    ORDER BY taken_date;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS get_study_heatmap(uuid, timestamptz);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS get_card_progress(uuid, int);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS get_mock_test_trend(uuid);");
        }
    }
}
