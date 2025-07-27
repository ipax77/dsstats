namespace dsstats.shared.DsFen;

public class AlarakBuild : CmdrBuild
{
    public AlarakBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
            {
                { "Supplicant", new('q') },
                { "Slayer", new('w', 2) },
                { "Havoc", new('e', 1) },
                { "Alarak", new('r', 2) },

                { "Vanguard", new('a', 2) },
                { "Ascendant", new('s') },
                { "Destroyer", new('d', 2, true) },
                { "WarPrism", new('f', 2, true) },

                { "Wrathwalker", new('z', 2) },
                { "MothershipTaldarim", new('x', 3, true) },
            };

        AbilityMap = new Dictionary<string, BuildOption>
            {
                { "SupplicantSoulAugmentation", new('q') },
                { "SupplicantBloodShields", new('w') },
                { "SlayerPhasingArmor", new('e') },
                { "HavocCloakingModule", new('r') },
                { "HavocDetectWeakness", new('t') },

                { "HavocBloodshardResonance", new('a') },
                { "VanguardFusionMortars", new('s') },
                { "VanguardMatterDispersion", new('d') },
                { "AscendantMindBlast", new('f') },
                { "AscendantPowerOverwhelming", new('g') },
                
                { "AscendantChaoticAttunement", new('z') },
                { "WrathwalkerAerialTracking", new('x') },
                { "WrathwalkerPowerCycling", new('c') },
                { "SupplicantStarlightAirAttack", new('v') },

                { "AlarakImposingPresence", new('r', IsAbility: true) },
                { "AlarakTelekinesis", new('g', IsAbility: true) },
            };

        UpgradeMap = new Dictionary<string, BuildOption>
            {
                { "ProtossGroundWeaponsLevel1", new('a') },
                { "ProtossGroundWeaponsLevel2", new('a') },
                { "ProtossGroundWeaponsLevel3", new('a') },
                { "ProtossGroundArmorsLevel1", new('s') },
                { "ProtossGroundArmorsLevel2", new('s') },
                { "ProtossGroundArmorsLevel3", new('s') },
                { "ProtossShieldsLevel1", new('d') },
                { "ProtossShieldsLevel2", new('d') },
                { "ProtossShieldsLevel3", new('d') },
                { "ProtossAirWeaponsLevel1", new('f') },
                { "ProtossAirWeaponsLevel2", new('f') },
                { "ProtossAirWeaponsLevel3", new('f') },
                { "ProtossAirArmorsLevel1", new('g') },
                { "ProtossAirArmorsLevel2", new('g') },
                { "ProtossAirArmorsLevel3", new('g') },
            };
        CreateActiveUnits();
    }
}