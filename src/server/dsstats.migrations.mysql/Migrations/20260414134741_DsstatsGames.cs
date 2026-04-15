using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class DsstatsGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "ArcadeGames",
            //    table: "PlayerRatings",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<int>(
            //    name: "DsstatsGames",
            //    table: "PlayerRatings",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            migrationBuilder.Sql(
                "ALTER TABLE PlayerRatings ADD COLUMN ArcadeGames INT NOT NULL DEFAULT 0 AFTER Games;");

            migrationBuilder.Sql(
                "ALTER TABLE PlayerRatings ADD COLUMN DsstatsGames INT NOT NULL DEFAULT 0 AFTER ArcadeGames;");
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
