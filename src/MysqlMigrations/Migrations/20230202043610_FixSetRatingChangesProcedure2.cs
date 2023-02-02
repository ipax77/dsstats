using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class FixSetRatingChangesProcedure2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"
	DROP PROCEDURE IF EXISTS SetRatingChange;
	CREATE PROCEDURE `SetRatingChange`()
BEGIN
	declare ratingType_counter int unsigned default 1;
	declare timePeriod_counter int unsigned default 1;
	
    declare ratingType_max int unsigned default 5;
	declare timePeriod_max int unsigned default 4;
    
    declare days int default -1;
    declare hlimit int unsigned default 2;

    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE PlayerRatingChanges;
    SET FOREIGN_KEY_CHECKS = 1;
    
	WHILE ratingType_counter < ratingType_max DO
		WHILE timePeriod_counter < timePeriod_max DO
        
            IF timePeriod_counter = 1 THEN
				SET days = -1;
                SET hlimit = 0;
			ELSEIF timePeriod_counter = 2 THEN
				SET days = -10;
                SET hlimit = 5;
			ELSE
				SET days = -30;
                SET hlimit = 10;
			END IF;
            
			CREATE TABLE IF NOT EXISTS TEMP_changetable AS (
				SELECT `p`.`PlayerId`, `pr`.`PlayerRatingId`, ROUND(COALESCE(SUM(`r1`.`RatingChange`), 0.0), 2) AS `RatingChange`
				  FROM `Replays` AS `r`
				  LEFT JOIN `ReplayRatings` AS `r0` ON `r`.`ReplayId` = `r0`.`ReplayId`
				  INNER JOIN `RepPlayerRatings` AS `r1` ON `r0`.`ReplayRatingId` = `r1`.`ReplayRatingInfoId`
				  INNER JOIN `ReplayPlayers` AS `r2` ON `r1`.`ReplayPlayerId` = `r2`.`ReplayPlayerId`
				  INNER JOIN `Players` AS `p` ON `r2`.`PlayerId` = `p`.`PlayerId`
                  INNER JOIN `PlayerRatings` AS `pr` ON `pr`.`PlayerId` = `p`.`PlayerId`
				  WHERE ((`r`.`GameTime` > DATE_ADD(now(), INTERVAL CAST(days * 24 AS signed) hour)) AND `p`.`UploaderId` IS NOT NULL) AND (`r0`.`RatingType` = ratingType_counter) AND (`pr`.`RatingType` = ratingType_counter)
				  GROUP BY `p`.`PlayerId`, `pr`.`PlayerRatingId`
				  HAVING COUNT(*) > hlimit
			  );
			  
			IF timePeriod_counter = 1 THEN
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT ROUND(RatingChange, 2), 0, 0, PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change24h =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			ELSEIF timePeriod_counter = 2 THEN
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT 0, ROUND(RatingChange, 2), 0, PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change10d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			ELSE
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT 0, 0, ROUND(RatingChange, 2), PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change30d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			END IF;
             
		  DROP TABLE IF EXISTS TEMP_changetable;
            
          set timePeriod_counter = timePeriod_counter+1;
        END WHILE;
		set ratingType_counter=ratingType_counter+1;
        set timePeriod_counter = 1;
    END WHILE;
END";
            migrationBuilder.Sql(sp);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
