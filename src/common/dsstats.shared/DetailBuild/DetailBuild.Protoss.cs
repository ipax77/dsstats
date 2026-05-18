using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ProtossBuild DetectProtossBuild(SpawnDto spawnDto)
    {
        const int DominantSharePercent = 60;
        const int TechCompositeSharePercent = 50;

        var composition = new ProtossComposition();

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Protoss);
            if (TryGetProtossProfile(name, out var profile))
            {
                composition.Add(profile, unit.Count);
            }
        }

        if (composition.HasCarrier)
        {
            return ProtossBuild.Carriers;
        }

        if (composition.HasTemplar)
        {
            return ProtossBuild.Templar;
        }

        if (composition.HasAirDisruptorAir
            && composition.HasDisruptor
            && (IsAtLeastShare(composition.AirDisruptorWeight, composition.TotalWeight, TechCompositeSharePercent)
                || composition.DisruptorCount >= 2
                || composition.VoidrayCount >= 2))
        {
            return ProtossBuild.AirDisruptor;
        }

        if (composition.HasArchon && composition.HasImmortal)
        {
            return ProtossBuild.ArchonsImmortals;
        }

        if (composition.HasImmortal)
        {
            return ProtossBuild.Immortals;
        }

        if (composition.HasArchon)
        {
            return ProtossBuild.Archons;
        }

        if (composition.VoidrayCount >= 2
            && (IsAtLeastShare(composition.VoidrayWeight, composition.TotalWeight, TechCompositeSharePercent)
                || IsAtLeastShare(composition.AirWeight, composition.TotalWeight, DominantSharePercent)))
        {
            return ProtossBuild.Voidrays;
        }

        if (composition.AdeptCount >= 2
            && composition.StalkerCount >= 2
            && IsAtLeastShare(composition.AdeptWeight + composition.StalkerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ProtossBuild.AdeptStalker;
        }

        if (composition.ZealotCount >= 2
            && composition.StalkerCount >= 2
            && IsAtLeastShare(composition.ZealotWeight + composition.StalkerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ProtossBuild.ZealotStalker;
        }

        if (composition.StalkerCount >= 2
            && IsAtLeastShare(composition.StalkerWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ProtossBuild.Stalker;
        }

        if (composition.AdeptCount >= 2
            && IsAtLeastShare(composition.AdeptWeight, composition.TotalWeight, DominantSharePercent))
        {
            return ProtossBuild.Adepts;
        }

        return composition.ZealotCount >= 2
            && IsAtLeastShare(composition.ZealotWeight, composition.TotalWeight, DominantSharePercent)
            ? ProtossBuild.Zealots
            : ProtossBuild.None;
    }

    private static bool TryGetProtossProfile(string normalizedName, out ProtossUnitProfile profile)
    {
        profile = normalizedName switch
        {
            "Zealot" => new ProtossUnitProfile(ProtossUnitKind.Zealot, 1),
            "Stalker" => new ProtossUnitProfile(ProtossUnitKind.Stalker, 1),
            "Adept" => new ProtossUnitProfile(ProtossUnitKind.Adept, 1),
            "Sentry" => new ProtossUnitProfile(ProtossUnitKind.Support, 1),
            "Oracle" => new ProtossUnitProfile(ProtossUnitKind.Oracle, 2),
            "Phoenix" => new ProtossUnitProfile(ProtossUnitKind.Air, 2),
            "Void Ray" => new ProtossUnitProfile(ProtossUnitKind.Voidray, 2),
            "Tempest" => new ProtossUnitProfile(ProtossUnitKind.Air, 3),
            "Dark Templar" => new ProtossUnitProfile(ProtossUnitKind.Support, 3),
            "Disruptor" => new ProtossUnitProfile(ProtossUnitKind.Disruptor, 4),
            "Archon" => new ProtossUnitProfile(ProtossUnitKind.Archon, 4),
            "Immortal" => new ProtossUnitProfile(ProtossUnitKind.Immortal, 4),
            "High Templar" => new ProtossUnitProfile(ProtossUnitKind.Templar, 4),
            "Carrier" => new ProtossUnitProfile(ProtossUnitKind.Carrier, 4),
            _ => default,
        };

        return profile.Kind != ProtossUnitKind.Unknown;
    }

    private enum ProtossUnitKind
    {
        Unknown,
        Zealot,
        Stalker,
        Adept,
        Support,
        Oracle,
        Air,
        Voidray,
        Disruptor,
        Archon,
        Immortal,
        Templar,
        Carrier,
    }

    private readonly record struct ProtossUnitProfile(ProtossUnitKind Kind, int Weight);

    private struct ProtossComposition
    {
        public int TotalWeight;
        public int ZealotWeight;
        public int StalkerWeight;
        public int AdeptWeight;
        public int AirWeight;
        public int VoidrayWeight;
        public int AirDisruptorWeight;
        public int ZealotCount;
        public int StalkerCount;
        public int AdeptCount;
        public int OracleCount;
        public int VoidrayCount;
        public int DisruptorCount;
        public bool HasAir;
        public bool HasAirDisruptorAir;
        public bool HasDisruptor;
        public bool HasArchon;
        public bool HasImmortal;
        public bool HasTemplar;
        public bool HasCarrier;

        public void Add(ProtossUnitProfile profile, int count)
        {
            var weight = profile.Weight * count;
            TotalWeight += weight;

            switch (profile.Kind)
            {
                case ProtossUnitKind.Zealot:
                    ZealotCount += count;
                    ZealotWeight += weight;
                    break;
                case ProtossUnitKind.Stalker:
                    StalkerCount += count;
                    StalkerWeight += weight;
                    break;
                case ProtossUnitKind.Adept:
                    AdeptCount += count;
                    AdeptWeight += weight;
                    break;
                case ProtossUnitKind.Oracle:
                    HasAir = true;
                    OracleCount += count;
                    AirWeight += weight;
                    AirDisruptorWeight += weight;
                    HasAirDisruptorAir |= OracleCount >= 2;
                    break;
                case ProtossUnitKind.Air:
                    HasAir = true;
                    HasAirDisruptorAir = true;
                    AirWeight += weight;
                    AirDisruptorWeight += weight;
                    break;
                case ProtossUnitKind.Voidray:
                    HasAir = true;
                    HasAirDisruptorAir = true;
                    VoidrayCount += count;
                    AirWeight += weight;
                    VoidrayWeight += weight;
                    AirDisruptorWeight += weight;
                    break;
                case ProtossUnitKind.Disruptor:
                    HasDisruptor = true;
                    DisruptorCount += count;
                    AirDisruptorWeight += weight;
                    break;
                case ProtossUnitKind.Archon:
                    HasArchon = true;
                    break;
                case ProtossUnitKind.Immortal:
                    HasImmortal = true;
                    break;
                case ProtossUnitKind.Templar:
                    HasTemplar = true;
                    break;
                case ProtossUnitKind.Carrier:
                    HasCarrier = true;
                    break;
            }
        }
    }
}
