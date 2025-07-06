namespace dsstats.builder;

public class ZergBuild : CmdrBuild
{
    public ZergBuild()
    {
        UnitMap = new Dictionary<string, char>
            {
                { "Zergling", 'q' },
                { "Baneling", 'w' },
                { "Roach", 'e' },
                { "Queen", 'r' },
                { "Overseer", 't' },
                { "Hydralisk", 'a' },
                { "Mutalisk", 's' },
                { "Corruptor", 'd' },
                { "Infestor", 'f' },
                { "Swarmhost", 'g' },
                { "Viper", 'z' },
                { "Ultralisk", 'x' },
                { "Broodloard", 'c' },
            };

        AbilityMap = new Dictionary<string, char>
            {
                { "MetabolikBoost", 'q' },
                { "AdrenalGlands", 'w' },
                { "CentrifugalHooks", 'e' },
                { "GlialReconstitution", 'r' },
                { "TunnelingClaws", 't' },
                { "GroovedSpines", 'a' },
                { "SeismicSpines", 's' },
                { "AdaptiveTalons", 'd' },
                { "NeuralParasite", 'g' },
                { "ChitinousPlating", 'z' },
                { "AnabolicSynthesis", 'x' },
                { "MuscularAugments", 'c' },
                { "PneumatizedCarapace", 'v' },
            };

        UpgradeMap = new Dictionary<string, char>
            {
                { "MeleeAttacksLevel1", 'a' },
                { "MeleeAttacksLevel2", 'a' },
                { "MeleeAttacksLevel3", 'a' },
                { "GroundCarapaceLevel1", 's' },
                { "GroundCarapaceLevel2", 's' },
                { "GroundCarapaceLevel3", 's' },
                { "MissileAttacksLevel1", 'd' },
                { "MissileAttacksLevel2", 'd' },
                { "MissileAttacksLevel3", 'd' },
                { "FlyerAttacksLevel1", 'f' },
                { "FlyerAttacksLevel2", 'f' },
                { "FlyerAttacksLevel3", 'f' },
                { "FlyerCarapaceLevel1", 'g' },
                { "FlyerCarapaceLevel2", 'g' },
                { "FlyerCarapaceLevel3", 'g' },
            };
    }
}