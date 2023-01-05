using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class PlayerRatingsRowNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Pos",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            var sp = @"CREATE PROCEDURE `SetPlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;
END";
            migrationBuilder.Sql(sp);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pos",
                table: "PlayerRatings");
        }
    }
}
