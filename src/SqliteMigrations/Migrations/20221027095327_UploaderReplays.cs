using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class UploaderReplays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UploaderReplays",
                columns: table => new
                {
                    ReplaysReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadersUploaderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploaderReplays", x => new { x.ReplaysReplayId, x.UploadersUploaderId });
                    table.ForeignKey(
                        name: "FK_UploaderReplays_Replays_ReplaysReplayId",
                        column: x => x.ReplaysReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploaderReplays_Uploaders_UploadersUploaderId",
                        column: x => x.UploadersUploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploaderReplays_UploadersUploaderId",
                table: "UploaderReplays",
                column: "UploadersUploaderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploaderReplays");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Players");
        }
    }
}
