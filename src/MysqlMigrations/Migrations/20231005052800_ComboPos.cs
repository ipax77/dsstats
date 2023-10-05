using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ComboPos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             var sp = @"DROP PROCEDURE IF EXISTS SetComboPlayerRatingPos;
CREATE PROCEDURE `SetComboPlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, PlayerId;
END";
            migrationBuilder.Sql(sp);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
