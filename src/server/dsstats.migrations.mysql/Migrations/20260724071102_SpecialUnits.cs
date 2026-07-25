using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class SpecialUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `SpawnUnits`
                    ADD COLUMN `Special` INT NULL,
                    ALGORITHM=INSTANT;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `ReplayPlayers`
                    ADD COLUMN `ScanCount` INT NULL,
                    ALGORITHM=INSTANT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `SpawnUnits`
                    DROP COLUMN `Special`,
                    ALGORITHM=INSTANT;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `ReplayPlayers`
                    DROP COLUMN `ScanCount`,
                    ALGORITHM=INSTANT;
                """);
        }
    }
}
