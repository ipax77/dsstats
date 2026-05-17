using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static TerranBuild DetectTerranBuild(SpawnDto spawnDto)
    {
        var hasBio = false;
        var hasMech = false;
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
                case "Viking":
                case "Thor":
                case "Widow Mine":
                case "Siege Tank":
                case "Hellion":
                case "Hellbat":
                case "Banshee":
                case "Cyclone":
                    hasMech = true;
                    break;
                case "Marine":
                case "Marauder":
                case "Medivac":
                case "Ghost":
                case "Reaper":
                    hasBio = true;
                    break;
            }
        }

        if (hasLibs)
        {
            return TerranBuild.Libs;
        }

        if (hasMech)
        {
            return TerranBuild.Mech;
        }

        return hasBio ? TerranBuild.Bio : TerranBuild.None;
    }
}
