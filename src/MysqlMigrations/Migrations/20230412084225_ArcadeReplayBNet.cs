using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeReplayBNet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ArcadeReplays");

            migrationBuilder.AddColumn<long>(
                name: "BnetBucketId",
                table: "ArcadeReplays",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BnetRecordId",
                table: "ArcadeReplays",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "BnetBucketId", "BnetRecordId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "BnetBucketId",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "BnetRecordId",
                table: "ArcadeReplays");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ArcadeReplays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays",
                column: "Id");
        }
    }
}
