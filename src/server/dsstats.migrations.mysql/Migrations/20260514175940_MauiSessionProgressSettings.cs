using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260514175940_MauiSessionProgressSettings")]
    public partial class MauiSessionProgressSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionWindowGameMode",
                table: "MauiConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowHours",
                table: "MauiConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SessionWindowInitialized",
                table: "MauiConfig",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowMode",
                table: "MauiConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionWindowReplayCount",
                table: "MauiConfig",
                type: "int",
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
                name: "SessionWindowInitialized",
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
