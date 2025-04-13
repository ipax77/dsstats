using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MainCmdr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Main",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainCount",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            var sql = @"
                ALTER TABLE PlayerRatings MODIFY Main int NOT NULL AFTER Confidence;
                ALTER TABLE PlayerRatings MODIFY MainCount int NOT NULL AFTER Main;";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Main",
                table: "PlayerRatings");

            migrationBuilder.DropColumn(
                name: "MainCount",
                table: "PlayerRatings");
        }
    }
}
