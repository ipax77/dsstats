using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class CombinedReplays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CombinedReplays",
                columns: table => new
                {
                    CombinedReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayId = table.Column<int>(type: "int", nullable: true),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: true),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Gametime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    TE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedReplays", x => x.CombinedReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_ArcadeReplayId",
                table: "CombinedReplays",
                column: "ArcadeReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_Gametime",
                table: "CombinedReplays",
                column: "Gametime");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_Imported",
                table: "CombinedReplays",
                column: "Imported");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_ReplayId",
                table: "CombinedReplays",
                column: "ReplayId",
                unique: true);

            var sql = @"DROP PROCEDURE IF EXISTS BatchImportCombinedReplays;
CREATE PROCEDURE BatchImportCombinedReplays()
BEGIN
    DECLARE lastReplayImported DATETIME;
    DECLARE lastArcadeImported DATETIME;

    -- last imported timestamps
    SELECT COALESCE(MAX(Imported), '1900-01-01') INTO lastReplayImported
    FROM CombinedReplays
    WHERE ReplayId IS NOT NULL;

    SELECT COALESCE(MAX(Imported), '1900-01-01') INTO lastArcadeImported
    FROM CombinedReplays
    WHERE ArcadeReplayId IS NOT NULL;

    -- insert new normal replays
    INSERT IGNORE INTO CombinedReplays
    (ReplayId, ArcadeReplayId, Gametime, GameMode, Duration, TE, PlayerCount, WinnerTeam, Imported)
    SELECT
        r.ReplayId, NULL, r.Gametime, r.GameMode, r.Duration, r.TE, r.PlayerCount, r.WinnerTeam, r.Imported
    FROM Replays r
    WHERE r.PlayerCount > 1
      AND r.Duration > 300
      AND r.WinnerTeam > 0
      AND r.Imported >= lastReplayImported;

    -- insert new arcade replays
    INSERT IGNORE INTO CombinedReplays
    (ReplayId, ArcadeReplayId, Gametime, GameMode, Duration, TE, PlayerCount, WinnerTeam, Imported)
    SELECT
        NULL, a.ArcadeReplayId, a.CreatedAt, a.GameMode, a.Duration, 0, a.PlayerCount, a.WinnerTeam, a.Imported
    FROM ArcadeReplays a
    WHERE a.PlayerCount = 6
      AND a.Duration > 300
      AND a.WinnerTeam > 0
      AND a.Imported >= lastArcadeImported;
END;";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS BatchImportCombinedReplays;");
            migrationBuilder.DropTable(
                name: "CombinedReplays");
        }
    }
}
