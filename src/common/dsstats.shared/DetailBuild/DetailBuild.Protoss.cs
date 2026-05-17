using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ProtossBuild DetectProtossBuild(SpawnDto spawnDto)
    {
        var zealotCount = 0;
        var stalkerCount = 0;
        var adeptCount = 0;
        var voidrayCount = 0;
        var hasAir = false;
        var hasDisruptor = false;
        var hasArchon = false;
        var hasImmortal = false;

        foreach (var unit in spawnDto.Units)
        {
            if (unit.Count <= 0)
            {
                continue;
            }

            var name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Protoss);
            switch (name)
            {
                case "Carrier":
                    return ProtossBuild.Carriers;
                case "High Templar":
                    return ProtossBuild.Templar;
                case "Oracle":
                case "Phoenix":
                case "Tempest":
                    hasAir = true;
                    break;
                case "Void Ray":
                    hasAir = true;
                    voidrayCount += unit.Count;
                    break;
                case "Disruptor":
                    hasDisruptor = true;
                    break;
                case "Zealot":
                    zealotCount += unit.Count;
                    break;
                case "Stalker":
                    stalkerCount += unit.Count;
                    break;
                case "Adept":
                    adeptCount += unit.Count;
                    break;
                case "Archon":
                    hasArchon = true;
                    break;
                case "Immortal":
                    hasImmortal = true;
                    break;
            }
        }

        if (hasAir && hasDisruptor)
        {
            return ProtossBuild.AirDisruptor;
        }

        if (hasArchon && hasImmortal)
        {
            return ProtossBuild.ArchonsImmortals;
        }

        if (hasImmortal)
        {
            return ProtossBuild.Immortals;
        }

        if (hasArchon)
        {
            return ProtossBuild.Archons;
        }

        if (voidrayCount >= 2)
        {
            return ProtossBuild.Voidrays;
        }

        if (adeptCount >= 2 && stalkerCount >= 2)
        {
            return ProtossBuild.AdeptStalker;
        }

        if (zealotCount >= 2 && stalkerCount >= 2)
        {
            return ProtossBuild.ZealotStalker;
        }

        if (stalkerCount >= 2)
        {
            return ProtossBuild.Stalker;
        }

        if (adeptCount >= 2)
        {
            return ProtossBuild.Adepts;
        }

        return zealotCount >= 2 ? ProtossBuild.Zealots : ProtossBuild.None;
    }
}
