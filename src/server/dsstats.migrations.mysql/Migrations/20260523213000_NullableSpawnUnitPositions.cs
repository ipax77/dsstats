using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260523213000_NullableSpawnUnitPositions")]
    public partial class NullableSpawnUnitPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `SpawnUnits`
                MODIFY COLUMN `Positions` longtext NULL,
                ALGORITHM=INPLACE,
                LOCK=NONE
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `SpawnUnits`
                SET `Positions` = '[]'
                WHERE `Positions` IS NULL
                """);
            migrationBuilder.Sql("""
                ALTER TABLE `SpawnUnits`
                MODIFY COLUMN `Positions` longtext NOT NULL,
                ALGORITHM=INPLACE,
                LOCK=NONE
                """);
        }
    }
}
