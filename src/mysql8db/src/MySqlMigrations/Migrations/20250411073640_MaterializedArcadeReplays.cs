using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MaterializedArcadeReplays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_Imported",
                table: "ArcadeReplays",
                column: "Imported");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_Imported",
                table: "ArcadeReplays");
        }
    }
}
