using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class MauiConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Replays",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Uploaded",
                table: "Replays",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MauiConfig",
                columns: table => new
                {
                    MauiConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AppGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CPUCores = table.Column<int>(type: "int", nullable: false),
                    AutoDecode = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CheckForUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UploadCredential = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayStartName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Culture = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadAskTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    IgnoreReplays = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauiConfig", x => x.MauiConfigId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sc2Profiles",
                columns: table => new
                {
                    Sc2ProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Folder = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MauiConfigId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "Uploaded",
                table: "Replays");
        }
    }
}
