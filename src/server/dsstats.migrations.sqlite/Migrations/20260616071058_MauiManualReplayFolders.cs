using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MauiManualReplayFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualReplayFolders",
                columns: table => new
                {
                    MauiReplayFolderId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Folder = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    MauiConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualReplayFolders", x => x.MauiReplayFolderId);
                    table.ForeignKey(
                        name: "FK_ManualReplayFolders_MauiConfig_MauiConfigId",
                        column: x => x.MauiConfigId,
                        principalTable: "MauiConfig",
                        principalColumn: "MauiConfigId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualReplayFolders_Folder",
                table: "ManualReplayFolders",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_ManualReplayFolders_MauiConfigId",
                table: "ManualReplayFolders",
                column: "MauiConfigId");

            migrationBuilder.Sql("""
                INSERT INTO ManualReplayFolders (Folder, Active, MauiConfigId)
                SELECT DISTINCT s.Folder, s.Active, s.MauiConfigId
                FROM Sc2Profiles s
                WHERE s.ToonId_Id <= 0
                    AND s.Folder IS NOT NULL
                    AND s.Folder <> ''
                    AND NOT EXISTS (
                        SELECT 1
                        FROM ManualReplayFolders f
                        WHERE f.MauiConfigId = s.MauiConfigId
                            AND f.Folder = s.Folder
                    );
                """);

            migrationBuilder.Sql("""
                DELETE FROM Sc2Profiles
                WHERE ToonId_Id <= 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualReplayFolders");
        }
    }
}
