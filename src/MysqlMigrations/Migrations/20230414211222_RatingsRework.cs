using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class RatingsRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "RepPlayerRatings");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "PlayerRatings");

            migrationBuilder.RenameColumn(
                name: "Consistency",
                table: "RepPlayerRatings",
                newName: "Deviation");

            migrationBuilder.RenameColumn(
                name: "Consistency",
                table: "PlayerRatings",
                newName: "Deviation");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Deviation",
                table: "RepPlayerRatings",
                newName: "Consistency");

            migrationBuilder.RenameColumn(
                name: "Deviation",
                table: "PlayerRatings",
                newName: "Consistency");

            migrationBuilder.AddColumn<float>(
                name: "Confidence",
                table: "RepPlayerRatings",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "PlayerRatings",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
