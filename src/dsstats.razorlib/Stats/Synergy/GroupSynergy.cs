using dsstats.shared;

namespace dsstats.razorlib.Stats.Synergy;

internal static class GroupSynergy
{
    public static GroupSynergyResponse GetGroupSynergy(SynergyResponse response, List<Commander> group)
    {
        GroupSynergyResponse groupSynergy = new()
        {
            Group = group
        };

        var commanders = response.Entities
            .Select(e => e.Commander)
            .Distinct()
            .ToList();

        Dictionary<Commander, GroupSynergyInfo> synergyInfos = [];
        foreach (var interest in group)
        {
            foreach (var cmdr in commanders)
            {
                var synergyEnt = response.Entities
                    .FirstOrDefault(e => e.Commander == interest && e.Teammate == cmdr);
                if (synergyEnt == null)
                {
                    continue;
                }
                if (!synergyInfos.TryGetValue(cmdr, out var infos))
                {
                    infos = new GroupSynergyInfo();
                    synergyInfos[cmdr] = infos;
                }
                infos.Counts.Add(synergyEnt.Count);
                infos.Wins.Add(synergyEnt.Wins);
                infos.AvgRatings.Add(synergyEnt.AvgRating);
                infos.AvgGains.Add(synergyEnt.AvgGain);
            }
        }

        var avgGainMax = response.Entities.Max(m => m.AvgGain); // normalized to 1
        var avgGainMin = response.Entities.Min(m => m.AvgGain); // normalized to 0

        foreach (var (teammate, synergyInfo) in synergyInfos)
        {
            groupSynergy.Entities.Add(synergyInfo.ToSynergyEnt(teammate, avgGainMax, avgGainMin));
        }

        return groupSynergy;
    }
}

internal record GroupSynergyInfo
{
    public List<int> Counts { get; set; } = [];
    public List<int> Wins { get; set; } = [];
    public List<double> AvgRatings { get; set; } = [];
    public List<double> AvgGains { get; set; } = [];

    public SynergyEnt ToSynergyEnt(Commander teammate, double avgGainMax, double avgGainMin)
    {
        // Calculate weighted average for AvgRating
        double totalWeightedRatingSum = 0;
        double totalCountForAvgRating = 0;
        for (int i = 0; i < Counts.Count; i++)
        {
            totalWeightedRatingSum += AvgRatings[i] * Counts[i];
            totalCountForAvgRating += Counts[i];
        }

        double finalAvgRating = totalCountForAvgRating > 0 ? totalWeightedRatingSum / totalCountForAvgRating : 0;

        // Calculate weighted average for AvgGain
        double totalWeightedGainSum = 0;
        double totalCountForAvgGain = 0;
        for (int i = 0; i < Counts.Count; i++)
        {
            totalWeightedGainSum += AvgGains[i] * Counts[i];
            totalCountForAvgGain += Counts[i];
        }

        double finalAvgGain = totalCountForAvgGain > 0 ? totalWeightedGainSum / totalCountForAvgGain : 0;

        return new SynergyEnt
        {
            Commander = Commander.None, // Placeholder for the group
            Teammate = teammate,
            Count = Counts.Sum(),
            Wins = Wins.Sum(),
            AvgRating = finalAvgRating,
            AvgGain = finalAvgGain,
            NormalizedAvgGain = totalCountForAvgGain > 0
                ? (finalAvgGain - avgGainMin) / (avgGainMax - avgGainMin)
                : 0
        };
    }
}