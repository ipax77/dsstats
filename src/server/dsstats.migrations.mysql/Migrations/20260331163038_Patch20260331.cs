using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// ABATHUR
//  - Viper Blinding Cloud duration decreased from 5 to 4.5 seconds.
//  - Viper cost increased from 250 to 275.
//  - Brutalisk cost decreased from 625 to 575.
//  - Leviathan cost decreased from 800 to 775.
//  - Ravager biomass cost increased from 4 to 5.
//  - Swarm Queen cost decreased from 110 to 105.

// ALARAK
//  - Vanguard cost decreased from 425 to 410.
//  - Destroyer cost decreased from 260 to 255.
//  - Wrathwalker cost decreased from 375 to 365.
//  - Thermal Lance cooldown decreased from 5 to 4.
//  - Fusion Mortars cost decreased from 125 to 100.

// ARTANIS
//  - Tempest cost increased from 365 to 385.
//  - High Archon cost decreased from 330 to 310.
//  - High Templar cost decreased from 175 to 165.
//  - Dragoon cost decreased from 120 to 115.

// DEHAKA
//  - Attack Level 1 upgrade decreased from 200 to 175; Armor upgrade decreased from 150 to 125.
//  - Attack Level 2 upgrade decreased from 300 to 250; Armor upgrade decreased from 225 to 175.
//  - Attack Level 3 upgrade decreased from 400 to 325; Armor upgrade decreased from 300 to 225.

// FENIX
//  - Kaldalis cost decreased from 400 to 350.
//  - Taldarin cost decreased from 550 to 500.
//  - Colossus cost decreased from 300 to 290.
//  - Disruptor cost decreased from 300 to 290.
//  - Conservator cost decreased from 170 to 160.
//  - Scout cost decreased from 145 to 140.
//  - Legionnaire cost decreased from 145 to 140.
//  - Stasis Field duration decreased from 5 to 4.5 seconds.
//  - Cloaking Field energy cost increased from 15 to 20 per second.
//  - Extended Thermal Lance cost decreased from 150 to 125.
//  - Legionnaire Charge cost decreased from 75 to 50.
//  - Disruptor Cloaking Module cost decreased from 100 to 75.
//  - Disruptor Purification Echo cost decreased from 150 to 125.

// HAN & HORNER
//  - Sovereign Battlecruiser cost increased from 950 to 1000.
//  - Hangar Drone cost decreased from 100 to 75.
//  - Asteria Wraith cost decreased from 325 to 315.
//  - Theia Raven cost decreased from 120 to 110.
// KARAX
//  - Support Carrier cost increased from 450 to 460.
//  - Extended Thermal Lance cost decreased from 100 to 75.
//  - Fire Beam cost decreased from 150 to 125.

// KERRIGAN
//  - Lurker cost decreased from 290 to 280.
//  - Brood Lord cost decreased from 310 to 300.
//  - Hydralisk cost decreased from 85 to 80.
//  - Queen cost decreased from 145 to 140.
//  - Kerrigan turn speed doubled.

// MENGSK
//  - Pride of Augustgrad cost increased from 1450 to 1575.
//  - Pride of Augustgrad Tactical Jump range reduced from 8 to 6.
//  - Emperor’s Shadow cost increased from 525 to 535.
//  - Emperor’s Shadow Pyrokinetic Immolation energy cost increased from 100 to 125.
//  - Dominion Trooper cost decreased from 45 to 40.
//  - Hailstorm Launcher cost decreased from 60 to 55.
//  - B-2 High Cal LMG cost decreased from 25 to 20.
//  - Imperial Witness cost decreased from 275 to 250.
//  - Ultralisk cost decreased from 260 to 250.

// NOVA
//  - Covert Banshee cost decreased from 350 to 340.
//  - Spec Ops Ghost cost decreased from 340 to 330.
//  - Penetrating Blast energy cost increased from 50 to 75.
//  - Blink cooldown increased from 30 to 35.
//  - Defensive Drone cooldown increased from 150 to 180.

// STUKOV
//  - Infested Diamondback cost decreased from 185 to 175.
//  - Infested Banshee cost decreased from 180 to 170.

// SWANN
//  - Wraith cost decreased from 140 to 130.
//  - Thor cost decreased from 400 to 390.
//  - Science Vessel cost decreased from 250 to 240.
//  - Goliath cost decreased from 135 to 130.
//  - Multi-Lock Weapon Systems moved from T3 to T2.
//  - Maelstrom Rounds cost decreased from 150 to 125.
// TYCHUS
//  - Sure Shot Networked Helmet cost increased from 300 to 400.
//  - KD8 Implosion Core cost increased from 150 to 175.
//  - Procyon Serum cost increased from 200 to 300.
//  - Procyon Twin Beam Gauntlet cost increased from 300 to 325.
//  - Umojan Repair Nanites cost increased from 250 to 300.
//  - Umojan Turret Frame cost decreased from 150 to 100.
//  - Moebius M34 Terror Rounds cost decreased from 300 to 275.
//  - Hammer Munitions cost increased from 400 to 425.
//  - Umojan Signal Modulator cost decreased from 100 to 75.
//  - Moebius Aggression Blend cost decreased from 100 to 50.
//  - Procyon Shade Suit cost decreased from 200 to 175.
//  - Enhanced Hostilities Kit cost decreased from 100 to 50.
//  - N3 Networking cost decreased from 150 to 100.

// VORAZUN
//  - Shadow Guard cost increased from 200 to 225.
//  - Dark Pylon cooldown increased from 60 to 75 seconds.

// ZAGARA
//  - Hunter Killer cost decreased from 145 to 140.
//  - Corruptor cost decreased from 190 to 180.
//  - Scourge cost decreased from 60 to 55.
//  - Aberration cost decreased from 340 to 335.
//  - Zagara cost decreased from 225 to 200.
//  - Mass Frenzy cooldown decreased from 180 to 150.

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class Patch20260331 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── DsUnits ────────────────────────────────────────────────────────────

            // ABATHUR (10)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 275 WHERE Name = 'Viper'       AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 575 WHERE Name = 'Brutalisk'   AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 775 WHERE Name = 'Leviathan'   AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 105 WHERE Name = 'Swarm Queen' AND Commander = 10;");

            // ALARAK (20)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 410 WHERE Name = 'Vanguard'    AND Commander = 20;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 255 WHERE Name = 'Destroyer'   AND Commander = 20;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 365 WHERE Name = 'Wrathwalker' AND Commander = 20;");

            // ARTANIS (30)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 385 WHERE Name = 'Purifier Tempest' AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 310 WHERE Name = 'High Archon'      AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 165 WHERE Name = 'High Templar'     AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 115 WHERE Name = 'Dragoon'          AND Commander = 30;");

            // FENIX (50)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 350 WHERE Name = 'Kaldalis'   AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 500 WHERE Name = 'Taldarin'   AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 290 WHERE Name = 'Colossus'   AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 290 WHERE Name = 'Disruptor'  AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 160 WHERE Name = 'Conservator' AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Scout'      AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Legionnaire' AND Commander = 50;");

            // HAN & HORNER (60)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 1000 WHERE Name = 'Sovereign Battlecruiser' AND Commander = 60;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 315  WHERE Name = 'Asteria Wraith'          AND Commander = 60;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 110  WHERE Name = 'Theia Raven'             AND Commander = 60;");

            // KARAX (70)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 460 WHERE Name = 'Support Carrier' AND Commander = 70;");

            // KERRIGAN (80)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 280 WHERE Name = 'Lurker'    AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 300 WHERE Name = 'Brood Lord' AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 80  WHERE Name = 'Hydralisk' AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Queen'     AND Commander = 80;");

            // MENGSK (90)
            // Pride of Augustgrad: DB was 1400 (patch notes say from 1450)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 1575 WHERE Name = 'Pride of Augustgrad'        AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 535  WHERE Name = 'Emperor''s Shadow'          AND Commander = 90;");
            // Trooper variants: DB stores combined costs; base 45→40, LMG component 25→20, Hailstorm 60→55
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 40   WHERE Name = 'Dominion Trooper'           AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 60   WHERE Name = 'LMG Dominion Trooper'       AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 95   WHERE Name = 'Hailstorm Dominion Trooper' AND Commander = 90;");
            // Flamethrower component unchanged; base drop of 5 reflected in combined cost
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 95   WHERE Name = 'Flamethrower Dominion Trooper' AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 250  WHERE Name = 'Imperial Witness'           AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 250  WHERE Name = 'Ultralisk'                  AND Commander = 90;");

            // NOVA (100)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 340 WHERE Name = 'Covert Banshee' AND Commander = 100;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 330 WHERE Name = 'Spec Ops Ghost' AND Commander = 100;");

            // STUKOV (130)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 175 WHERE Name = 'Infested Diamondback' AND Commander = 130;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 170 WHERE Name = 'Infested Banshee'     AND Commander = 130;");

            // SWANN (140)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 130 WHERE Name = 'Wraith'         AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 390 WHERE Name = 'Thor'           AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 240 WHERE Name = 'Science Vessel' AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 130 WHERE Name = 'Goliath'        AND Commander = 140;");

            // VORAZUN (160)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 225 WHERE Name = 'Shadow Guard' AND Commander = 160;");

            // ZAGARA (170)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Hunter Killer' AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 180 WHERE Name = 'Corruptor'     AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 55  WHERE Name = 'Scourge'       AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 335 WHERE Name = 'Aberration'    AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 200 WHERE Name = 'Zagara'        AND Commander = 170;");

            // ── DsUpgrades ─────────────────────────────────────────────────────────

            // ALARAK (20)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Fusion Mortars' AND Commander = 20;");

            // FENIX (50)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 125 WHERE Upgrade = 'Extended Thermal Lance' AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 50  WHERE Upgrade = 'Charge'                 AND Commander = 50;"); // Legionnaire Charge
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 75  WHERE Upgrade = 'Cloaking Module'        AND Commander = 50;"); // Disruptor Cloaking Module
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 125 WHERE Upgrade = 'Purification Echo'      AND Commander = 50;"); // Disruptor Purification Echo

            // KARAX (70)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 75  WHERE Upgrade = 'Extended Thermal Lance' AND Commander = 70;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 125 WHERE Upgrade = 'Fire Beam'              AND Commander = 70;");

            // SWANN (140)
            migrationBuilder.Sql("UPDATE DsUpgrades SET RequiredTier = 2 WHERE Upgrade = 'Multi-Lock Weapons System' AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 125       WHERE Upgrade = 'Maelstrom Rounds'          AND Commander = 140;");

            // TYCHUS (150)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 400 WHERE Upgrade = 'SureShot Networked Helmet'      AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 175 WHERE Upgrade = 'KD9a Implosion Core'            AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 300 WHERE Upgrade = 'Procyon Serum'                  AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 325 WHERE Upgrade = 'Procyon Twin Heal Beam Gauntlet' AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 300 WHERE Upgrade = 'Umojan Repair Nanites'          AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Umojan Turret Frame'            AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 275 WHERE Upgrade = 'Moebius M34 Terror Rounds'      AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 425 WHERE Upgrade = 'Hammer Munitions'               AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 75  WHERE Upgrade = 'Umojan Signal Modulator'        AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 50  WHERE Upgrade = 'Moebius Aggression Blend'       AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 175 WHERE Upgrade = 'Procyon Shade Suit'             AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 50  WHERE Upgrade = 'Enhanced Hostilities Kit'       AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'N3 Networking'                  AND Commander = 150;");

            // ── DsAbilities ────────────────────────────────────────────────────────

            // ALARAK (20): Thermal Lance cooldown 5 → 4
            migrationBuilder.Sql("UPDATE DsAbilities SET Cooldown = 4 WHERE Name = 'Thermal Lance' AND Commander = 20;");

            // FENIX (50): Cloaking Field energy cost → 20 per second (DB was 5)
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 20 WHERE Name = 'Cloaking Field' AND Commander = 50;");

            // NOVA (100): Penetrating Blast energy cost 50 → 75
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 75 WHERE Name = 'Penetrating Blast' AND Commander = 100;");

            // MENGSK (90): Pyrokinetic Immolation energy cost → 125 (DB was 75)
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 125 WHERE Name = 'Pyrokinetic Immolation' AND Commander = 90;");

            // ABATHUR (10): Disabling Cloud duration 5 → 4.5 seconds (reflected in description)
            migrationBuilder.Sql("UPDATE DsAbilities SET Description = REPLACE(Description, 'Lasts for 5 seconds.', 'Lasts for 4.5 seconds.') WHERE Name = 'Disabling Cloud' AND Commander = 10;");

            // FENIX (50): Stasis Field duration 5 → 4.5 seconds (reflected in description)
            migrationBuilder.Sql("UPDATE DsAbilities SET Description = REPLACE(Description, 'stasis for 5 seconds.', 'stasis for 4.5 seconds.') WHERE Name = 'Stasis Field' AND Commander = 50;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── DsUnits ────────────────────────────────────────────────────────────

            // ABATHUR (10)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 250 WHERE Name = 'Viper'       AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 625 WHERE Name = 'Brutalisk'   AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 800 WHERE Name = 'Leviathan'   AND Commander = 10;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 110 WHERE Name = 'Swarm Queen' AND Commander = 10;");

            // ALARAK (20)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 425 WHERE Name = 'Vanguard'    AND Commander = 20;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 260 WHERE Name = 'Destroyer'   AND Commander = 20;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 375 WHERE Name = 'Wrathwalker' AND Commander = 20;");

            // ARTANIS (30)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 365 WHERE Name = 'Purifier Tempest' AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 330 WHERE Name = 'High Archon'      AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 175 WHERE Name = 'High Templar'     AND Commander = 30;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 120 WHERE Name = 'Dragoon'          AND Commander = 30;");

            // FENIX (50)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 395 WHERE Name = 'Kaldalis'   AND Commander = 50;"); // was 395 in DB
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 550 WHERE Name = 'Taldarin'   AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 300 WHERE Name = 'Colossus'   AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 300 WHERE Name = 'Disruptor'  AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 170 WHERE Name = 'Conservator' AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 155 WHERE Name = 'Scout'      AND Commander = 50;"); // was 155 in DB
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Legionnaire' AND Commander = 50;");

            // HAN & HORNER (60)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 925 WHERE Name = 'Sovereign Battlecruiser' AND Commander = 60;"); // was 925 in DB
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 325 WHERE Name = 'Asteria Wraith'          AND Commander = 60;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 120 WHERE Name = 'Theia Raven'             AND Commander = 60;");

            // KARAX (70)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 450 WHERE Name = 'Support Carrier' AND Commander = 70;");

            // KERRIGAN (80)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 290 WHERE Name = 'Lurker'    AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 310 WHERE Name = 'Brood Lord' AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 85  WHERE Name = 'Hydralisk' AND Commander = 80;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 145 WHERE Name = 'Queen'     AND Commander = 80;");

            // MENGSK (90)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 1400 WHERE Name = 'Pride of Augustgrad'        AND Commander = 90;"); // was 1400 in DB
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 525  WHERE Name = 'Emperor''s Shadow'          AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 45   WHERE Name = 'Dominion Trooper'           AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 70   WHERE Name = 'LMG Dominion Trooper'       AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 105  WHERE Name = 'Hailstorm Dominion Trooper' AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 100  WHERE Name = 'Flamethrower Dominion Trooper' AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 275  WHERE Name = 'Imperial Witness'           AND Commander = 90;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 260  WHERE Name = 'Ultralisk'                  AND Commander = 90;");

            // NOVA (100)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 350 WHERE Name = 'Covert Banshee' AND Commander = 100;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 340 WHERE Name = 'Spec Ops Ghost' AND Commander = 100;");

            // STUKOV (130)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 185 WHERE Name = 'Infested Diamondback' AND Commander = 130;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 180 WHERE Name = 'Infested Banshee'     AND Commander = 130;");

            // SWANN (140)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 140 WHERE Name = 'Wraith'         AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 400 WHERE Name = 'Thor'           AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 250 WHERE Name = 'Science Vessel' AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 135 WHERE Name = 'Goliath'        AND Commander = 140;");

            // VORAZUN (160)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 200 WHERE Name = 'Shadow Guard' AND Commander = 160;");

            // ZAGARA (170)
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 145 WHERE Name = 'Hunter Killer' AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 190 WHERE Name = 'Corruptor'     AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 60  WHERE Name = 'Scourge'       AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 340 WHERE Name = 'Aberration'    AND Commander = 170;");
            migrationBuilder.Sql("UPDATE DsUnits SET Cost = 225 WHERE Name = 'Zagara'        AND Commander = 170;");

            // ── DsUpgrades ─────────────────────────────────────────────────────────

            // ALARAK (20)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 125 WHERE Upgrade = 'Fusion Mortars' AND Commander = 20;");

            // FENIX (50)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'Extended Thermal Lance' AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 75  WHERE Upgrade = 'Charge'                 AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Cloaking Module'        AND Commander = 50;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'Purification Echo'      AND Commander = 50;");

            // KARAX (70)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Extended Thermal Lance' AND Commander = 70;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'Fire Beam'              AND Commander = 70;");

            // SWANN (140)
            migrationBuilder.Sql("UPDATE DsUpgrades SET RequiredTier = 3 WHERE Upgrade = 'Multi-Lock Weapons System' AND Commander = 140;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150       WHERE Upgrade = 'Maelstrom Rounds'          AND Commander = 140;");

            // TYCHUS (150)
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 300 WHERE Upgrade = 'SureShot Networked Helmet'      AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'KD9a Implosion Core'            AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 200 WHERE Upgrade = 'Procyon Serum'                  AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 300 WHERE Upgrade = 'Procyon Twin Heal Beam Gauntlet' AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 250 WHERE Upgrade = 'Umojan Repair Nanites'          AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'Umojan Turret Frame'            AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 300 WHERE Upgrade = 'Moebius M34 Terror Rounds'      AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 400 WHERE Upgrade = 'Hammer Munitions'               AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Umojan Signal Modulator'        AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Moebius Aggression Blend'       AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 200 WHERE Upgrade = 'Procyon Shade Suit'             AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 100 WHERE Upgrade = 'Enhanced Hostilities Kit'       AND Commander = 150;");
            migrationBuilder.Sql("UPDATE DsUpgrades SET Cost = 150 WHERE Upgrade = 'N3 Networking'                  AND Commander = 150;");

            // ── DsAbilities ────────────────────────────────────────────────────────

            // ALARAK (20)
            migrationBuilder.Sql("UPDATE DsAbilities SET Cooldown = 5 WHERE Name = 'Thermal Lance' AND Commander = 20;");

            // FENIX (50)
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 5  WHERE Name = 'Cloaking Field'          AND Commander = 50;");

            // NOVA (100)
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 50 WHERE Name = 'Penetrating Blast'        AND Commander = 100;");

            // MENGSK (90)
            migrationBuilder.Sql("UPDATE DsAbilities SET EnergyCost = 75 WHERE Name = 'Pyrokinetic Immolation'   AND Commander = 90;");

            // ABATHUR (10): Disabling Cloud description revert
            migrationBuilder.Sql("UPDATE DsAbilities SET Description = REPLACE(Description, 'Lasts for 4.5 seconds.', 'Lasts for 5 seconds.') WHERE Name = 'Disabling Cloud' AND Commander = 10;");

            // FENIX (50): Stasis Field description revert
            migrationBuilder.Sql("UPDATE DsAbilities SET Description = REPLACE(Description, 'stasis for 4.5 seconds.', 'stasis for 5 seconds.') WHERE Name = 'Stasis Field' AND Commander = 50;");
        }
    }
}
