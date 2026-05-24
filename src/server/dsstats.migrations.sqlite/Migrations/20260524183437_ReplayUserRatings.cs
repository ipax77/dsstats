using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ReplayUserRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayUserRatingCollects",
                columns: table => new
                {
                    ReplayUserRatingCollectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    IpHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayUserRatingCollects", x => x.ReplayUserRatingCollectId);
                    table.ForeignKey(
                        name: "FK_ReplayUserRatingCollects_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayUserRatingSummaries",
                columns: table => new
                {
                    ReplayUserRatingSummaryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    VoteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoreSum = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayUserRatingSummaries", x => x.ReplayUserRatingSummaryId);
                    table.ForeignKey(
                        name: "FK_ReplayUserRatingSummaries_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUserRatingCollects_ProcessedAt_ReplayId",
                table: "ReplayUserRatingCollects",
                columns: new[] { "ProcessedAt", "ReplayId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUserRatingCollects_ReplayId_IpHash_CreatedAt",
                table: "ReplayUserRatingCollects",
                columns: new[] { "ReplayId", "IpHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUserRatingSummaries_ReplayId",
                table: "ReplayUserRatingSummaries",
                column: "ReplayId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayUserRatingCollects");

            migrationBuilder.DropTable(
                name: "ReplayUserRatingSummaries");
        }
    }
}
