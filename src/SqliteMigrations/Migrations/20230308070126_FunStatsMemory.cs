using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class FunStatsMemory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FunStatMemories",
                columns: table => new
                {
                    FunStatsMemoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    TimePeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTimePlayed = table.Column<long>(type: "INTEGER", nullable: false),
                    AvgGameDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitNameMost = table.Column<string>(type: "TEXT", nullable: false),
                    UnitCountMost = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitNameLeast = table.Column<string>(type: "TEXT", nullable: false),
                    UnitCountLeast = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstReplay = table.Column<string>(type: "TEXT", nullable: true),
                    GreatestArmyReplay = table.Column<string>(type: "TEXT", nullable: true),
                    MostUpgradesReplay = table.Column<string>(type: "TEXT", nullable: true),
                    MostCompetitiveReplay = table.Column<string>(type: "TEXT", nullable: true),
                    GreatestComebackReplay = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunStatMemories", x => x.FunStatsMemoryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunStatMemories");
        }
    }
}
