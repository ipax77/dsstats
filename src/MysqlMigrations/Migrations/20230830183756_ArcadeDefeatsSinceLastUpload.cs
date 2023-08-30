using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeDefeatsSinceLastUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DamageEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Breakpoint = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    AvgKills = table.Column<int>(type: "int", nullable: false),
                    AvgArmy = table.Column<int>(type: "int", nullable: false),
                    AvgUpgrades = table.Column<int>(type: "int", nullable: false),
                    AvgGas = table.Column<double>(type: "double", nullable: false),
                    AvgIncome = table.Column<int>(type: "int", nullable: false),
                    AvgAPM = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SynergyEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Teammate = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<double>(type: "double", nullable: false),
                    AvgGain = table.Column<double>(type: "double", nullable: false),
                    NormalizedAvgGain = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WinrateEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<double>(type: "double", nullable: false),
                    AvgGain = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            var sp = @"CREATE PROCEDURE `SetPlayerRatingPosWithDefeats`()
BEGIN
	SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating - ArcadeDefeatsSinceLastUpload * 25 DESC, PlayerId;
END";
            migrationBuilder.Sql(sp);

            var sp2 =
@"INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Soverign Battlecruisers now only spawn once.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Soverign Battlecruiser charge count reduced from infinite to 1.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Soverign Battlecruiser first comes off cooldown at 400 seconds. Subsequent SBCs have a cooldown of 360.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Hellbat cost reduced from 145 to 125.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Hellion cost reduced from 145 to 125.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Reaper cost reduced from 60 to 55.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Widow Mine cost reduced from 190 to 160.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Widow Mine Significant Other bonus reduced from 1.5% to 1%.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Theia Raven cost reduced from 140 to 125.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Asteria Wraith Significant Other bonus reduced from 2.5% to 2%.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Deimos Viking Significant Other bonus reduced from 2.5% to 2%.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Strike Fighter cost reduced from 250 to 210.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Hangar Bay cost reduced from 175 to 125.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Tactical Jump cost reduced from 150 to 100');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Asteria Wraith cost reduced from 380 to 330.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Deimos Viking cost reduced from 430 to 370.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Galleon Significant Other bonus reduced from 3.5% to 3%.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (60, '2023-08-30 18:00:00', '1146516609679237150', ' - Galleon cost reduced from 540 to 450.');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (30, '2023-08-30 18:00:00', '1146516609679237150', ' - Shield Overcharge benefit on allies reduced from 100 to 50.');
";

            migrationBuilder.Sql(sp2);

            var sp3 = "ALTER TABLE PlayerRatings MODIFY COLUMN ArcadeDefeatsSinceLastUpload int(11) AFTER Pos;";
            migrationBuilder.Sql(sp3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamageEnts");

            migrationBuilder.DropTable(
                name: "SynergyEnts");

            migrationBuilder.DropTable(
                name: "WinrateEnts");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings");
        }
    }
}
