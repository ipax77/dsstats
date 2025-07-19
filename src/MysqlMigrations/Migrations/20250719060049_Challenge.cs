using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Challenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpChallenges",
                columns: table => new
                {
                    SpChallengeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Fen = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Base64Image = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Time = table.Column<int>(type: "int", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpChallenges", x => x.SpChallengeId);
                    table.ForeignKey(
                        name: "FK_SpChallenges_Players_WinnerPlayerId",
                        column: x => x.WinnerPlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpChallengeSubmissions",
                columns: table => new
                {
                    SpChallengeSubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Submitted = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    GameTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Fen = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Time = table.Column<int>(type: "int", nullable: false),
                    SpChallengeId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpChallengeSubmissions", x => x.SpChallengeSubmissionId);
                    table.ForeignKey(
                        name: "FK_SpChallengeSubmissions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpChallengeSubmissions_SpChallenges_SpChallengeId",
                        column: x => x.SpChallengeId,
                        principalTable: "SpChallenges",
                        principalColumn: "SpChallengeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SpChallengeSubmissions_PlayerId",
                table: "SpChallengeSubmissions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpChallengeSubmissions_SpChallengeId",
                table: "SpChallengeSubmissions",
                column: "SpChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_SpChallenges_WinnerPlayerId",
                table: "SpChallenges",
                column: "WinnerPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpChallengeSubmissions");

            migrationBuilder.DropTable(
                name: "SpChallenges");
        }
    }
}
