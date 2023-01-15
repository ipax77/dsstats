using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerRatingChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerRatingChanges",
                columns: table => new
                {
                    PlayerRatingChangeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Change24h = table.Column<float>(type: "REAL", nullable: false),
                    Change10d = table.Column<float>(type: "REAL", nullable: false),
                    Change30d = table.Column<float>(type: "REAL", nullable: false),
                    PlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatingChanges", x => x.PlayerRatingChangeId);
                    table.ForeignKey(
                        name: "FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId",
                        column: x => x.PlayerRatingId,
                        principalTable: "PlayerRatings",
                        principalColumn: "PlayerRatingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatingChanges_PlayerRatingId",
                table: "PlayerRatingChanges",
                column: "PlayerRatingId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRatingChanges");
        }
    }
}
