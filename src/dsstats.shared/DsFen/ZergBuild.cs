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
                { "MetabolikBoost", new('q') },
                { "AdrenalGlands", new('w') },
                { "CentrifugalHooks", new('e') },
                { "GlialReconstitution", new('r') },
                { "TunnelingClaws", new('t') },
                { "GroovedSpines", new('a') },
                { "SeismicSpines", new('s') },
                { "AdaptiveTalons", new('d') },
                { "NeuralParasite", new('g') },
                { "ChitinousPlating", new('z') },
                { "AnabolicSynthesis", new('x') },
                { "MuscularAugments", new('c') },
                { "PneumatizedCarapace", new('v') },
            };

        UpgradeMap = new Dictionary<string, BuildOption>
            {
                { "MeleeAttacksLevel1", new('a') },
                { "MeleeAttacksLevel2", new('a') },
                { "MeleeAttacksLevel3", new('a') },
                { "GroundCarapaceLevel1", new('s') },
                { "GroundCarapaceLevel2", new('s') },
                { "GroundCarapaceLevel3", new('s') },
                { "MissileAttacksLevel1", new('d') },
                { "MissileAttacksLevel2", new('d') },
                { "MissileAttacksLevel3", new('d') },
                { "FlyerAttacksLevel1", new('f') },
                { "FlyerAttacksLevel2", new('f') },
                { "FlyerAttacksLevel3", new('f') },
                { "FlyerCarapaceLevel1", new('g') },
                { "FlyerCarapaceLevel2", new('g') },
                { "FlyerCarapaceLevel3", new('g') },
            };
        CreateActiveUnits();
    }
}