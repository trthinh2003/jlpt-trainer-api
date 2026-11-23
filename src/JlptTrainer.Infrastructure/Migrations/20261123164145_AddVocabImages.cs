using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vocab_images",
                columns: table => new
                {
                    object_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    vocab_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cleanup_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocab_images", x => x.object_key);
                    table.ForeignKey(
                        name: "fk_vocab_images_vocabs_vocab_id",
                        column: x => x.vocab_id,
                        principalTable: "vocabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vocab_images_cleanup_after",
                table: "vocab_images",
                column: "cleanup_after",
                filter: "vocab_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vocab_images_vocab_id",
                table: "vocab_images",
                column: "vocab_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vocab_images");
        }
    }
}
