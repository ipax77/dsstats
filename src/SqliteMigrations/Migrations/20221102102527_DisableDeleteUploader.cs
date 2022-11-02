using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class DisableDeleteUploader : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BattleNetInfos_Uploaders_UploaderId",
                table: "BattleNetInfos");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UploadDisabledCount",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UploadIsDisabled",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadLastDisabled",
                table: "Uploaders",
                type: "TEXT",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "UploaderId",
                table: "BattleNetInfos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BattleNetInfos_Uploaders_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId",
                principalTable: "Uploaders",
                principalColumn: "UploaderId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BattleNetInfos_Uploaders_UploaderId",
                table: "BattleNetInfos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "UploadDisabledCount",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "UploadIsDisabled",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "UploadLastDisabled",
                table: "Uploaders");

            migrationBuilder.AlterColumn<int>(
                name: "UploaderId",
                table: "BattleNetInfos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_BattleNetInfos_Uploaders_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId",
                principalTable: "Uploaders",
                principalColumn: "UploaderId");
        }
    }
}
