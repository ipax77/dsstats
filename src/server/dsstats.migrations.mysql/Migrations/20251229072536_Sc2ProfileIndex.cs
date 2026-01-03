using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class Sc2ProfileIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" },
                unique: true);
        }
    }
}
