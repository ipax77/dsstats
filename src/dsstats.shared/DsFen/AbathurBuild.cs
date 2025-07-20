namespace dsstats.shared.DsFen;

public class AbathurBuild : CmdrBuild
{
    public AbathurBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
            {
                { "VileRoach", new('q') },
                { "SwarmQueen", new('w') },

                { "Mutalisk", new('a', 1, true) },
                { "Ravager", new('s', 2) },
                { "Devourer", new('d', 2, true) },
                { "Overseer", new('f', 2, true) },
                { "SwarmHost", new('g', 2) },

                { "Viper", new('z', 2, true) },
                { "Guardian", new('x', 2, true) },
                { "Brutalisk", new('c', 2) },
                { "Leviathan", new('v', 3, true) },
            };

        AbilityMap = new Dictionary<string, BuildOption>
            {
                { "GlialReconstitution", new('q') },
                { "TunnelingClaws", new('w') },
                { "VileRoachHydriodicBile", new('r') },
                { "VileRoachAdaptivePlating", new('e') },
                { "SwarmQueenBioMechanicalTransfusion", new('t') },

                { "RavagerBloatedBileDucts", new('a') },
                { "RavagerPotentBile", new('s') },
                { "GuardianProlongedDispersion", new('d') },
                { "DevourerCorrosiveSpray", new('f') },
                { "overlordspeed", new('g') },

                { "BroodMutaliskViciousGlave", new('z') },
                { "BroodMutaliskSunderingGlave", new('x') },
                { "SwarmHostPressurizedGlands", new('c') },
                { "ViperVirulentMicrobes", new('v') },
            };

        UpgradeMap = new Dictionary<string, BuildOption>
            {
                { "ZergMeleeWeaponsLevel1", new('a') },
                { "ZergMeleeWeaponsLevel2", new('a') },
                { "ZergMeleeWeaponsLevel3", new('a') },
                { "ZergGroundArmorsLevel1", new('s') },
                { "ZergGroundArmorsLevel2", new('s') },
                { "ZergGroundArmorsLevel3", new('s') },
                { "ZergMissileWeaponsLevel1", new('d') },
                { "ZergMissileWeaponsLevel2", new('d') },
                { "ZergMissileWeaponsLevel3", new('d') },
                { "ZergFlyerWeaponsLevel1", new('f') },
                { "ZergFlyerWeaponsLevel2", new('f') },
                { "ZergFlyerWeaponsLevel3", new('f') },
                { "ZergFlyerArmorsLevel1", new('g') },
                { "ZergFlyerArmorsLevel2", new('g') },
                { "ZergFlyerArmorsLevel3", new('g') },
            };
        CreateActiveUnits();
    }
}