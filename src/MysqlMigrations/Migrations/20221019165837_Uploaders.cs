using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class Uploaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestUpload",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "UploaderId",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Uploaders",
                columns: table => new
                {
                    UploaderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AppGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppVersion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BattleNetId = table.Column<int>(type: "int", nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploaders", x => x.UploaderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UploaderId",
                table: "Players",
                column: "UploaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players",
                column: "UploaderId",
                principalTable: "Uploaders",
                principalColumn: "UploaderId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Uploaders_UploaderId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "Uploaders");

            migrationBuilder.DropIndex(
                name: "IX_Players_UploaderId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UploaderId",
                table: "Players");

            migrationBuilder.AddColumn<DateTime>(
                name: "LatestUpload",
                table: "Players",
                type: "datetime(0)",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
