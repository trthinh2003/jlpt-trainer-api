using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JlptTrainer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_valid_role",
                table: "users",
                sql: "role IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_valid_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
