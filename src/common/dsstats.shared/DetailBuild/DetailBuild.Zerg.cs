using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ZergBuild DetectZergBuild(SpawnDto spawnDto)
    {
        var zerglingCount = 0;
        var banelingCount = 0;
        var queenCount = 0;
        var roachCount = 0;
        var hasLurker = false;
        var hasMutalisk = false;
        var hydraliskCount = 0;
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
                    hydraliskCount += unit.Count;
                    break;
                case "Zergling":
                    zerglingCount += unit.Count;
                    break;
                case "Baneling":
                    banelingCount += unit.Count;
                    break;
                case "Queen":
                    queenCount += unit.Count;
                    break;
                case "Roach":
                    roachCount += unit.Count;
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

        if (roachCount >= 2 && queenCount >= 2 && hasLurker)
        {
            return ZergBuild.RoachQueenLurker;
        }

        if (queenCount >= 2 && hasLurker)
        {
            return ZergBuild.QueenLurker;
        }

        if (roachCount >= 2 && queenCount >= 2)
        {
            return ZergBuild.RoachQueen;
        }

        if (hydraliskCount >= 2)
        {
            return ZergBuild.Hydras;
        }

        if (roachCount >= 2)
        {
            return ZergBuild.Roaches;
        }

        if (queenCount >= 2)
        {
            return ZergBuild.Queens;
        }

        if (zerglingCount >= 2 && banelingCount >= 2)
        {
            return ZergBuild.LingBanes;
        }

        return zerglingCount >= 2 ? ZergBuild.Zerglings : ZergBuild.None;
    }
}
