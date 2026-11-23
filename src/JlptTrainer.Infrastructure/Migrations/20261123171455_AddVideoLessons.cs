using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    object_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cleanup_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_video_lessons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_lessons_cleanup_after",
                table: "video_lessons",
                column: "cleanup_after",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_video_lessons_published_at_id",
                table: "video_lessons",
                columns: new[] { "published_at", "id" },
                filter: "published_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_lessons");
        }
    }
}
