using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class AvgRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ReplayRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ComboReplayRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ArcadeReplayRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MaterializedArcadeReplays",
                columns: table => new
                {
                    MaterializedArcadeReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterializedArcadeReplays", x => x.MaterializedArcadeReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            var sql = @"CREATE PROCEDURE `CreateMaterializedArcadeReplays`()
BEGIN
	TRUNCATE TABLE MaterializedArcadeReplays;
    INSERT INTO MaterializedArcadeReplays (ArcadeReplayId, CreatedAt, WinnerTeam, Duration, GameMode)
    SELECT `a`.`ArcadeReplayId`, `a`.`CreatedAt`, `a`.`WinnerTeam`, `a`.`Duration`, `a`.`GameMode`
    FROM `ArcadeReplays` AS `a`
    WHERE (((((`a`.`CreatedAt` >= '2021-02-01') AND (`a`.`PlayerCount` = 6)) AND (`a`.`Duration` >= 300)) AND (`a`.`WinnerTeam` > 0)) AND NOT (`a`.`TournamentEdition`)) AND `a`.`GameMode` IN (3, 7, 4)
    ORDER BY `a`.`CreatedAt`, `a`.`ArcadeReplayId`;
END
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterializedArcadeReplays");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "ReplayRatings");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "ComboReplayRatings");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "ArcadeReplayRatings");
        }
    }
}
