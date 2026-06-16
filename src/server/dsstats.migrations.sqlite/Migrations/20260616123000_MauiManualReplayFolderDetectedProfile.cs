using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260616123000_MauiManualReplayFolderDetectedProfile")]
    public partial class MauiManualReplayFolderDetectedProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DetectedAtUtc",
                table: "ManualReplayFolders",
                type: "TEXT",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetectedName",
                table: "ManualReplayFolders",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetectedReplayCount",
                table: "ManualReplayFolders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DetectedToonIdId",
                table: "ManualReplayFolders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetectedToonIdRealm",
                table: "ManualReplayFolders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetectedToonIdRegion",
                table: "ManualReplayFolders",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetectedAtUtc",
                table: "ManualReplayFolders");

            migrationBuilder.DropColumn(
                name: "DetectedName",
                table: "ManualReplayFolders");

            migrationBuilder.DropColumn(
                name: "DetectedReplayCount",
                table: "ManualReplayFolders");

            migrationBuilder.DropColumn(
                name: "DetectedToonIdId",
                table: "ManualReplayFolders");

            migrationBuilder.DropColumn(
                name: "DetectedToonIdRealm",
                table: "ManualReplayFolders");

            migrationBuilder.DropColumn(
                name: "DetectedToonIdRegion",
                table: "ManualReplayFolders");
        }
    }
}
