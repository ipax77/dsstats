using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ReplayImportDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "Replays",
                type: "datetime(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Imported",
                table: "Replays",
                column: "Imported");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Replays_Imported",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "Replays");
        }
    }
}
