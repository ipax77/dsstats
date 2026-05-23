using dsstats.db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DsstatsContext))]
    [Migration("20260523213020_NullableSpawnUnitPositions")]
    public partial class NullableSpawnUnitPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "SpawnUnits_temp" (
                    "SpawnUnitId" INTEGER NOT NULL CONSTRAINT "PK_SpawnUnits" PRIMARY KEY AUTOINCREMENT,
                    "Count" INTEGER NOT NULL,
                    "Positions" TEXT NULL,
                    "UnitId" INTEGER NOT NULL,
                    "SpawnId" INTEGER NOT NULL,
                    CONSTRAINT "FK_SpawnUnits_Spawns_SpawnId" FOREIGN KEY ("SpawnId") REFERENCES "Spawns" ("SpawnId") ON DELETE CASCADE,
                    CONSTRAINT "FK_SpawnUnits_Units_UnitId" FOREIGN KEY ("UnitId") REFERENCES "Units" ("UnitId") ON DELETE CASCADE
                );
                INSERT INTO "SpawnUnits_temp" ("SpawnUnitId", "Count", "Positions", "UnitId", "SpawnId")
                SELECT "SpawnUnitId", "Count", "Positions", "UnitId", "SpawnId" FROM "SpawnUnits";
                DROP TABLE "SpawnUnits";
                ALTER TABLE "SpawnUnits_temp" RENAME TO "SpawnUnits";
                CREATE INDEX "IX_SpawnUnits_SpawnId" ON "SpawnUnits" ("SpawnId");
                CREATE INDEX "IX_SpawnUnits_UnitId" ON "SpawnUnits" ("UnitId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "SpawnUnits_temp" (
                    "SpawnUnitId" INTEGER NOT NULL CONSTRAINT "PK_SpawnUnits" PRIMARY KEY AUTOINCREMENT,
                    "Count" INTEGER NOT NULL,
                    "Positions" TEXT NOT NULL,
                    "UnitId" INTEGER NOT NULL,
                    "SpawnId" INTEGER NOT NULL,
                    CONSTRAINT "FK_SpawnUnits_Spawns_SpawnId" FOREIGN KEY ("SpawnId") REFERENCES "Spawns" ("SpawnId") ON DELETE CASCADE,
                    CONSTRAINT "FK_SpawnUnits_Units_UnitId" FOREIGN KEY ("UnitId") REFERENCES "Units" ("UnitId") ON DELETE CASCADE
                );
                INSERT INTO "SpawnUnits_temp" ("SpawnUnitId", "Count", "Positions", "UnitId", "SpawnId")
                SELECT "SpawnUnitId", "Count", COALESCE("Positions", '[]'), "UnitId", "SpawnId" FROM "SpawnUnits";
                DROP TABLE "SpawnUnits";
                ALTER TABLE "SpawnUnits_temp" RENAME TO "SpawnUnits";
                CREATE INDEX "IX_SpawnUnits_SpawnId" ON "SpawnUnits" ("SpawnId");
                CREATE INDEX "IX_SpawnUnits_UnitId" ON "SpawnUnits" ("UnitId");
                """);
        }
    }
}
