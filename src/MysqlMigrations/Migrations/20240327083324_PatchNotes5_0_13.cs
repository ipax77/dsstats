using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PatchNotes5_0_13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Liberator Advanced ballistics range bonus reduced from 3 to 2.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Widow Mine Splash damage radius reduced from 1.75 to 1.5.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Cyclone Weapon cooldown increased from 0.48 to 0.58.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Cyclone Lock On now cooldown increased from 0 to 2.86.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Cyclone Weapon now has turret tracking, damage point reduced from 0.119 to 0.036.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Cyclone Health increased from 110 to 130.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (2, '2024-03-26', 0, '- Raven Interference Matrix can no longer target units already targeted or affected by Interference Matrix.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (3, '2024-03-26', 0, '- Infestor Fungal Growth range increased from 9 to 10.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (1, '2024-03-26', 0, '- Observer Health/Shields increased from 40/20 to 40/30.');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (1, '2024-03-26', 0, '- Sentry Damage increased from 6 to 6 (+4 vs Shields).');
INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES (1, '2024-03-26', 0, '- Sentry Light attribute tag removed.');
";
            var sql2 = @"UPDATE DsUnits SET Life = 130 WHERE Name = 'Cyclone' AND Commander = 2;
UPDATE DsUnits SET Shields = 30 WHERE Name = 'Observer' AND Commander = 1;
UPDATE DsUnits SET UnitType = 768 WHERE Name = 'Sentry' AND Commander = 1;
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
