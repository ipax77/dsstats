using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static TerranBuild DetectTerranBuild(SpawnDto spawnDto)
    {
        const int BansheeThreshold = 3;
        const int RavenVikingRavenThreshold = 2;
        const int RavenVikingVikingThreshold = 4;
        const int DominantSharePercent = 60;

        var composition = new TerranComposition();

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Terran);
            if (TryGetTerranProfile(name, out var profile))
            {
                composition.Add(profile, unit.Count);
            }
        }

        if (composition.HasBattlecruiser)
        {
            return TerranBuild.BC;
        }

        if (composition.HasLiberator)
        {
            if (composition.BioCount >= 2
                && IsAtLeastShare(composition.BioWeight, composition.TotalWeight, DominantSharePercent))
            {
                return TerranBuild.LibBio;
            }

            return TerranBuild.Libs;
        }

        if (composition.BansheeCount >= BansheeThreshold
            && (IsAtLeastShare(composition.BansheeWeight, composition.TotalWeight, DominantSharePercent)
                || composition.BansheeWeight > composition.BioWeight))
        {
            return TerranBuild.Banshees;
        }

        if (composition.RavenCount >= RavenVikingRavenThreshold
            && composition.VikingCount >= RavenVikingVikingThreshold
            && IsAtLeastShare(composition.RavenVikingWeight, composition.TotalWeight, DominantSharePercent))
        {
            return TerranBuild.RavenViking;
        }

        if (composition.BioCount >= 2
            && IsAtLeastShare(composition.BioWeight, composition.TotalWeight, DominantSharePercent))
        {
            return TerranBuild.Bio;
        }

        if (IsAtLeastShare(composition.MechWeight, composition.TotalWeight, DominantSharePercent))
        {
            return TerranBuild.Mech;
        }

        return composition.BioCount >= 2 ? TerranBuild.Bio : TerranBuild.None;
    }

    private static bool IsAtLeastShare(int value, int total, int percent)
    {
        return total > 0 && value * 100 >= total * percent;
    }

    private static bool TryGetTerranProfile(string normalizedName, out TerranUnitProfile profile)
    {
        profile = normalizedName switch
        {
            "Marine" => new TerranUnitProfile(TerranUnitKind.Bio, 1),
            "Marauder" => new TerranUnitProfile(TerranUnitKind.Bio, 1),
            "Reaper" => new TerranUnitProfile(TerranUnitKind.Bio, 1),
            "Medivac" => new TerranUnitProfile(TerranUnitKind.Bio, 1),
            "Ghost" => new TerranUnitProfile(TerranUnitKind.Bio, 1),
            "Raven" => new TerranUnitProfile(TerranUnitKind.Raven, 2),
            "Viking" => new TerranUnitProfile(TerranUnitKind.Viking, 2),
            "Widow Mine" => new TerranUnitProfile(TerranUnitKind.Mech, 2),
            "Siege Tank" => new TerranUnitProfile(TerranUnitKind.Mech, 2),
            "Hellion" => new TerranUnitProfile(TerranUnitKind.Mech, 2),
            "Hellbat" => new TerranUnitProfile(TerranUnitKind.Mech, 2),
            "Cyclone" => new TerranUnitProfile(TerranUnitKind.Mech, 2),
            "Banshee" => new TerranUnitProfile(TerranUnitKind.Banshee, 2),
            "Liberator" => new TerranUnitProfile(TerranUnitKind.Liberator, 2),
            "Thor" => new TerranUnitProfile(TerranUnitKind.Mech, 4),
            "Battlecruiser" => new TerranUnitProfile(TerranUnitKind.Battlecruiser, 4),
            _ => default,
        };

        return profile.Kind != TerranUnitKind.Unknown;
    }

    private enum TerranUnitKind
    {
        Unknown,
        Bio,
        Mech,
        Banshee,
        Raven,
        Viking,
        Liberator,
        Battlecruiser,
    }

    private readonly record struct TerranUnitProfile(TerranUnitKind Kind, int Weight);

    private struct TerranComposition
    {
        public int TotalWeight;
        public int BioWeight;
        public int MechWeight;
        public int BansheeWeight;
        public int RavenVikingWeight;
        public int BioCount;
        public int BansheeCount;
        public int RavenCount;
        public int VikingCount;
        public bool HasLiberator;
        public bool HasBattlecruiser;

        public void Add(TerranUnitProfile profile, int count)
        {
            var weight = profile.Weight * count;
            TotalWeight += weight;

            switch (profile.Kind)
            {
                case TerranUnitKind.Bio:
                    BioCount += count;
                    BioWeight += weight;
                    break;
                case TerranUnitKind.Banshee:
                    BansheeCount += count;
                    BansheeWeight += weight;
                    MechWeight += weight;
                    break;
                case TerranUnitKind.Raven:
                    RavenCount += count;
                    RavenVikingWeight += weight;
                    MechWeight += weight;
                    break;
                case TerranUnitKind.Viking:
                    VikingCount += count;
                    RavenVikingWeight += weight;
                    MechWeight += weight;
                    break;
                case TerranUnitKind.Liberator:
                    HasLiberator = true;
                    MechWeight += weight;
                    break;
                case TerranUnitKind.Battlecruiser:
                    HasBattlecruiser = true;
                    MechWeight += weight;
                    break;
                case TerranUnitKind.Mech:
                    MechWeight += weight;
                    break;
            }
        }
    }
}
