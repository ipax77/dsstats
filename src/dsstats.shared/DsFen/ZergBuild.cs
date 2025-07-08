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
                { "Queen", new('r') },
                { "Overseer", new('t', true) },
                { "Hydralisk", new('a', false, true, true) },
                { "Lurker", new('a', false, true) },
                { "Mutalisk", new('s', true) },
                { "Corruptor", new('d', true) },
                { "Infestor", new('f') },
                { "SwarmHost", new('g') },
                { "Viper", new('z', true) },
                { "Ultralisk", new('x') },
                { "BroodLord", new('c', true) },
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