using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerRatings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MmrChange",
                table: "ReplayPlayers");

            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                columns: table => new
                {
                    PlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Mvp = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamGames = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Main = table.Column<int>(type: "INTEGER", nullable: false),
                    MmrOverTime = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Consistency = table.Column<double>(type: "REAL", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    IsUploader = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatings", x => x.PlayerRatingId);
                    table.ForeignKey(
                        name: "FK_PlayerRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayerRatings",
                columns: table => new
                {
                    ReplayPlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MmrChange = table.Column<double>(type: "REAL", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerRatings", x => x.ReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId",
                table: "PlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayId",
                table: "ReplayPlayerRatings",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayPlayerId",
                table: "ReplayPlayerRatings",
                column: "ReplayPlayerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayPlayerRatings");

            migrationBuilder.AddColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "REAL",
                nullable: true);
        }
    }
}
