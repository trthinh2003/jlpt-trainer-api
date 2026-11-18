using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredentialVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "credential_version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "credential_version",
                table: "users");
        }
    }
}
