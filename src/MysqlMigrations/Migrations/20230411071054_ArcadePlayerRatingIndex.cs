using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadePlayerRatingIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_RatingType",
                table: "ArcadePlayerRatings",
                column: "RatingType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadePlayerRatings_RatingType",
                table: "ArcadePlayerRatings");
        }
    }
}
