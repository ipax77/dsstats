using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class GameCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcadeGames",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DsstatsGames",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            var sql = @"
                ALTER TABLE PlayerRatings MODIFY DsstatsGames int NOT NULL AFTER Games;
                ALTER TABLE PlayerRatings MODIFY ArcadeGames int NOT NULL AFTER DsstatsGames;";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArcadeGames",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "DsstatsGames",
                table: "PlayerRatings");
        }
    }
}
