using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerRatingsRowNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Pos",
                table: "PlayerRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_RatingType",
                table: "PlayerRatings",
                column: "RatingType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerRatings_RatingType",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "Pos",
                table: "PlayerRatings");
        }
    }
}
