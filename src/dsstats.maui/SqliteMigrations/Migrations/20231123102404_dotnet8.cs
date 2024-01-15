using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class dotnet8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamageEnts");

            migrationBuilder.DropTable(
                name: "DRangeResults");

            migrationBuilder.DropTable(
                name: "SynergyEnts");

            migrationBuilder.DropTable(
                name: "TimelineQueryDatas");

            migrationBuilder.DropTable(
                name: "WinrateEnts");

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

            migrationBuilder.CreateTable(
                name: "DamageEnts",
                columns: table => new
                {
                    AvgAPM = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgArmy = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgGas = table.Column<double>(type: "REAL", nullable: false),
                    AvgIncome = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgKills = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgUpgrades = table.Column<int>(type: "INTEGER", nullable: false),
                    Breakpoint = table.Column<int>(type: "INTEGER", nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Mvp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "DRangeResults",
                columns: table => new
                {
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    DRange = table.Column<int>(type: "INTEGER", nullable: false),
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    WinsOrRating = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SynergyEnts",
                columns: table => new
                {
                    AvgGain = table.Column<double>(type: "REAL", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    NormalizedAvgGain = table.Column<double>(type: "REAL", nullable: false),
                    Teammate = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "TimelineQueryDatas",
                columns: table => new
                {
                    AvgGain = table.Column<double>(type: "REAL", nullable: false),
                    AvgOppRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgOppTeamRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgTeamRating = table.Column<double>(type: "REAL", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    Rmonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Ryear = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "WinrateEnts",
                columns: table => new
                {
                    AvgGain = table.Column<double>(type: "REAL", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });
        }
    }
}
