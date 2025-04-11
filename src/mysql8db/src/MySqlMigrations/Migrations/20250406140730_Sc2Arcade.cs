using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Sc2Arcade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArcadeReplays",
                columns: table => new
                {
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    BnetBucketId = table.Column<long>(type: "bigint", nullable: false),
                    BnetRecordId = table.Column<long>(type: "bigint", nullable: false),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplays", x => x.ArcadeReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayers", x => x.ArcadeReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayArcadeMatches",
                columns: table => new
                {
                    ReplayArcadeMatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayArcadeMatches", x => x.ReplayArcadeMatchId);
                    table.ForeignKey(
                        name: "FK_ReplayArcadeMatches_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayArcadeMatches_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadeReplayId",
                table: "ArcadeReplayPlayers",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_PlayerId",
                table: "ArcadeReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt",
                table: "ArcadeReplays",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "BnetBucketId", "BnetRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayArcadeMatches_ArcadeReplayId",
                table: "ReplayArcadeMatches",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayArcadeMatches_ReplayId",
                table: "ReplayArcadeMatches",
                column: "ReplayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayers");

            migrationBuilder.DropTable(
                name: "ReplayArcadeMatches");

            migrationBuilder.DropTable(
                name: "ArcadeReplays");
        }
    }
}
