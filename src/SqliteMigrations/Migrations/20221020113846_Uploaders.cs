using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Uploaders",
                columns: table => new {
                    UploaderId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", nullable: false),
                    BattleNetId = table.Column<int>(type: "INTEGER", nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Uploaders", x => x.UploaderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_UploaderId",
                table: "Players",
                column: "UploaderId");

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
                principalColumn: "UploaderId");
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
                type: "TEXT",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
