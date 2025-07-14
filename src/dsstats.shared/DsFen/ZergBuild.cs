namespace dsstats.shared.DsFen;

public class ZergBuild : CmdrBuild
{
    public ZergBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
            {
                { "Zergling", new('q') },
                { "Baneling", new('w') },
                { "Roach", new('e') },
                { "Queen", new('r', 2) },
                { "Overseer", new('t', 2, true) },
                { "Hydralisk", new('a', 1, false, true, true) },
                { "Lurker", new('a', 2, false, true) },
                { "Mutalisk", new('s', 1, true) },
                { "Corruptor", new('d', 2, true) },
                { "Infestor", new('f', 2) },
                { "SwarmHost", new('g', 2) },
                { "Viper", new('z', 2, true) },
                { "Ultralisk", new('x', 2) },
                { "BroodLord", new('c', 2, true) },
            };

        AbilityMap = new Dictionary<string, BuildOption>
            {
                { "zerglingmovementspeed", new('q') },
                { "zerglingattackspeed", new('w') },
                { "CentrifugalHooks", new('e') },
                { "GlialReconstitution", new('r') },
                { "TunnelingClaws", new('t') },
                { "EvolveGroovedSpines", new('a') },
                { "LurkerRange", new('s') },
                { "DiggingClaws", new('d') },
                { "NeuralParasite", new('g') },
                { "ChitinousPlating", new('z') },
                { "AnabolicSynthesis", new('x') },
                { "MuscularAugments", new('c') },
                { "overlordspeed", new('v') },
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