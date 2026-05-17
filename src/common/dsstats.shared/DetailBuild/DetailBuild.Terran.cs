using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static TerranBuild DetectTerranBuild(SpawnDto spawnDto)
    {
        const int BioSupportThreshold = 12;
        const int BansheeThreshold = 3;
        const int RavenVikingRavenThreshold = 2;
        const int RavenVikingVikingThreshold = 4;

        var bioCount = 0;
        var bansheeCount = 0;
        var cycloneCount = 0;
        var hellbatCount = 0;
        var hellionCount = 0;
        var ravenCount = 0;
        var siegeTankCount = 0;
        var thorCount = 0;
        var vikingCount = 0;
        var hasSoftSupportTech = false;
        var hasLibs = false;

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Terran);
            switch (name)
            {
                case "Battlecruiser":
                    return TerranBuild.BC;
                case "Liberator":
                    hasLibs = true;
                    break;
                case "Raven":
                    ravenCount += unit.Count;
                    hasSoftSupportTech = true;
                    break;
                case "Viking":
                    vikingCount += unit.Count;
                    hasSoftSupportTech = true;
                    break;
                case "Widow Mine":
                    hasSoftSupportTech = true;
                    break;
                case "Banshee":
                    bansheeCount += unit.Count;
                    break;
                case "Thor":
                    thorCount += unit.Count;
                    break;
                case "Siege Tank":
                    siegeTankCount += unit.Count;
                    break;
                case "Hellion":
                    hellionCount += unit.Count;
                    break;
                case "Hellbat":
                    hellbatCount += unit.Count;
                    break;
                case "Cyclone":
                    cycloneCount += unit.Count;
                    break;
                case "Marine":
                case "Marauder":
                case "Medivac":
                case "Ghost":
                case "Reaper":
                    bioCount += unit.Count;
                    break;
            }
        }

        if (hasLibs)
        {
            return TerranBuild.Libs;
        }

        if (bansheeCount >= BansheeThreshold)
        {
            return TerranBuild.Banshees;
        }

        if (ravenCount >= RavenVikingRavenThreshold && vikingCount >= RavenVikingVikingThreshold)
        {
            return TerranBuild.RavenViking;
        }

        if (bioCount >= BioSupportThreshold
            && thorCount <= 1
            && hellbatCount == 0
            && siegeTankCount <= 2
            && hellionCount <= 3
            && cycloneCount <= 4)
        {
            return TerranBuild.Bio;
        }

        if (thorCount > 0
            || hellbatCount > 0
            || siegeTankCount > 0
            || hellionCount > 0
            || cycloneCount > 0
            || hasSoftSupportTech
            || bansheeCount > 0)
        {
            return TerranBuild.Mech;
        }

        return bioCount >= 2 ? TerranBuild.Bio : TerranBuild.None;
    }
}
