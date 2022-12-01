using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
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
                    PlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    TeamGames = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    Main = table.Column<int>(type: "int", nullable: false),
                    MmrOverTime = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId",
                table: "PlayerRatings",
                column: "PlayerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.AddColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "float",
                nullable: true);
        }
    }
}
