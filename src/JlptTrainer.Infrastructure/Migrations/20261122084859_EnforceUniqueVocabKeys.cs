using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueVocabKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                LOCK TABLE vocabs, review_cards, mock_test_attempt_questions IN SHARE ROW EXCLUSIVE MODE;

                DO $checks$
                BEGIN
                    IF EXISTS (SELECT 1 FROM vocabs GROUP BY word, reading HAVING count(*) > 1)
                       AND current_setting('jlpt.merge_identical_vocabs', true) IS DISTINCT FROM 'on' THEN
                        RAISE EXCEPTION 'Duplicate vocabulary found. After reviewing the merge, run update-database.ps1 -MergeIdenticalVocabs.';
                    END IF;
                    IF EXISTS (
                        SELECT 1
                        FROM vocabs
                        GROUP BY word, reading
                        HAVING count(DISTINCT ROW(meaning, example_sentence, example_sentence_meaning, level)) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate vocabulary keys with differing content require review.';
                    END IF;
                END;
                $checks$;

                CREATE TEMP TABLE vocab_duplicates ON COMMIT DROP AS
                WITH duplicate_keys AS (
                    SELECT word, reading FROM vocabs GROUP BY word, reading HAVING count(*) > 1
                ), card_counts AS (
                    SELECT reference_id, count(*) AS count
                    FROM review_cards WHERE card_type = 0 GROUP BY reference_id
                ), question_counts AS (
                    SELECT source_vocab_id, count(*) AS count
                    FROM mock_test_attempt_questions WHERE source_vocab_id IS NOT NULL GROUP BY source_vocab_id
                ), ranked AS (
                    SELECT vocab.id, first_value(vocab.id) OVER (
                        PARTITION BY vocab.word, vocab.reading
                        ORDER BY coalesce(cards.count, 0) + coalesce(questions.count, 0) DESC, vocab.created_at, vocab.id
                    ) AS keep_id
                    FROM vocabs vocab
                    JOIN duplicate_keys USING (word, reading)
                    LEFT JOIN card_counts cards ON cards.reference_id = vocab.id
                    LEFT JOIN question_counts questions ON questions.source_vocab_id = vocab.id
                )
                SELECT id, keep_id FROM ranked;

                CREATE TEMP TABLE vocab_duplicate_cards ON COMMIT DROP AS
                SELECT card.id, card.reference_id, vocab.keep_id,
                       row_number() OVER (
                           PARTITION BY card.user_id, vocab.keep_id
                           ORDER BY card.last_reviewed_at DESC NULLS LAST,
                                    (card.reference_id = vocab.keep_id) DESC, card.created_at, card.id
                       ) AS priority
                FROM review_cards card
                JOIN vocab_duplicates vocab ON vocab.id = card.reference_id
                WHERE card.card_type = 0;

                DELETE FROM review_cards card
                USING vocab_duplicate_cards plan
                WHERE card.id = plan.id AND plan.priority > 1;

                UPDATE review_cards card
                SET reference_id = plan.keep_id, version = card.version + 1
                FROM vocab_duplicate_cards plan
                WHERE card.id = plan.id AND plan.priority = 1 AND plan.reference_id <> plan.keep_id;

                UPDATE mock_test_attempt_questions question
                SET source_vocab_id = vocab.keep_id
                FROM vocab_duplicates vocab
                WHERE question.source_vocab_id = vocab.id AND vocab.id <> vocab.keep_id;

                DELETE FROM vocabs vocab
                USING vocab_duplicates plan
                WHERE vocab.id = plan.id AND plan.id <> plan.keep_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_vocabs_word_reading",
                table: "vocabs",
                columns: new[] { "word", "reading" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vocabs_word_reading",
                table: "vocabs");
        }
    }
}
