using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class DsUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DRangeResults",
                columns: table => new
                {
                    Race = table.Column<int>(type: "int", nullable: false),
                    DRange = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    WinsOrRating = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsUpdates",
                columns: table => new
                {
                    DsUpdateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    DiscordId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Change = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpdates", x => x.DsUpdateId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TimelineQueryDatas",
                columns: table => new
                {
                    Race = table.Column<int>(type: "int", nullable: false),
                    Ryear = table.Column<int>(type: "int", nullable: false),
                    Rmonth = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    AvgGain = table.Column<double>(type: "double", nullable: false),
                    AvgRating = table.Column<double>(type: "double", nullable: false),
                    AvgOppRating = table.Column<double>(type: "double", nullable: false),
                    AvgTeamRating = table.Column<double>(type: "double", nullable: false),
                    AvgOppTeamRating = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
