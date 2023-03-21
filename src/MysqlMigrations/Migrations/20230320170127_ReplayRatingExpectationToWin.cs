using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ReplayRatingExpectationToWin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ExpectationToWin",
                table: "ReplayRatings",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            var sp = @"ALTER TABLE ReplayRatings MODIFY ReplayId int(11) AFTER ExpectationToWin;";
            migrationBuilder.Sql(sp);                
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectationToWin",
                table: "ReplayRatings");
        }
    }
}
