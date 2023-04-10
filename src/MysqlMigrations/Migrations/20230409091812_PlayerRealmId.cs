using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
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
                type: "int",
                nullable: false,
                defaultValue: 0);

            var sp = @"UPDATE Players SET RealmId = 1;";
            migrationBuilder.Sql(sp);

            migrationBuilder.CreateIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players",
                columns: new[] { "RegionId", "RealmId", "ToonId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players");

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
