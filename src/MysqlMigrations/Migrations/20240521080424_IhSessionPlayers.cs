using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class IhSessionPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupStateV2",
                table: "IhSessions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IhSessionPlayers",
                columns: table => new
                {
                    IhSessionPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Obs = table.Column<int>(type: "int", nullable: false),
                    RatingStart = table.Column<int>(type: "int", nullable: false),
                    RatingEnd = table.Column<int>(type: "int", nullable: false),
                    Performance = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    IhSessionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IhSessionPlayers", x => x.IhSessionPlayerId);
                    table.ForeignKey(
                        name: "FK_IhSessionPlayers_IhSessions_IhSessionId",
                        column: x => x.IhSessionId,
                        principalTable: "IhSessions",
                        principalColumn: "IhSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IhSessionPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IhSessionPlayers_IhSessionId",
                table: "IhSessionPlayers",
                column: "IhSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IhSessionPlayers_PlayerId",
                table: "IhSessionPlayers",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IhSessionPlayers");

            migrationBuilder.DropColumn(
                name: "GroupStateV2",
                table: "IhSessions");
        }
    }
}
