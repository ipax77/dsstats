using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ZergBuild DetectZergBuild(SpawnDto spawnDto)
    {
        var hasZergling = false;
        var hasBaneling = false;
        var hasQueen = false;
        var hasRoach = false;
        var hasLurker = false;
        var hasMutalisk = false;
        var hasHydralisk = false;
        var hasUltralisk = false;

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Zerg);
            switch (name)
            {
                case "Ultralisk":
                    hasUltralisk = true;
                    break;
                case "Mutalisk":
                    hasMutalisk = true;
                    break;
                case "Hydralisk":
                    hasHydralisk = true;
                    break;
                case "Zergling":
                    hasZergling = true;
                    break;
                case "Baneling":
                    hasBaneling = true;
                    break;
                case "Queen":
                    hasQueen = true;
                    break;
                case "Roach":
                    hasRoach = true;
                    break;
                case "Lurker":
                    hasLurker = true;
                    break;
            }
        }

        if (hasUltralisk)
        {
            return ZergBuild.Ultras;
        }

        if (hasMutalisk)
        {
            return ZergBuild.Mutas;
        }

        if (hasRoach && hasQueen && hasLurker)
        {
            return ZergBuild.RoachQueenLurker;
        }

        if (hasQueen && hasLurker)
        {
            return ZergBuild.QueenLurker;
        }

        if (hasRoach && hasQueen)
        {
            return ZergBuild.RoachQueen;
        }

        if (hasHydralisk)
        {
            return ZergBuild.Hydras;
        }

        if (hasRoach)
        {
            return ZergBuild.Roaches;
        }

        if (hasQueen)
        {
            return ZergBuild.Queens;
        }

        if (hasZergling && hasBaneling)
        {
            return ZergBuild.LingBanes;
        }

        return hasZergling ? ZergBuild.Zerglings : ZergBuild.None;
    }
}
