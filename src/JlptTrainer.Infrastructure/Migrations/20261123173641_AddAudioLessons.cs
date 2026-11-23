using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audio_lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    object_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    cover_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    lyrics = table.Column<string>(type: "character varying(28000)", maxLength: 28000, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cleanup_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_lessons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audio_lesson_words",
                columns: table => new
                {
                    audio_lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vocab_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_lesson_words", x => new { x.audio_lesson_id, x.vocab_id });
                    table.ForeignKey(
                        name: "fk_audio_lesson_words_audio_lessons_audio_lesson_id",
                        column: x => x.audio_lesson_id,
                        principalTable: "audio_lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_audio_lesson_words_vocabs_vocab_id",
                        column: x => x.vocab_id,
                        principalTable: "vocabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audio_lesson_words_vocab_id",
                table: "audio_lesson_words",
                column: "vocab_id");

            migrationBuilder.CreateIndex(
                name: "ix_audio_lessons_cleanup_after",
                table: "audio_lessons",
                column: "cleanup_after",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audio_lessons_published_at_id",
                table: "audio_lessons",
                columns: new[] { "published_at", "id" },
                filter: "published_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_lesson_words");

            migrationBuilder.DropTable(
                name: "audio_lessons");
        }
    }
}
