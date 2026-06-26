using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class GhostBuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualReplayFolders",
                columns: table => new
                {
                    MauiReplayFolderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Folder = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DetectedName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DetectedToonIdRegion = table.Column<int>(type: "int", nullable: true),
                    DetectedToonIdRealm = table.Column<int>(type: "int", nullable: true),
                    DetectedToonIdId = table.Column<int>(type: "int", nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    DetectedReplayCount = table.Column<int>(type: "int", nullable: false),
                    MauiConfigId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualReplayFolders", x => x.MauiReplayFolderId);
                    table.ForeignKey(
                        name: "FK_ManualReplayFolders_MauiConfig_MauiConfigId",
                        column: x => x.MauiConfigId,
                        principalTable: "MauiConfig",
                        principalColumn: "MauiConfigId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ManualReplayFolders_Folder",
                table: "ManualReplayFolders",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_ManualReplayFolders_MauiConfigId",
                table: "ManualReplayFolders",
                column: "MauiConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualReplayFolders");
        }
    }
}
