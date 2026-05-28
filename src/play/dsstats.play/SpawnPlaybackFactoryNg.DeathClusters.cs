using dsstats.shared;

namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private const int DeathClusterMinimumDeaths = 4;
    private const int DeathClusterWindowGameloops = 112;
    private const int DeathClusterRadius = 8;
    private const int DeathClusterRadiusSquared = DeathClusterRadius * DeathClusterRadius;

    private static DeathCluster[] CreateDeathClusters(
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers,
        Dictionary<int, ReplayPlayerDto> replayPlayersByGamePos)
    {
        int deathCount = CountDeathsWithPositions(sidecarPlayers, replayPlayersByGamePos);
        if (deathCount < DeathClusterMinimumDeaths)
        {
            return [];
        }

        DeathEvent[] deaths = new DeathEvent[deathCount];
        int deathIndex = 0;
        for (int playerIndex = 0; playerIndex < sidecarPlayers.Length; playerIndex++)
        {
            var sidecarPlayer = sidecarPlayers[playerIndex];
            if (!replayPlayersByGamePos.ContainsKey(sidecarPlayer.GamePos))
            {
                continue;
            }

            var units = sidecarPlayer.Units;
            for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
            {
                var unit = units[unitIndex];
                if (unit.DiedGameloop is int diedGameloop
                    && unit.DiedX is int diedX
                    && unit.DiedY is int diedY)
                {
                    deaths[deathIndex++] = new(diedGameloop, diedX, diedY);
                }
            }
        }

        Array.Sort(deaths, static (left, right) => left.Gameloop.CompareTo(right.Gameloop));
        List<DeathClusterAccumulator> clusters = [];
        int windowEnd = 0;
        for (int start = 0; start < deaths.Length; start++)
        {
            int windowLimit = deaths[start].Gameloop + DeathClusterWindowGameloops;
            while (windowEnd < deaths.Length && deaths[windowEnd].Gameloop <= windowLimit)
            {
                windowEnd++;
            }

            int count = 0;
            int firstGameloop = int.MaxValue;
            int lastGameloop = 0;
            double sumX = 0;
            double sumY = 0;
            for (int candidateIndex = start; candidateIndex < windowEnd; candidateIndex++)
            {
                var candidate = deaths[candidateIndex];
                if (GetDistanceSquared(deaths[start], candidate) > DeathClusterRadiusSquared)
                {
                    continue;
                }

                count++;
                firstGameloop = Math.Min(firstGameloop, candidate.Gameloop);
                lastGameloop = Math.Max(lastGameloop, candidate.Gameloop);
                sumX += candidate.X;
                sumY += candidate.Y;
            }

            if (count >= DeathClusterMinimumDeaths)
            {
                AddDeathClusterCandidate(clusters, new(count, firstGameloop, lastGameloop, sumX, sumY));
            }
        }

        if (clusters.Count == 0)
        {
            return [];
        }

        DeathCluster[] result = new DeathCluster[clusters.Count];
        for (int i = 0; i < result.Length; i++)
        {
            var cluster = clusters[i];
            double centerX = cluster.CenterX;
            double centerY = cluster.CenterY;
            result[i] = new(cluster.FirstGameloop, cluster.LastGameloop, centerX + centerY, centerX, centerY, cluster.Count);
        }

        Array.Sort(result, static (left, right) =>
        {
            int sumComparison = left.LineSum.CompareTo(right.LineSum);
            return sumComparison != 0
                ? sumComparison
                : left.FirstGameloop.CompareTo(right.FirstGameloop);
        });
        return result;
    }

    private static int CountDeathsWithPositions(
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers,
        Dictionary<int, ReplayPlayerDto> replayPlayersByGamePos)
    {
        int count = 0;
        for (int playerIndex = 0; playerIndex < sidecarPlayers.Length; playerIndex++)
        {
            var sidecarPlayer = sidecarPlayers[playerIndex];
            if (!replayPlayersByGamePos.ContainsKey(sidecarPlayer.GamePos))
            {
                continue;
            }

            var units = sidecarPlayer.Units;
            for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
            {
                var unit = units[unitIndex];
                if (unit.DiedGameloop is not null
                    && unit.DiedX is not null
                    && unit.DiedY is not null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AddDeathClusterCandidate(List<DeathClusterAccumulator> clusters, DeathClusterAccumulator candidate)
    {
        for (int i = clusters.Count - 1; i >= 0; i--)
        {
            var existing = clusters[i];
            if (existing.LastGameloop < candidate.FirstGameloop)
            {
                break;
            }

            if (existing.FirstGameloop <= candidate.LastGameloop
                && GetDistanceSquared(existing.CenterX, existing.CenterY, candidate.CenterX, candidate.CenterY) <= DeathClusterRadiusSquared)
            {
                clusters[i] = existing.Merge(candidate);
                return;
            }
        }

        clusters.Add(candidate);
    }

    private static double GetDistanceSquared(DeathEvent left, DeathEvent right)
    {
        return GetDistanceSquared(left.X, left.Y, right.X, right.Y);
    }

    private static double GetDistanceSquared(double leftX, double leftY, double rightX, double rightY)
    {
        double deltaX = leftX - rightX;
        double deltaY = leftY - rightY;
        return deltaX * deltaX + deltaY * deltaY;
    }

    private readonly record struct DeathEvent(int Gameloop, int X, int Y);

    private readonly record struct DeathCluster(
        int FirstGameloop,
        int LastGameloop,
        double LineSum,
        double CenterX,
        double CenterY,
        int Count);

    private readonly record struct DeathClusterAccumulator(
        int Count,
        int FirstGameloop,
        int LastGameloop,
        double SumX,
        double SumY)
    {
        public double CenterX => SumX / Count;

        public double CenterY => SumY / Count;

        public DeathClusterAccumulator Merge(DeathClusterAccumulator other)
        {
            return new(
                Count + other.Count,
                Math.Min(FirstGameloop, other.FirstGameloop),
                Math.Max(LastGameloop, other.LastGameloop),
                SumX + other.SumX,
                SumY + other.SumY);
        }
    }
}
