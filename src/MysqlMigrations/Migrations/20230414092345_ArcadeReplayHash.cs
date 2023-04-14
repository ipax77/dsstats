using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeReplayHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "ArcadeReplays",
                type: "datetime(0)",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReplayHash",
                table: "ArcadeReplays",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays",
                column: "ReplayHash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "ReplayHash",
                table: "ArcadeReplays");
        }
    }
}
