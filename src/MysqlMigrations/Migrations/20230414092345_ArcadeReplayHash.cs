using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeReplayHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "ArcadeReplays",
                type: "datetime(0)",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReplayHash",
                table: "ArcadeReplays",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays",
                column: "ReplayHash");


            var sp = @"
                DROP PROCEDURE IF EXISTS SetArcadePlayerRatingPos;
                CREATE PROCEDURE `SetArcadePlayerRatingPos`()
                BEGIN
	                SET @pos = 0;
                    UPDATE ArcadePlayerRatings
                    SET Pos = (@pos:=@pos+1)
                    WHERE RatingType = 1
                    ORDER BY Rating DESC, ArcadePlayerId;
    
                    SET @pos = 0;
                    UPDATE ArcadePlayerRatings
                    SET Pos = (@pos:=@pos+1)
                    WHERE RatingType = 2
                    ORDER BY Rating DESC, ArcadePlayerId;

                    SET @pos = 0;
                    UPDATE ArcadePlayerRatings
                    SET Pos = (@pos:=@pos+1)
                    WHERE RatingType = 3
                    ORDER BY Rating DESC, ArcadePlayerId;

                    SET @pos = 0;
                    UPDATE ArcadePlayerRatings
                    SET Pos = (@pos:=@pos+1)
                    WHERE RatingType = 4
                    ORDER BY Rating DESC, ArcadePlayerId;
                END
            ";

            migrationBuilder.Sql(sp);

            var sp2 = @"
                DROP PROCEDURE IF EXISTS SetArcadeRatingChange;
                CREATE PROCEDURE `SetArcadeRatingChange`()
                BEGIN
	                declare ratingType_counter int unsigned default 1;
	                declare timePeriod_counter int unsigned default 1;
	
                    declare ratingType_max int unsigned default 3;
	                declare timePeriod_max int unsigned default 4;
    
                    declare days int default -1;
                    declare hlimit int unsigned default 2;

                    SET FOREIGN_KEY_CHECKS = 0;
                    TRUNCATE ArcadePlayerRatingChanges;
                    SET FOREIGN_KEY_CHECKS = 1;
    
	                WHILE ratingType_counter < ratingType_max DO
		                WHILE timePeriod_counter < timePeriod_max DO
        
                            IF timePeriod_counter = 1 THEN
				                SET days = -1;
                                SET hlimit = 5;
			                ELSEIF timePeriod_counter = 2 THEN
				                SET days = -10;
                                SET hlimit = 10;
			                ELSE
				                SET days = -30;
                                SET hlimit = 20;
			                END IF;
            
			                CREATE TABLE IF NOT EXISTS TEMP_changetable AS (
				                SELECT `p`.`ArcadePlayerId`, `pr`.`ArcadePlayerRatingId`, ROUND(COALESCE(SUM(`r1`.`RatingChange`), 0.0), 2) AS `RatingChange`
				                  FROM `ArcadeReplays` AS `r`
				                  LEFT JOIN `ArcadeReplayRatings` AS `r0` ON `r`.`ArcadeReplayId` = `r0`.`ArcadeReplayId`
				                  INNER JOIN `ArcadeReplayPlayerRatings` AS `r1` ON `r0`.`ArcadeReplayRatingId` = `r1`.`ArcadeReplayRatingId`
				                  INNER JOIN `ArcadeReplayPlayers` AS `r2` ON `r1`.`ArcadeReplayPlayerId` = `r2`.`ArcadeReplayPlayerId`
				                  INNER JOIN `ArcadePlayers` AS `p` ON `r2`.`ArcadePlayerId` = `p`.`ArcadePlayerId`
                                  INNER JOIN `ArcadePlayerRatings` AS `pr` ON `pr`.`ArcadePlayerId` = `p`.`ArcadePlayerId`
				                  WHERE ((`r`.`CreatedAt` > DATE_ADD(now(), INTERVAL CAST(days * 24 AS signed) hour)) AND (`r0`.`RatingType` = ratingType_counter) AND (`pr`.`RatingType` = ratingType_counter) AND (`pr`.`Games` > 20))
				                  GROUP BY `p`.`ArcadePlayerId`, `pr`.`ArcadePlayerRatingId`
				                  HAVING COUNT(*) > hlimit
			                  );
			  
			                IF timePeriod_counter = 1 THEN
                              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				                SELECT ROUND(RatingChange, 2), 0, 0, ArcadePlayerRatingId
				                FROM TEMP_changetable
                                ON DUPLICATE KEY UPDATE Change24h =
                                (
					                SELECT ROUND(RatingChange, 2)
					                FROM TEMP_changetable
                                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				                )
			                  ;
			                ELSEIF timePeriod_counter = 2 THEN
                              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				                SELECT 0, ROUND(RatingChange, 2), 0, ArcadePlayerRatingId
				                FROM TEMP_changetable
                                ON DUPLICATE KEY UPDATE Change10d =
                                (
					                SELECT ROUND(RatingChange, 2)
					                FROM TEMP_changetable
                                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				                )
			                  ;
			                ELSE
                              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				                SELECT 0, 0, ROUND(RatingChange, 2), ArcadePlayerRatingId
				                FROM TEMP_changetable
                                ON DUPLICATE KEY UPDATE Change30d =
                                (
					                SELECT ROUND(RatingChange, 2)
					                FROM TEMP_changetable
                                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				                )
			                  ;
			                END IF;
             
		                  DROP TABLE IF EXISTS TEMP_changetable;
            
                          set timePeriod_counter = timePeriod_counter+1;
                        END WHILE;
		                set ratingType_counter=ratingType_counter+1;
                        set timePeriod_counter = 1;
                    END WHILE;
                END
            ";
            migrationBuilder.Sql(sp2);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "ArcadeReplays");

            migrationBuilder.DropColumn(
                name: "ReplayHash",
                table: "ArcadeReplays");
        }
    }
}
