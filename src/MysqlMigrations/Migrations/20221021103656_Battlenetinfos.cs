using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class Battlenetinfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Uploaders_BattleNetId",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "BattleNetId",
                table: "Uploaders");

            migrationBuilder.AlterColumn<int>(
                name: "UploaderId",
                table: "Players",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "BattleNetInfos",
                columns: table => new
                {
                    BattleNetInfoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BattleNetId = table.Column<int>(type: "int", nullable: false),
                    UploaderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleNetInfos", x => x.BattleNetInfoId);
                    table.ForeignKey(
                        name: "FK_BattleNetInfos_Uploaders_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Uploaders_AppGuid",
                table: "Uploaders",
                column: "AppGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleNetInfos_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players",
                column: "UploaderId",
                principalTable: "Uploaders",
                principalColumn: "UploaderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "BattleNetInfos");

            migrationBuilder.DropIndex(
                name: "IX_Uploaders_AppGuid",
                table: "Uploaders");

            migrationBuilder.AddColumn<int>(
                name: "BattleNetId",
                table: "Uploaders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UploaderId",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uploaders_BattleNetId",
                table: "Uploaders",
                column: "BattleNetId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players",
                column: "UploaderId",
                principalTable: "Uploaders",
                principalColumn: "UploaderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
