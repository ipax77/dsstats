using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ArcadePosFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var cleanup = "DROP PROCEDURE IF EXISTS `SetArcadePlayerRatingPos`;";
            migrationBuilder.Sql(cleanup);
            var SetArcadePlayerRatingPos = @"CREATE PROCEDURE `SetArcadePlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, PlayerId;
END
";
            migrationBuilder.Sql(SetArcadePlayerRatingPos);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
