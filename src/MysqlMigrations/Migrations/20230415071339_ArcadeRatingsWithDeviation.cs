using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeRatingsWithDeviation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"
                SET FOREIGN_KEY_CHECKS = 0;
                RENAME TABLE ArcadePlayerRatings TO t1;
                CREATE TABLE ArcadePlayerRatings LIKE t1;
                DROP TABLE t1;
                RENAME TABLE ArcadeReplayRatings TO t2;
                CREATE TABLE ArcadeReplayRatings LIKE t2;
                DROP TABLE t2;
                RENAME TABLE ArcadeReplayPlayerRatings TO t3;
                CREATE TABLE ArcadeReplayPlayerRatings LIKE t3;
                DROP TABLE t3;
                RENAME TABLE ArcadePlayerRatingChanges TO t4;
                CREATE TABLE ArcadePlayerRatingChanges LIKE t4;
                DROP TABLE t4;
                SET FOREIGN_KEY_CHECKS = 1;
            ";

            migrationBuilder.Sql(sp);

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "ArcadeReplayPlayerRatings");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "ArcadePlayerRatings");

            migrationBuilder.RenameColumn(
                name: "Consistency",
                table: "ArcadeReplayPlayerRatings",
                newName: "Deviation");

            migrationBuilder.RenameColumn(
                name: "Consistency",
                table: "ArcadePlayerRatings",
                newName: "Deviation");

            var sp2 = @"
                DROP PROCEDURE IF EXISTS PROC_DROP_FOREIGN_KEY;
                CREATE PROCEDURE PROC_DROP_FOREIGN_KEY(IN tableName VARCHAR(65), IN constraintName VARCHAR(65))
                BEGIN
                    IF EXISTS(
                        SELECT * FROM information_schema.table_constraints
                        WHERE 
                            table_schema    = DATABASE()     AND
                            table_name      = tableName      AND
                            constraint_name = constraintName AND
                            constraint_type = 'FOREIGN KEY')
                    THEN
                        SET @query = CONCAT('ALTER TABLE ', tableName, ' DROP FOREIGN KEY `', constraintName, '`;');
                        PREPARE stmt FROM @query; 
                        EXECUTE stmt; 
                        DEALLOCATE PREPARE stmt; 
                    END IF; 
                END
            ";
            migrationBuilder.Sql(sp2);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Deviation",
                table: "ArcadeReplayPlayerRatings",
                newName: "Consistency");

            migrationBuilder.RenameColumn(
                name: "Deviation",
                table: "ArcadePlayerRatings",
                newName: "Consistency");

            migrationBuilder.AddColumn<float>(
                name: "Confidence",
                table: "ArcadeReplayPlayerRatings",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "ArcadePlayerRatings",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
