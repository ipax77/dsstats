using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InHouseRosterPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InHouseGameSessionRosterPlayers",
                columns: table => new
                {
                    InHouseGameSessionRosterPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InHouseGameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ToonId_Region = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialRating = table.Column<double>(type: "REAL", precision: 7, scale: 2, nullable: false),
                    JoinedReplayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSitter = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsManual = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddSource = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    AddedByInHouseUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionRosterPlayers", x => x.InHouseGameSessionRosterPlayerId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionRosterPlayers_InHouseGameSessions_InHouseGameSessionId",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionRosterPlayers_InHouseUsers_AddedByInHouseUserId",
                        column: x => x.AddedByInHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionRosterPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionRosterPlayers_AddedByInHouseUserId",
                table: "InHouseGameSessionRosterPlayers",
                column: "AddedByInHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionRosterPlayers_InHouseGameSessionId_PlayerId",
                table: "InHouseGameSessionRosterPlayers",
                columns: new[] { "InHouseGameSessionId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionRosterPlayers_PlayerId",
                table: "InHouseGameSessionRosterPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionRosterPlayers_PublicId",
                table: "InHouseGameSessionRosterPlayers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessionRosterPlayers_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "InHouseGameSessionRosterPlayers",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InHouseGameSessionRosterPlayers");
        }
    }
}
