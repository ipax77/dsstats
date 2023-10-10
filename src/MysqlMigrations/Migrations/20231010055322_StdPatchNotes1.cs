using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class StdPatchNotes1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp =
            @"INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Adept cost reduced from 95 to 90');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Stalker cost increased from 105 to 110');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Disruptor cost increased from 225 to 250');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Void Ray cost reduced from 260 to 250');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Colossus cost reduced from 325 to 310');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Tempest cost reduced from 350 to 335');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Mothership cost reduced from 900 to 750');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (1, '2023-10-09 20:00:00', '1161025252390862889', ' - Extended Thermal Lance cost reduced from 100 to 75');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Thor cost increased from 400 to 415');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Liberator cost increased from 225 to 235');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Siege Tank cost reduced from 290 to 275');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Cyclone cost reduced from 140 to 125');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Medivac cost reduced from 125 to 120');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (2, '2023-10-09 20:00:00', '1161025252390862889', ' - Hi-Sec Auto Tracking cost reduced from 100 to 25');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (3, '2023-10-09 20:00:00', '1161025252390862889', ' - Queen cost reduced from 165 to 160');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (3, '2023-10-09 20:00:00', '1161025252390862889', ' - Hydralisk cost reduced from 100 to 95');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (3, '2023-10-09 20:00:00', '1161025252390862889', ' - Swarm Host cost reduced from  225 to 215');
INSERT INTO DsUpdates (Commander, `Time`, DiscordId, `Change`) values (3, '2023-10-09 20:00:00', '1161025252390862889', ' - Broodlord 375 to 325');
";
            migrationBuilder.Sql(sp);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
