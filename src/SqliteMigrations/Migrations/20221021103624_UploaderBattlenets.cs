using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class UploaderBattlenets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Uploaders_BattleNetId",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "BattleNetId",
                table: "Uploaders");

            migrationBuilder.CreateTable(
                name: "BattleNetInfos",
                columns: table => new {
                    BattleNetInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BattleNetId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploaderId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_BattleNetInfos", x => x.BattleNetInfoId);
                    table.ForeignKey(
                        name: "FK_BattleNetInfos_Uploaders_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uploaders_AppGuid",
                table: "Uploaders",
                column: "AppGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleNetInfos_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleNetInfos");

            migrationBuilder.DropIndex(
                name: "IX_Uploaders_AppGuid",
                table: "Uploaders");

            migrationBuilder.AddColumn<int>(
                name: "BattleNetId",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Uploaders_BattleNetId",
                table: "Uploaders",
                column: "BattleNetId",
                unique: true);
        }
    }
}
