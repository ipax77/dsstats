using Microsoft.EntityFrameworkCore.Migrations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Patch250905 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql1 = @"INSERT INTO DsUpdates (`Commander`, `Time`, `DiscordId`, `Change`) VALUES
    -- Han & Horner (Commander = 60)
    (60, '2025-09-06', 0, '- Weapon reduced from 150/225/300 -> 125/200/250'),
    (60, '2025-09-06', 0, '- Deimos Viking reduced from 370 -> 360'),
    (60, '2025-09-06', 0, '- Asteria Wraith reduced from 330 -> 325'),
    (60, '2025-09-06', 0, '- Assault Galleon reduced from 450 -> 440'),
    (60, '2025-09-06', 0, '- Theia Raven reduced from 125 -> 120'),
    (60, '2025-09-06', 0, '- Mag Mine: Cooldown increased from 360 -> 420'),
    (60, '2025-09-06', 0, '- Drone Hanger reduced from 125 -> 100'),
    (60, '2025-09-06', 0, '- Call in the Fleet: Cooldown reduced from 360 -> 300'),
    (60, '2025-09-06', 0, '- Strike Fighter: Range increased from 6 -> 7'),

    -- Karax (Commander = 70)
    (70, '2025-09-06', 0, '- Ground Weapons reduced from 150/200/250 -> 125/175/225'),
    (70, '2025-09-06', 0, '- Shields reduced from 125/175/225 -> 100/150/200'),
    (70, '2025-09-06', 0, '- Air Weapons reduced from 125/175/225 -> 100/150/200'),
    (70, '2025-09-06', 0, '- Air Armor reduced from 125/175/225 -> 100/150/200'),
    (70, '2025-09-06', 0, '- Energizer reduced from 140 -> 135'),
    (70, '2025-09-06', 0, '- Mirage reduced from 145 -> 140'),
    (70, '2025-09-06', 0, '- Orbital Beacon: Cooldown reduced from 225 -> 200 seconds'),
    (70, '2025-09-06', 0, '- Sentinel reduced from 90 -> 85'),
    (70, '2025-09-06', 0, '- Annihilator reduced from 260 -> 255'),

    -- Raynor (Commander = 110)
    (110, '2025-09-06', 0, '- Dusk Wing reduced from 200 -> 190'),
    (110, '2025-09-06', 0, '- Viking reduced from 205 -> 200'),
    (110, '2025-09-06', 0, '- Siege Tank reduced from 240 -> 235'),
    (110, '2025-09-06', 0, '- Banshee reduced from 180 -> 175'),
    (110, '2025-09-06', 0, '- Battlecruiser increased from 460 -> 465'),
    (110, '2025-09-06', 0, '- Hyperion reduced from 1950 -> 1900'),
    (110, '2025-09-06', 0, '- Hyperion Yamato Cannon: Cooldown reduced from 300 -> 240'),
    (110, '2025-09-06', 0, '- Vehicle & Ship Weapons reduced from 150/200/250 -> 125/175/225'),
    (110, '2025-09-06', 0, '- Vehicle & Ship Plating reduced from 125/150/200 -> 100/125/175'),
    (110, '2025-09-06', 0, '- Cloaking Field (Banshee Cloak) reduced from 125 -> 100'),
    (110, '2025-09-06', 0, '- Shockwave Missile Battery (Banshee AoE) reduced from 150 -> 125'),
    (110, '2025-09-06', 0, '- Phobos Weapon System (Viking Range) reduced from 125 -> 100'),
    (110, '2025-09-06', 0, '- Ripwave Missiles (Viking AoE) reduced from 100 -> 75'),
    (110, '2025-09-06', 0, '- Advanced Siege Tech (Tank Transform Speed & Armor) reduced from 100 -> 75'),
    (110, '2025-09-06', 0, '- Advanced Targeting System (Hyperion Attack Aura) reduced from 200 -> 150'),

    -- Zagara (Commander = 170)
    (170, '2025-09-06', 0, '- Aberration reduced from 350 -> 340'),
    (170, '2025-09-06', 0, '- Scourge reduced from 65 -> 60'),
    (170, '2025-09-06', 0, '- Corruptor reduced from 200 -> 190'),
    (170, '2025-09-06', 0, '- Zagara (Hero) reduced from 250 -> 225'),
    (170, '2025-09-06', 0, '- Queens reduced from 140 -> 135'),
    (170, '2025-09-06', 0, '- Swarmling reduced from 18 -> 17'),
    (170, '2025-09-06', 0, '- Melee Weapons reduced from 150/225/300 -> 125/200/275'),
    (170, '2025-09-06', 0, '- Medusa Blades (Zagara Multi-Hit Upgrade): Now Tier 1'),
    (170, '2025-09-06', 0, '- Broodmother (Extra Banelings on Ability): Now Tier 1'),

    -- Abathur (Commander = 10)
    (10, '2025-09-06', 0, '- Swarm Queen reduced from 115 -> 110'),
    (10, '2025-09-06', 0, '- Vile Roach reduced from 80 -> 75'),
    (10, '2025-09-06', 0, '- Ravager increased from 225 -> 240'),
    (10, '2025-09-06', 0, '- Guardian reduced from 225 -> 220'),
    (10, '2025-09-06', 0, '- Brutalisk reduced from 650 -> 625'),
    (10, '2025-09-06', 0, '- Leviathan reduced from 825 -> 800'),

    -- Nova (Commander = 100)
    (100, '2025-09-06', 0, '- Defensive Drone: Charge cooldown increased from 120 -> 150'),

    -- Mengsk (Commander = 90)
    (90, '2025-09-06', 0, '- Pride of Augustgrad increased from 1400 -> 1450'),

    -- Swann (Commander = 140)
    (140, '2025-09-06', 0, '- Drakken Laser Drill reduced from 175 -> 150'),
    (140, '2025-09-06', 0, '- Siege Tank reduced from 240 -> 235'),

    -- Stukov (Commander = 130)
    (130, '2025-09-06', 0, '- Crash Landing reduced from 350 -> 300'),
    (130, '2025-09-06', 0, '- Aleksander reduced from 1500 -> 1450'),

    -- Dehaka (Commander = 40)
    (40, '2025-09-06', 0, '- Impaler reduced from 440 -> 430'),
    (40, '2025-09-06', 0, '- Primal Host reduced from 285 -> 275'),
    (40, '2025-09-06', 0, '- Primal Ultralisk reduced from 290 -> 280'),
    (40, '2025-09-06', 0, '- Creeper Host reduced from 510 -> 500'),
    (40, '2025-09-06', 0, '- Primal Hydralisk reduced from 95 -> 90'),
    (40, '2025-09-06', 0, '- Ravasaur reduced from 90 -> 85'),
    (40, '2025-09-06', 0, '- Primal Zergling reduced from 45 -> 40'),
    (40, '2025-09-06', 0, '- Dehaka (Hero) reduced from 400 -> 350'),

    -- Stetmann (Commander = 120)
    (120, '2025-09-06', 0, '- Mecha Battle Carrier Lord reduced from 725 -> 700'),
    (120, '2025-09-06', 0, '- Mecha Lurker increased from 400 -> 415'),
    (120, '2025-09-06', 0, '- Mecha Ultralisk: Stun duration reduced from 2s -> 1.5s');
    ";
            migrationBuilder.Sql(sql1);

            var sql2 = @"
    -- Han & Horner
    UPDATE DsUnits SET Cost = 360 WHERE Commander = 60 and Name = 'Deimos Viking';
    UPDATE DsUnits SET Cost = 325 WHERE Commander = 60 and Name = 'Asteria Wraith';
    UPDATE DsUnits SET Cost = 440 WHERE Commander = 60 and Name = 'Assault Galleon';
    UPDATE DsUnits SET Cost = 120 WHERE Commander = 60 and Name = 'Theia Raven';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 60 and Upgrade = 'Drone Hanger';

    -- Karax
    UPDATE DsUnits SET Cost = 135 WHERE Commander = 70 and Name = 'Energizer';
    UPDATE DsUnits SET Cost = 140 WHERE Commander = 70 and Name = 'Mirage';
    UPDATE DsUnits SET Cost = 85 WHERE Commander = 70 and Name = 'Sentinel';
    UPDATE DsUnits SET Cost = 255 WHERE Commander = 70 and Name = 'Annihilator';

    -- Raynor
    UPDATE DsUnits SET Cost = 190 WHERE Commander = 110 and Name = 'Dusk Wing';
    UPDATE DsUnits SET Cost = 200 WHERE Commander = 110 and Name = 'Viking';
    UPDATE DsUnits SET Cost = 235 WHERE Commander = 110 and Name = 'Siege Tank';
    UPDATE DsUnits SET Cost = 175 WHERE Commander = 110 and Name = 'Banshee';
    UPDATE DsUnits SET Cost = 465 WHERE Commander = 110 and Name = 'Battlecruiser';
    UPDATE DsUnits SET Cost = 1900 WHERE Commander = 110 and Name = 'Hyperion';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 110 and Upgrade = 'Cloaking Field';
    UPDATE DsUpgrades SET Cost = 125 WHERE Commander = 110 and Upgrade = 'Shockwave Missile Battery';
    UPDATE DsUpgrades SET Cost = 100 WHERE Commander = 110 and Upgrade = 'Phobos Weapon System';
    UPDATE DsUpgrades SET Cost = 75 WHERE Commander = 110 and Upgrade = 'Ripwave Missiles';
    UPDATE DsUpgrades SET Cost = 75 WHERE Commander = 110 and Upgrade = 'Advanced Siege Tech';
    UPDATE DsUpgrades SET Cost = 150 WHERE Commander = 110 and Upgrade = 'Advanced Targeting System';

    -- Zagara
    UPDATE DsUnits SET Cost = 340 WHERE Commander = 170 and Name = 'Aberration';
    UPDATE DsUnits SET Cost = 60 WHERE Commander = 170 and Name = 'Scourge';
    UPDATE DsUnits SET Cost = 190 WHERE Commander = 170 and Name = 'Corruptor';
    UPDATE DsUnits SET Cost = 225 WHERE Commander = 170 and Name = 'Zagara';
    UPDATE DsUnits SET Cost = 135 WHERE Commander = 170 and Name = 'Queen';
    UPDATE DsUnits SET Cost = 17 WHERE Commander = 170 and Name = 'Swarmling';

    -- Abathur
    UPDATE DsUnits SET Cost = 110 WHERE Commander = 10 and Name = 'Swarm Queen';
    UPDATE DsUnits SET Cost = 75 WHERE Commander = 10 and Name = 'Vile Roach';
    UPDATE DsUnits SET Cost = 240 WHERE Commander = 10 and Name = 'Ravager';
    UPDATE DsUnits SET Cost = 220 WHERE Commander = 10 and Name = 'Guardian';
    UPDATE DsUnits SET Cost = 625 WHERE Commander = 10 and Name = 'Brutalisk';
    UPDATE DsUnits SET Cost = 800 WHERE Commander = 10 and Name = 'Leviathan';

    -- Swann
    UPDATE DsUnits SET Cost = 235 WHERE Commander = 140 and Name = 'Siege Tank';
    UPDATE DsUpgrades SET Cost = 150 WHERE Commander = 140 and Upgrade = 'Drakken Laser Drill';

    -- Stukov
    UPDATE DsUpgrades SET Cost = 300 WHERE Commander = 130 and Upgrade = 'Crash Landing';
    UPDATE DsUnits SET Cost = 1450 WHERE Commander = 130 and Name = 'Aleksander';

    -- Dehaka
    UPDATE DsUnits SET Cost = 430 WHERE Commander = 40 and Name = 'Impaler';
    UPDATE DsUnits SET Cost = 275 WHERE Commander = 40 and Name = 'Primal Host';
    UPDATE DsUnits SET Cost = 280 WHERE Commander = 40 and Name = 'Primal Ultralisk';
    UPDATE DsUnits SET Cost = 500 WHERE Commander = 40 and Name = 'Creeper Host';
    UPDATE DsUnits SET Cost = 90 WHERE Commander = 40 and Name = 'Primal Hydralisk';
    UPDATE DsUnits SET Cost = 85 WHERE Commander = 40 and Name = 'Ravasaur';
    UPDATE DsUnits SET Cost = 40 WHERE Commander = 40 and Name = 'Primal Zergling';
    UPDATE DsUnits SET Cost = 350 WHERE Commander = 40 and Name = 'Dehaka';

    -- Stetmann
    UPDATE DsUnits SET Cost = 700 WHERE Commander = 120 and Name = 'Mecha Battlecarrier Lord';
    UPDATE DsUnits SET Cost = 415 WHERE Commander = 120 and Name = 'Mecha Lurker';
    ";
            migrationBuilder.Sql(sql2);

            var sql3 = @"UPDATE DsUpgrades SET RequiredTier = 0 WHERE Commander = 170 AND Upgrade = 'Medusa Blades';
        UPDATE DsUpgrades SET RequiredTier = 0 WHERE Commander = 170 AND Upgrade = 'Broodmother';
        UPDATE DsAbilities SET CastRange = 7 WHERE Commander = 60 AND Name = 'Precision Strike';
        UPDATE DsUnits SET Life = 110 WHERE Commander = 120 and Name = 'Mecha Roach';
        UPDATE DsUnits SET Life = 120 WHERE Commander = 120 and Name = 'Mecha Ravager';
        UPDATE DsWeapons SET DamagePerUpgrade = 1 WHERE Name = 'Erudition Missiles';
        UPDATE DsUnits SET UnitType = 9 WHERE Commander = 120 AND Name = 'Mecha Lurker';
        UPDATE DsAbilities SET Description = 'Cloaks the Centurion and lets them dash to the enemy.' WHERE Commander = 160 and Name = 'Shadow Charge';
            ";

            migrationBuilder.Sql(sql3);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

