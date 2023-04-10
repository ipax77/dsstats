using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerRealmId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_ToonId",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "RealmId",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            var sp = @"UPDATE Players SET RealmId = 1;";
            migrationBuilder.Sql(sp);

            migrationBuilder.CreateIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players",
                columns: new[] { "RegionId", "RealmId", "ToonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_GameMode_CreatedAt",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "GameMode", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_RegionId_GameMode_CreatedAt",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ToonId",
                table: "Players",
                column: "ToonId",
                unique: true);
        }
    }
}
