using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DsUnits0624 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"UPDATE DsUnits SET Cost = 80 WHERE Commander = 10 and Name = 'Vile Roach';
UPDATE DsUnits SET Cost = 425 WHERE Commander = 20 and Name = 'Vanguard';
UPDATE DsUnits SET Cost = 300 WHERE Commander = 20 and Name = 'Ascendant';
UPDATE DsUnits SET Cost = 1250 WHERE Commander = 20 and Name = 'Tal\'darim Mothership';
UPDATE DsUnits SET Cost = 295 WHERE Commander = 80 and Name = 'Lurker';
UPDATE DsUnits SET Cost = 320 WHERE Commander = 80 and Name = 'Brood Lord';
UPDATE DsUnits SET Cost = 350 WHERE Commander = 160 and Name = 'Dark Archon';
UPDATE DsUpgrades SET Cost = 75 WHERE Commander = 80 and Upgrade = 'Seismic Spines';
";

            var sql2 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (10, '2024-06-25', 0, '- Vile Roach cost increased from 75 to 80.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (20, '2024-06-25', 0, '- Vanguard cost reduced from 435 to 425.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (20, '2024-06-25', 0, '- Ascendant cost reduced from 310 to 300.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (20, '2024-06-25', 0, '- Tal\'darim Mothership cost reduced from 1275 to 1250.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (80, '2024-06-25', 0, '- Lurker cost reduced from 300 to 295.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (80, '2024-06-25', 0, '- Brood Lord cost reduced from 325 to 320.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (80, '2024-06-25', 0, '- Siesmic Spines upgrade cost reduced from 100 to 75.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (160, '2024-06-25', 0, '- Dark Archon cost reduced from 360 to 350.');
";

            migrationBuilder.Sql(sql1);
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
