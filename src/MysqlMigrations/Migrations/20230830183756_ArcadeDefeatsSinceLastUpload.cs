using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeDefeatsSinceLastUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DamageEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Breakpoint = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    AvgKills = table.Column<int>(type: "int", nullable: false),
                    AvgArmy = table.Column<int>(type: "int", nullable: false),
                    AvgUpgrades = table.Column<int>(type: "int", nullable: false),
                    AvgGas = table.Column<double>(type: "double", nullable: false),
                    AvgIncome = table.Column<int>(type: "int", nullable: false),
                    AvgAPM = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SynergyEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Teammate = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<double>(type: "double", nullable: false),
                    AvgGain = table.Column<double>(type: "double", nullable: false),
                    NormalizedAvgGain = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WinrateEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<double>(type: "double", nullable: false),
                    AvgGain = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            var sp = @"CREATE PROCEDURE `SetPlayerRatingPosWithDefeats`()
BEGIN
	SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;
END";
            migrationBuilder.Sql(sp);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamageEnts");

            migrationBuilder.DropTable(
                name: "SynergyEnts");

            migrationBuilder.DropTable(
                name: "WinrateEnts");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings");
        }
    }
}
