using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.postgresql.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260523213040_NullableSpawnUnitPositions")]
    public partial class NullableSpawnUnitPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int[]>(
                name: "Positions",
                table: "SpawnUnits",
                type: "integer[]",
                nullable: true,
                oldClrType: typeof(int[]),
                oldType: "integer[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int[]>(
                name: "Positions",
                table: "SpawnUnits",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]",
                oldNullable: true);
        }
    }
}
