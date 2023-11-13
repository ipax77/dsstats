using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AvgRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ReplayRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ComboReplayRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgRating",
                table: "ArcadeReplayRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MaterializedArcadeReplays",
                columns: table => new
                {
                    MaterializedArcadeReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArcadeReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameMode = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerTeam = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterializedArcadeReplays", x => x.MaterializedArcadeReplayId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime",
                table: "Replays",
                column: "GameTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterializedArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_GameTime",
                table: "Replays");

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
