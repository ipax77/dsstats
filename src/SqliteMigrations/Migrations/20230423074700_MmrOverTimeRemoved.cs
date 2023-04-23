using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class MmrOverTimeRemoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "MmrOverTime",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "MmrOverTime",
                table: "ArcadePlayerRatings");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ArcadeReplays",
                newName: "BnetRecordId");

            migrationBuilder.AddColumn<long>(
                name: "BnetBucketId",
                table: "ArcadeReplays",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "ArcadeReplays",
                type: "TEXT",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReplayHash",
                table: "ArcadeReplays",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_RatingType",
                table: "ReplayRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "BnetBucketId", "BnetRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays",
                column: "ReplayHash");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_RatingType",
                table: "ArcadePlayerRatings",
                column: "RatingType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReplayRatings_RatingType",
                table: "ReplayRatings");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_ArcadePlayerRatings_RatingType",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropColumn(
                name: "BnetBucketId",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "ReplayHash",
                table: "ArcadeReplays");

            migrationBuilder.RenameColumn(
                name: "BnetRecordId",
                table: "ArcadeReplays",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "MmrOverTime",
                table: "PlayerRatings",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MmrOverTime",
                table: "ArcadePlayerRatings",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays",
                column: "Id");
        }
    }
}
