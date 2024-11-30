using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeReplayDsPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayerRatings");

            migrationBuilder.RenameColumn(
                name: "ArcadePlayerId",
                table: "ArcadePlayerRatings",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_ArcadePlayerRatings_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                newName: "IX_ArcadePlayerRatings_PlayerId");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayDsPlayers",
                columns: table => new
                {
                    ArcadeReplayDsPlayerId = table.Column<int>(type: "int", nullable: false)
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
                    table.PrimaryKey("PK_ArcadeReplayDsPlayers", x => x.ArcadeReplayDsPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayDsPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayDsPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    ArcadeReplayDsPlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayDsPlayerRatings", x => x.ArcadeReplayDsPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayerRatings_ArcadeReplayDsPlayers_ArcadeRepl~",
                        column: x => x.ArcadeReplayDsPlayerId,
                        principalTable: "ArcadeReplayDsPlayers",
                        principalColumn: "ArcadeReplayDsPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayerRatings_ArcadeReplayRatings_ArcadeReplay~",
                        column: x => x.ArcadeReplayRatingId,
                        principalTable: "ArcadeReplayRatings",
                        principalColumn: "ArcadeReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayerRatings_ArcadeReplayDsPlayerId",
                table: "ArcadeReplayDsPlayerRatings",
                column: "ArcadeReplayDsPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayDsPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayers_ArcadeReplayId",
                table: "ArcadeReplayDsPlayers",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayers_PlayerId",
                table: "ArcadeReplayDsPlayers",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_Players_PlayerId",
                table: "ArcadePlayerRatings",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);

            var cleanup = "DROP PROCEDURE IF EXISTS `SetArcadePlayerRatingPos`;";
            migrationBuilder.Sql(cleanup);
            var SetArcadePlayerRatingPos = @"CREATE PROCEDURE `SetArcadePlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, PlayerId;
END
";
            migrationBuilder.Sql(SetArcadePlayerRatingPos);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_Players_PlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadeReplayDsPlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadeReplayDsPlayers");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "ArcadePlayerRatings",
                newName: "ArcadePlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_ArcadePlayerRatings_PlayerId",
                table: "ArcadePlayerRatings",
                newName: "IX_ArcadePlayerRatings_ArcadePlayerId");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayerRatings", x => x.ArcadeReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayPlayers_ArcadeReplayPl~",
                        column: x => x.ArcadeReplayPlayerId,
                        principalTable: "ArcadeReplayPlayers",
                        principalColumn: "ArcadeReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayRatings_ArcadeReplayRa~",
                        column: x => x.ArcadeReplayRatingId,
                        principalTable: "ArcadeReplayRatings",
                        principalColumn: "ArcadeReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayPlayerId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId",
                principalTable: "ArcadePlayers",
                principalColumn: "ArcadePlayerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
