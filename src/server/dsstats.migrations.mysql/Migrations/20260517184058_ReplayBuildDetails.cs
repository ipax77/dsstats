using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ReplayBuildDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayBuildDetails",
                columns: table => new
                {
                    ReplayBuildDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DetectionVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayBuildDetails", x => x.ReplayBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayBuildDetails_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayPlayerBuildDetails",
                columns: table => new
                {
                    ReplayPlayerBuildDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Build = table.Column<int>(type: "int", nullable: false),
                    GasFirst = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Lane = table.Column<int>(type: "int", nullable: false),
                    OppGamePos = table.Column<int>(type: "int", nullable: false),
                    OppCommander = table.Column<int>(type: "int", nullable: false),
                    OppBuild = table.Column<int>(type: "int", nullable: false),
                    OppGasFirst = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Won = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayBuildDetailId = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    OppReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerBuildDetails", x => x.ReplayPlayerBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayBuildDetails_ReplayBuildDetai~",
                        column: x => x.ReplayBuildDetailId,
                        principalTable: "ReplayBuildDetails",
                        principalColumn: "ReplayBuildDetailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayPlayers_OppReplayPlayerId",
                        column: x => x.OppReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayTeamBuildDetails",
                columns: table => new
                {
                    ReplayTeamBuildDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    TeamBuild = table.Column<int>(type: "int", nullable: false),
                    ReplayBuildDetailId = table.Column<int>(type: "int", nullable: false),
                    LeaderReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    FollowerReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayTeamBuildDetails", x => x.ReplayTeamBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayBuildDetails_ReplayBuildDetailId",
                        column: x => x.ReplayBuildDetailId,
                        principalTable: "ReplayBuildDetails",
                        principalColumn: "ReplayBuildDetailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayPlayers_FollowerReplayPlayerId",
                        column: x => x.FollowerReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayPlayers_LeaderReplayPlayerId",
                        column: x => x.LeaderReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameMode_TE_PlayerCount_WinnerTeam_Duration_ReplayId",
                table: "Replays",
                columns: new[] { "GameMode", "TE", "PlayerCount", "WinnerTeam", "Duration", "ReplayId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayBuildDetails_ReplayId",
                table: "ReplayBuildDetails",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayBuildDetails_Status_DetectionVersion",
                table: "ReplayBuildDetails",
                columns: new[] { "Status", "DetectionVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_Commander_Build_OppCommander_OppBui~",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "Commander", "Build", "OppCommander", "OppBuild" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_Commander_Build_TeamId_Won",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "Commander", "Build", "TeamId", "Won" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_OppReplayPlayerId",
                table: "ReplayPlayerBuildDetails",
                column: "OppReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_ReplayBuildDetailId",
                table: "ReplayPlayerBuildDetails",
                column: "ReplayBuildDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_ReplayPlayerId",
                table: "ReplayPlayerBuildDetails",
                column: "ReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_TeamId_Lane",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "TeamId", "Lane" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_FollowerReplayPlayerId",
                table: "ReplayTeamBuildDetails",
                column: "FollowerReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_LeaderReplayPlayerId",
                table: "ReplayTeamBuildDetails",
                column: "LeaderReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_ReplayBuildDetailId",
                table: "ReplayTeamBuildDetails",
                column: "ReplayBuildDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_TeamBuild_TeamId",
                table: "ReplayTeamBuildDetails",
                columns: new[] { "TeamBuild", "TeamId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayPlayerBuildDetails");

            migrationBuilder.DropTable(
                name: "ReplayTeamBuildDetails");

            migrationBuilder.DropTable(
                name: "ReplayBuildDetails");

            migrationBuilder.DropIndex(
                name: "IX_Replays_GameMode_TE_PlayerCount_WinnerTeam_Duration_ReplayId",
                table: "Replays");
        }
    }
}
