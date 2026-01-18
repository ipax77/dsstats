using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MauiConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MauiConfig",
                columns: table => new
                {
                    MauiConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    CPUCores = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoDecode = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckForUpdates = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadCredential = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplayStartName = table.Column<string>(type: "TEXT", nullable: false),
                    Culture = table.Column<string>(type: "TEXT", nullable: false),
                    UploadAskTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    IgnoreReplays = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauiConfig", x => x.MauiConfigId);
                });

            migrationBuilder.CreateTable(
                name: "Sc2Profiles",
                columns: table => new
                {
                    Sc2ProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Folder = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ToonId_Region = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "INTEGER", nullable: false),
                    ToonId_Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    MauiConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sc2Profiles", x => x.Sc2ProfileId);
                    table.ForeignKey(
                        name: "FK_Sc2Profiles_MauiConfig_MauiConfigId",
                        column: x => x.MauiConfigId,
                        principalTable: "MauiConfig",
                        principalColumn: "MauiConfigId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_MauiConfigId",
                table: "Sc2Profiles",
                column: "MauiConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_Name",
                table: "Sc2Profiles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sc2Profiles");

            migrationBuilder.DropTable(
                name: "MauiConfig");
        }
    }
}
