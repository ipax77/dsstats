using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Patch241005 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"UPDATE DsUnits SET Cost = 125 WHERE Commander = 20 and Name = 'Slayer';
UPDATE DsUnits SET Cost = 95 WHERE Commander = 40 and Name = 'Primal Hydralisk';
UPDATE DsUnits SET Cost = 825 WHERE Commander = 40 and Name = 'Tyrannozor';
UPDATE DsUnits SET Cost = 385 WHERE Commander = 50 and Name = 'Carrier';
UPDATE DsUnits SET Cost = 375 WHERE Commander = 50 and Name = 'Mojo';
UPDATE DsUnits SET Cost = 900 WHERE Commander = 50 and Name = 'Clolarion';
UPDATE DsUnits SET Cost = 170 WHERE Commander = 50 and Name = 'Conservator';
UPDATE DsUnits SET Cost = 150 WHERE Commander = 70 and Name = 'Mirage';
UPDATE DsUnits SET Cost = 95 WHERE Commander = 70 and Name = 'Sentinel';
UPDATE DsUnits SET Cost = 450 WHERE Commander = 90 and Name = 'Sky Furry';
UPDATE DsUnits SET Cost = 20 WHERE Commander = 90 and Name = 'Zergling';
UPDATE DsUnits SET Cost = 400 WHERE Commander = 120 and Name = 'Mecha Lurker';
";

            var sql2 = @"UPDATE DsUpgrades SET Cost = 125 WHERE Commander = 20 and Upgrade = 'Fusion Mortars';
UPDATE DsUpgrades SET Cost = 125 WHERE Commander = 20 and Upgrade = 'Bloodshard Resonance';
UPDATE DsUpgrades SET Cost = 125 WHERE Commander = 20 and Upgrade = 'Aerial Tracking';
UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 20 and Upgrade = 'Rapid Power Cycling';
UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 20 and Upgrade = 'Energy Shaping';
UPDATE DsUpgrades SET Cost = 25 WHERE Commander = 20 and Upgrade = 'Blood Shields';
UPDATE DsUpgrades SET Cost = 125 WHERE Commander = 40 and Upgrade = 'Barrage of Spikes';
UPDATE DsUpgrades SET Cost = 50 WHERE Commander = 40 and Upgrade = 'Tyrant\'s Protection';
UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 40 and Upgrade = 'Explosive Spores';
UPDATE DsUpgrades SET Cost = 75 WHERE Commander = 40 and Upgrade = 'Concentrated Fire';
UPDATE DsUpgrades SET Cost = 75 WHERE Commander = 120 and Upgrade = 'Focused Strike Algorithm';
UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 120 and Upgrade = 'BONUS Ravager!';
UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 160 and Upgrade = 'Disruption Web';
";

            var sql3 = @"UPDATE DsAbilities SET Cooldown = 120 WHERE Commander = 20 and Name = 'Structure Overcharge';
UPDATE DsAbilities SET Description = 'Douses enemy units in oil, reducing attack and movement speed by 50% and preventing them from cloaking. When a unit under the effect of Oil Spill is hit by a fire attack, it takes 7 (+7 vs Light) damage per second over 7 seconds. Lasts 4 seconds. Lasts 1 second for Heroic Units.' WHERE Commander = 150 and Name = 'Oil Spill';
UPDATE DsAbilities SET Description = 'Permanently cloaks targeted unit. Increases ranged unit ranges by 2' WHERE Commander = 160 and Name = 'Dark Pylon';
";

            var sql4 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) 
VALUES (20, '2024-10-05', 0, '- Structure Overcharge from 180 seconds to 120 seconds, and now holds up to 3 charges.'),
       (20, '2024-10-05', 0, '- Slayer reduced from 130 to 125.'),
       (20, '2024-10-05', 0, '- Fusion Mortars reduced down from 150 to 125.'),
       (20, '2024-10-05', 0, '- Blood Shard Resonance reduced down from 150 to 125.'),
       (20, '2024-10-05', 0, '- Ariel Tracking reduced down from 150 to 125.'),
       (20, '2024-10-05', 0, '- Rapid Power Cycling reduced down from 150 to 100.'),
       (20, '2024-10-05', 0, '- Energy Shaping reduced down from 150 to 100.'),
       (20, '2024-10-05', 0, '- Blood Shields reduced down from 125 to 25.'),
       (40, '2024-10-05', 0, '- Primal Hydralisk cost reduced from 100 to 95.'),
       (40, '2024-10-05', 0, '- Tyrannosaur cost reduced from 850 to 825.'),
       (40, '2024-10-05', 0, '- Barrage of Spikes cost reduced from 150 minerals to 125.'),
       (40, '2024-10-05', 0, '- Tyrant Protection cost reduced from 100 minerals to 50.'),
       (40, '2024-10-05', 0, '- Explosive Spores cost reduced from 125 to 100.'),
       (40, '2024-10-05', 0, '- Concentrated Fire cost reduced from 100 to 75.'),
       (50, '2024-10-05', 0, '- Carriers cost reduced from 390 to 385.'),
       (50, '2024-10-05', 0, '- Mojo cost reduced from 400 to 375.'),
       (50, '2024-10-05', 0, '- Clolarion cost reduced from 925 to 900.'),
       (50, '2024-10-05', 0, '- Conservator cost reduced from 175 to 170.'),
       (50, '2024-10-05', 0, '- Interdictors cost reduced from 150 to 100.'),
       (70, '2024-10-05', 0, '- Purifier Beam changed from 350 (+350 vs Armored) damage over 5 seconds, to 250 (+250 vs Armored) damage over 4 seconds.'),
       (70, '2024-10-05', 0, '- Mirage cost increased from 145 to 150.'),
       (70, '2024-10-05', 0, '- Sentinel cost increased from 90 to 95.'),
       (90, '2024-10-05', 0, '- Sky Furry cost increased from 425 to 450.'),
       (90, '2024-10-05', 0, '- Zergling cost increased from 18 to 20.'),
       (120, '2024-10-05', 0, '- Mecha Lurker\'s Tunnel of TERROR Algorithm now autocasts on units over 100 vitality.'),
       (120, '2024-10-05', 0, '- Mecha lurkers cost increased from 385 to 400.'),
       (120, '2024-10-05', 0, '- Focused Strike Algorithm cost reduced from 150 to 75.'),
       (120, '2024-10-05', 0, '- Bonus Ravager cost reduced from 125 to 100.'),
       (150, '2024-10-05', 0, '- Blaze oil spill now reduces attack speed and move speed of units from 75% to 50%, but lasts 4 seconds, up from 2.'),
       (160, '2024-10-05', 0, '- Dark Pylon increases ranged unit ranges by 2.'),
       (160, '2024-10-05', 0, '- Stasis calibration cost reduced from 150 to 100.'),
       (160, '2024-10-05', 0, '- Disruption web cost reduced from 150 to 100.'),
       (60, '2024-10-05', 0, '- Soverign Battlecruiser may once again be purchased (no longer grants Significant Other bonuses).'),
       (140, '2024-10-05', 0, '- Ares unit radius reduced from 0.8125 to 0.75.');";

            migrationBuilder.Sql(sql1);
            migrationBuilder.Sql(sql2);
            migrationBuilder.Sql(sql3);
            migrationBuilder.Sql(sql4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
