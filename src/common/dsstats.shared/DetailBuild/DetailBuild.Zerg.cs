using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ZergBuild DetectZergBuild(SpawnDto spawnDto)
    {
        const int DominantSharePercent = 60;

        var composition = new ZergComposition();

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Zerg);
            if (TryGetZergProfile(name, out var profile))
            {
                composition.Add(profile, unit.Count);
            }
        }

        if (composition.HasUltralisk)
        {
            return ZergBuild.Ultras;
        }

        if (composition.HasMutalisk)
        {
            return ZergBuild.Mutas;
        }

        if (composition.HasSwarmHost)
        {
            return ZergBuild.SwarmHosts;
        }

        if (composition.RoachCount >= 2
            && composition.QueenCount >= 2
            && composition.HasLurker
            && IsAtLeastShare(composition.RoachWeight + composition.QueenWeight + composition.LurkerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.RoachQueenLurker;
        }

        if (composition.QueenCount >= 2
            && composition.HasLurker
            && IsAtLeastShare(composition.QueenWeight + composition.LurkerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.QueenLurker;
        }

        if (composition.RavagerCount >= 2
            && IsAtLeastShare(composition.RavagerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.Ravagers;
        }

        if (composition.RoachCount >= 2
            && composition.QueenCount >= 2
            && IsAtLeastShare(composition.RoachWeight + composition.QueenWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.RoachQueen;
        }

        if (composition.HydraliskCount >= 2
            && IsAtLeastShare(composition.HydraliskWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.Hydras;
        }

        if (composition.RoachCount >= 2
            && IsAtLeastShare(composition.RoachWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.Roaches;
        }

        if (composition.QueenCount >= 2
            && IsAtLeastShare(composition.QueenWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.Queens;
        }

        if (composition.ZerglingCount >= 2
            && composition.BanelingCount >= 2
            && IsAtLeastShare(composition.ZerglingWeight + composition.BanelingWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ZergBuild.LingBanes;
        }

        return composition.ZerglingCount >= 2
            && IsAtLeastShare(composition.ZerglingWeight, composition.TotalWeight, DominantSharePercent)
            ? ZergBuild.Zerglings
            : ZergBuild.None;
    }

    private static bool TryGetZergProfile(string normalizedName, out ZergUnitProfile profile)
    {
        profile = normalizedName switch
        {
            "Zergling" => new ZergUnitProfile(ZergUnitKind.Zergling, 1),
            "Baneling" => new ZergUnitProfile(ZergUnitKind.Baneling, 1),
            "Queen" => new ZergUnitProfile(ZergUnitKind.Queen, 2),
            "Roach" => new ZergUnitProfile(ZergUnitKind.Roach, 2),
            "Hydralisk" => new ZergUnitProfile(ZergUnitKind.Hydralisk, 2),
            "Ravager" => new ZergUnitProfile(ZergUnitKind.Ravager, 2),
            "Lurker" => new ZergUnitProfile(ZergUnitKind.Lurker, 3),
            "Mutalisk" => new ZergUnitProfile(ZergUnitKind.Mutalisk, 3),
            "Swarm Host" => new ZergUnitProfile(ZergUnitKind.SwarmHost, 3),
            "Ultralisk" => new ZergUnitProfile(ZergUnitKind.Ultralisk, 4),
            _ => default,
        };

        return profile.Kind != ZergUnitKind.Unknown;
    }

    private enum ZergUnitKind
    {
        Unknown,
        Zergling,
        Baneling,
        Queen,
        Roach,
        Hydralisk,
        Ravager,
        Lurker,
        Mutalisk,
        SwarmHost,
        Ultralisk,
    }

    private readonly record struct ZergUnitProfile(ZergUnitKind Kind, int Weight);

    private struct ZergComposition
    {
        public int TotalWeight;
        public int ZerglingWeight;
        public int BanelingWeight;
        public int QueenWeight;
        public int RoachWeight;
        public int HydraliskWeight;
        public int RavagerWeight;
        public int LurkerWeight;
        public int ZerglingCount;
        public int BanelingCount;
        public int QueenCount;
        public int RoachCount;
        public int HydraliskCount;
        public int RavagerCount;
        public bool HasLurker;
        public bool HasMutalisk;
        public bool HasSwarmHost;
        public bool HasUltralisk;

        public void Add(ZergUnitProfile profile, int count)
        {
            var weight = profile.Weight * count;
            TotalWeight += weight;

            switch (profile.Kind)
            {
                case ZergUnitKind.Zergling:
                    ZerglingCount += count;
                    ZerglingWeight += weight;
                    break;
                case ZergUnitKind.Baneling:
                    BanelingCount += count;
                    BanelingWeight += weight;
                    break;
                case ZergUnitKind.Queen:
                    QueenCount += count;
                    QueenWeight += weight;
                    break;
                case ZergUnitKind.Roach:
                    RoachCount += count;
                    RoachWeight += weight;
                    break;
                case ZergUnitKind.Hydralisk:
                    HydraliskCount += count;
                    HydraliskWeight += weight;
                    break;
                case ZergUnitKind.Ravager:
                    RavagerCount += count;
                    RavagerWeight += weight;
                    break;
                case ZergUnitKind.Lurker:
                    HasLurker = true;
                    LurkerWeight += weight;
                    break;
                case ZergUnitKind.Mutalisk:
                    HasMutalisk = true;
                    break;
                case ZergUnitKind.SwarmHost:
                    HasSwarmHost = true;
                    break;
                case ZergUnitKind.Ultralisk:
                    HasUltralisk = true;
                    break;
            }
        }
    }
}
