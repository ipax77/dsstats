using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArcadePlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayers");

            migrationBuilder.DropTable(
                name: "ArcadePlayers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArcadePlayers",
                columns: table => new
                {
                    ArcadePlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    RealmId = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayers", x => x.ArcadePlayerId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArcadePlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayers", x => x.ArcadeReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadePlayers_ArcadePlayerId",
                        column: x => x.ArcadePlayerId,
                        principalTable: "ArcadePlayers",
                        principalColumn: "ArcadePlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayers_Name",
                table: "ArcadePlayers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayers_RegionId_RealmId_ProfileId",
                table: "ArcadePlayers",
                columns: new[] { "RegionId", "RealmId", "ProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadePlayerId",
                table: "ArcadeReplayPlayers",
                column: "ArcadePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadeReplayId",
                table: "ArcadeReplayPlayers",
                column: "ArcadeReplayId");
        }
    }
}
