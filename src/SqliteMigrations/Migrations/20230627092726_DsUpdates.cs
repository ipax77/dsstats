using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class DsUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DRangeResults",
                columns: table => new
                {
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    DRange = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    WinsOrRating = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "DsUpdates",
                columns: table => new
                {
                    DsUpdateId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    DiscordId = table.Column<string>(type: "TEXT", nullable: false),
                    Change = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpdates", x => x.DsUpdateId);
                });

            migrationBuilder.CreateTable(
                name: "TimelineQueryDatas",
                columns: table => new
                {
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    Ryear = table.Column<int>(type: "INTEGER", nullable: false),
                    Rmonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgGain = table.Column<double>(type: "REAL", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgOppRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgTeamRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgOppTeamRating = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_DsUpdates_Time",
                table: "DsUpdates",
                column: "Time");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DRangeResults");

            migrationBuilder.DropTable(
                name: "DsUpdates");

            migrationBuilder.DropTable(
                name: "TimelineQueryDatas");
        }
    }
}
