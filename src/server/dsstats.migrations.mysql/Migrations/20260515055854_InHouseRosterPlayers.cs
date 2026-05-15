using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
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
                    InHouseGameSessionRosterPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    InitialRating = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: false),
                    JoinedReplayCount = table.Column<int>(type: "int", nullable: false),
                    IsSitter = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsManual = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddSource = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddedByInHouseUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionRosterPlayers", x => x.InHouseGameSessionRosterPlayerId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionRosterPlayers_InHouseGameSessions_InHouseG~",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionRosterPlayers_InHouseUsers_AddedByInHouseU~",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_InHouseGameSessionRosterPlayers_ToonId_Region_ToonId_Realm_T~",
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
