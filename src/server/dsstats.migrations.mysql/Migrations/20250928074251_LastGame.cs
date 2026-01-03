using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class LastGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "LastGame",
            //    table: "PlayerRatings",
            //    type: "datetime(0)",
            //    precision: 0,
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(@"
                ALTER TABLE PlayerRatings 
                ADD COLUMN LastGame datetime NOT NULL DEFAULT '2018-01-01 00:00:00' 
                AFTER Confidence"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_LastGame",
                table: "PlayerRatings",
                column: "LastGame");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerRatings_LastGame",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "LastGame",
                table: "PlayerRatings");
        }
    }
}
