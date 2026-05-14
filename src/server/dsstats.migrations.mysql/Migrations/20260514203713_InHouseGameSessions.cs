using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class InHouseGameSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InHouseGameSessions",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByInHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessions", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessions_InHouseUsers_CreatedByInHouseUserId",
                        column: x => x.CreatedByInHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionPlayerSummaries",
                columns: table => new
                {
                    InHouseGameSessionPlayerSummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Observes = table.Column<int>(type: "int", nullable: false),
                    RatingStart = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: true),
                    RatingEnd = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: true),
                    RatingDelta = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: true),
                    AverageGain = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: true),
                    PlayedLatestGame = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ObservedLatestGame = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RatingsPending = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionPlayerSummaries", x => x.InHouseGameSessionPlayerSummaryId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionPlayerSummaries_InHouseGameSessions_InHous~",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionPlayerSummaries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionReplays",
                columns: table => new
                {
                    InHouseGameSessionReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    UploadedByInHouseUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionReplays", x => x.InHouseGameSessionReplayId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplays_InHouseGameSessions_InHouseGameSes~",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplays_InHouseUsers_UploadedByInHouseUser~",
                        column: x => x.UploadedByInHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplays_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionReplayPlayers",
                columns: table => new
                {
                    InHouseGameSessionReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseGameSessionReplayId = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: true),
                    PlayerId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Observer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionReplayPlayers", x => x.InHouseGameSessionReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplayPlayers_InHouseGameSessionReplays_In~",
                        column: x => x.InHouseGameSessionReplayId,
                        principalTable: "InHouseGameSessionReplays",
                        principalColumn: "InHouseGameSessionReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplayPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionReplayPlayers_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionPlayerSummaries_InHouseGameSessionId_Playe~",
                table: "InHouseGameSessionPlayerSummaries",
                columns: new[] { "InHouseGameSessionId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionPlayerSummaries_PlayerId",
                table: "InHouseGameSessionPlayerSummaries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionPlayerSummaries_ToonId_Region_ToonId_Realm~",
                table: "InHouseGameSessionPlayerSummaries",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplayPlayers_InHouseGameSessionReplayId_O~",
                table: "InHouseGameSessionReplayPlayers",
                columns: new[] { "InHouseGameSessionReplayId", "Observer" });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplayPlayers_PlayerId",
                table: "InHouseGameSessionReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplayPlayers_ReplayPlayerId",
                table: "InHouseGameSessionReplayPlayers",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplayPlayers_ToonId_Region_ToonId_Realm_T~",
                table: "InHouseGameSessionReplayPlayers",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplays_InHouseGameSessionId_ReplayId",
                table: "InHouseGameSessionReplays",
                columns: new[] { "InHouseGameSessionId", "ReplayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplays_ReplayId",
                table: "InHouseGameSessionReplays",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplays_UploadedAt",
                table: "InHouseGameSessionReplays",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionReplays_UploadedByInHouseUserId",
                table: "InHouseGameSessionReplays",
                column: "UploadedByInHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_ClosedAt",
                table: "InHouseGameSessions",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedAt",
                table: "InHouseGameSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedByInHouseUserId",
                table: "InHouseGameSessions",
                column: "CreatedByInHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_PublicId",
                table: "InHouseGameSessions",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InHouseGameSessionPlayerSummaries");

            migrationBuilder.DropTable(
                name: "InHouseGameSessionReplayPlayers");

            migrationBuilder.DropTable(
                name: "InHouseGameSessionReplays");

            migrationBuilder.DropTable(
                name: "InHouseGameSessions");
        }
    }
}
