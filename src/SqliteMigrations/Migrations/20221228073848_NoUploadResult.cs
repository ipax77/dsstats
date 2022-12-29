using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class NoUploadResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoUploadResults",
                columns: table => new
                {
                    NoUploadResultId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TotalReplays = table.Column<int>(type: "INTEGER", nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    NoUploadTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    NoUploadDefeats = table.Column<int>(type: "INTEGER", nullable: false),
                    LatestNoUpload = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoUploadResults", x => x.NoUploadResultId);
                    table.ForeignKey(
                        name: "FK_NoUploadResults_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoUploadResults_PlayerId",
                table: "NoUploadResults",
                column: "PlayerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoUploadResults");
        }
    }
}
