using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class MainAndChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "Change",
            //    table: "PlayerRatings",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<int>(
            //    name: "Main",
            //    table: "PlayerRatings",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<int>(
            //    name: "MainCount",
            //    table: "PlayerRatings",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            migrationBuilder.Sql(@"
                ALTER TABLE PlayerRatings
                ADD COLUMN Main int NOT NULL DEFAULT 0 AFTER Mvps,
                ADD COLUMN MainCount int NOT NULL DEFAULT 0 AFTER Main,
                ADD COLUMN `Change` int NOT NULL DEFAULT 0 AFTER MainCount
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Change",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "Main",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "MainCount",
                table: "PlayerRatings");
        }
    }
}
