using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MaterializedArcadeReplaysIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = "TRUNCATE TABLE MaterializedArcadeReplays;";
            migrationBuilder.Sql(sql);
            migrationBuilder.CreateIndex(
                name: "IX_MaterializedArcadeReplays_CreatedAt",
                table: "MaterializedArcadeReplays",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterializedArcadeReplays_CreatedAt",
                table: "MaterializedArcadeReplays");
        }
    }
}
