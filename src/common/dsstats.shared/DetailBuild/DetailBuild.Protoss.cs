using dsstats.shared.Units;

namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static ProtossBuild DetectProtossBuild(SpawnDto spawnDto)
    {
        var zealotCount = 0;
        var stalkerCount = 0;
        var adeptCount = 0;
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

        if (adeptCount > stalkerCount && stalkerCount > 0)
        {
            return ProtossBuild.AdeptStalker;
        }

        if (zealotCount > stalkerCount && stalkerCount > 0)
        {
            return ProtossBuild.ZealotStalker;
        }

        if (stalkerCount > 0)
        {
            return ProtossBuild.Stalker;
        }

        return zealotCount > 0 ? ProtossBuild.Zealots : ProtossBuild.None;
    }
}
