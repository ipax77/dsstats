using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Patch250129 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES 
    (10, '2025-01-29', 0, '- Viper cost increased from 220 to 250'),
    (30, '2025-01-29', 0, '- Dragoon reduced from 125 to 120'),
    (30, '2025-01-29', 0, '- Honor Guard reduced from 80 to 75'),
    (30, '2025-01-29', 0, '- High Archon reduced from 340 to 330'),
    (30, '2025-01-29', 0, '- Immortal reduced from 225 to 215'),
    (30, '2025-01-29', 0, '- High Templar from 180 to 175'),
    (30, '2025-01-29', 0, '- Reaver from 425 to 420'),
    (40, '2025-01-29', 0, '- Primal Wurm now increases in cost with each purchase, but he once again has access to 3 wurms'),
    (60, '2025-01-29', 0, '- Hellion reduced from 125 to 120'),
    (60, '2025-01-29', 0, '- Hellbat reduced from 125 to 120'),
    (60, '2025-01-29', 0, '- Executioner Missiles upgrade cost reduced from 150 to 100'),
    (60, '2025-01-29', 0, '- Black Market Launchers upgrade cost reduced from 125 to 100'),
    (60, '2025-01-29', 0, '- Sovereign Battlecruiser now gives 5% attack speed to Han\'s forces'),
    (70, '2025-01-29', 0, '- Orbital Strike Beacon cd reduced from 240 to 225'),
    (70, '2025-01-29', 0, '- Orbital Strike Beacon deals half damage to structures'),
    (70, '2025-01-29', 0, '- Optimized Ordnance cost reduced from 125 to 75'),
    (70, '2025-01-29', 0, '- Energizer reduced from 145 to 140'),
    (70, '2025-01-29', 0, '- Annihilator reduced from 270 to 260'),
    (70, '2025-01-29', 0, '- Mirage reduced from 150 to 145'),
    (70, '2025-01-29', 0, '- Sentinel reduced from 95 to 90'),
    (80, '2025-01-29', 0, '- Lurker price reduced from 295 to 290'),
    (80, '2025-01-29', 0, '- Queen price reduced from 150 to 145'),
    (80, '2025-01-29', 0, '- Brood Lord price reduced from 320 to 310'),
    (90, '2025-01-29', 0, '- Pride of Augustgrad now has the \'Promote to Flagship\' ability'),
    (90, '2025-01-29', 0, '- Ultralisk reduced from 275 to 260'),
    (100, '2025-01-29', 0, '- Hellbat Ranger reduced from 300 to 290'),
    (100, '2025-01-29', 0, '- Heavy Siege Tank reduced from 340 to 325'),
    (130, '2025-01-29', 0, '- Volatile Infested cost reduced from 50 to 45'),
    (130, '2025-01-29', 0, '- Infested Marine cost reduced from 38 to 35'),
    (140, '2025-01-29', 0, '- Drakken Laser Drill level 2 now also increases lock-on speed'),
    (140, '2025-01-29', 0, '- Drakken Laser Drill cost reduced from 200 to 175'),
    (140, '2025-01-29', 0, '- Wraith reduced from 145 to 140'),
    (140, '2025-01-29', 0, '- Cyclone reduced from 170 to 165'),
    (140, '2025-01-29', 0, '- A.R.E.S reduced from 275 to 260'),
    (160, '2025-01-29', 0, '- Black hole duration reduced from 6 to 5 seconds'),
    (160, '2025-01-29', 0, '- Stalkers increased from 115 to 120'),
    (160, '2025-01-29', 0, '- Dark Archon reduced from 350 to 340'),
    (170, '2025-01-29', 0, '- Mass Frenzy can now hold 2 charges'),
    (170, '2025-01-29', 0, '- Rupture cost reduced from 200 to 150'),
    (170, '2025-01-29', 0, '- Virulent Spores upgrade cost reduced from 125 to 100');
            ";
                migrationBuilder.Sql(sql1);

            var sql2 = @"UPDATE DsUnits SET Cost = 250 WHERE Commander = 10 and Name = 'Viper';
    UPDATE DsUnits SET Cost = 120 WHERE Commander = 30 and Name = 'Dragoon';
    UPDATE DsUnits SET Cost = 75 WHERE Commander = 30 and Name = 'Honor Guard';
    UPDATE DsUnits SET Cost = 330 WHERE Commander = 30 and Name = 'High Archon';
    UPDATE DsUnits SET Cost = 215 WHERE Commander = 30 and Name = 'Immortal';
    UPDATE DsUnits SET Cost = 175 WHERE Commander = 30 and Name = 'High Templar';
    UPDATE DsUnits SET Cost = 420 WHERE Commander = 30 and Name = 'Reaver';
    UPDATE DsUnits SET Cost = 120 WHERE Commander = 60 and Name = 'Hellion';
    UPDATE DsUnits SET Cost = 120 WHERE Commander = 60 and Name = 'Hellbat';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 60 and Upgrade = 'Executioner Missiles';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 60 and Upgrade = 'Black Market Launchers';
    UPDATE DsUnits SET Cost = 140 WHERE Commander = 70 and Name = 'Energizer';
    UPDATE DsUnits SET Cost = 260 WHERE Commander = 70 and Name = 'Annihilator';
    UPDATE DsUnits SET Cost = 145 WHERE Commander = 70 and Name = 'Mirage';
    UPDATE DsUnits SET Cost = 90 WHERE Commander = 70 and Name = 'Sentinel';
    UPDATE DsUnits SET Cost = 290 WHERE Commander = 80 and Name = 'Lurker';
    UPDATE DsUnits SET Cost = 145 WHERE Commander = 80 and Name = 'Queen';
    UPDATE DsUnits SET Cost = 310 WHERE Commander = 80 and Name = 'Brood Lord';
    UPDATE DsUnits SET Cost = 260 WHERE Commander = 90 and Name = 'Ultralisk';
    UPDATE DsUnits SET Cost = 290 WHERE Commander = 100 and Name = 'Hellbat Ranger';
    UPDATE DsUnits SET Cost = 325 WHERE Commander = 100 and Name = 'Heavy Siege Tank';
    UPDATE DsUnits SET Cost = 45 WHERE Commander = 130 and Name = 'Volatile Infested';
    UPDATE DsUnits SET Cost = 35 WHERE Commander = 130 and Name = 'Infested Marine';
    UPDATE DsUnits SET Cost = 140 WHERE Commander = 140 and Name = 'Wraith';
    UPDATE DsUnits SET Cost = 165 WHERE Commander = 140 and Name = 'Cyclone';
    UPDATE DsUnits SET Cost = 260 WHERE Commander = 140 and Name = 'A.R.E.S.';
    UPDATE DsUnits SET Cost = 120 WHERE Commander = 160 and Name = 'Stalker';
    UPDATE DsUnits SET Cost = 340 WHERE Commander = 160 and Name = 'Dark Archon';
    UPDATE DsUpgrades SET Cost = 150 WHERE Commander = 170 and Upgrade = 'Rupture';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 170 and Upgrade = 'Virulent Spores';
    ";
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed
        }
    }
}
