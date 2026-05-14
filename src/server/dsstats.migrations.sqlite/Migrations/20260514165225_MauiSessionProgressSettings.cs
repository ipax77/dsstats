using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MauiSessionProgressSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionWindowGameMode",
                table: "MauiConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowHours",
                table: "MauiConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowMode",
                table: "MauiConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowReplayCount",
                table: "MauiConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionWindowGameMode",
                table: "MauiConfig");

            migrationBuilder.DropColumn(
                name: "SessionWindowHours",
                table: "MauiConfig");

            migrationBuilder.DropColumn(
                name: "SessionWindowMode",
                table: "MauiConfig");

            migrationBuilder.DropColumn(
                name: "SessionWindowReplayCount",
                table: "MauiConfig");
        }
    }
}
