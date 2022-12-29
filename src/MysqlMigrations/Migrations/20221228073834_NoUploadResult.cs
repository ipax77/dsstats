using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class NoUploadResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoUploadResults",
                columns: table => new
                {
                    NoUploadResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TotalReplays = table.Column<int>(type: "int", nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    NoUploadTotal = table.Column<int>(type: "int", nullable: false),
                    NoUploadDefeats = table.Column<int>(type: "int", nullable: false),
                    LatestNoUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
