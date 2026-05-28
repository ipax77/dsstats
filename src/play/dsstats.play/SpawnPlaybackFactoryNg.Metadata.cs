using dsstats.shared;

namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private const int PlanetaryX = 160;
    private const int PlanetaryY = 152;
    private const int BunkerX = 146;
    private const int BunkerY = 138;
    private const int NexusX = 96;
    private const int NexusY = 88;
    private const int CannonX = 110;
    private const int CannonY = 102;

    private static int GetExpiresGameloop(int spawnGameloop, int? diedGameloop)
    {
        return diedGameloop ?? spawnGameloop + MaxUnitLifetimeGameloops;
    }

    private static SpawnPlaybackMiddleControl GetMiddleControl(ReplayDto replay)
    {
        if (replay.MiddleChanges.Count < 2 || replay.MiddleChanges[0] is not (1 or 2))
        {
            return new(0, []);
        }

        int[] changeGameloops = new int[replay.MiddleChanges.Count - 1];
        for (int i = 1; i < replay.MiddleChanges.Count; i++)
        {
            changeGameloops[i - 1] = ToGameloop(replay.MiddleChanges[i]);
        }

        return new(replay.MiddleChanges[0], changeGameloops);
    }

    private static SpawnPlaybackLandmark[] GetReplayDtoLandmarks(ReplayDto replay)
    {
        return
        [
            new("Planetary", "Base", 1, PlanetaryX, PlanetaryY, 14, "#5DADEC", 0),
            new("Bunker", "Defense", 1, BunkerX, BunkerY, 9, "#F8D34A", 0, ToGameloopOrNull(replay.Bunker)),
            new("Cannon", "Defense", 2, CannonX, CannonY, 9, "#F59E0B", 0, ToGameloopOrNull(replay.Cannon)),
            new("Nexus", "Base", 2, NexusX, NexusY, 14, "#F87171", 0),
        ];
    }

    private static SpawnPlaybackSnapshot[] GetSnapshots(SpawnPlaybackSidecarDto sidecar)
    {
        SpawnPlaybackSnapshot[] snapshots = new SpawnPlaybackSnapshot[sidecar.Snapshots.Count];
        for (int i = 0; i < snapshots.Length; i++)
        {
            var snapshot = sidecar.Snapshots[i];
            snapshots[i] = new(snapshot.SpawnNumber, snapshot.StartGameloop, snapshot.EndGameloop);
        }

        return snapshots;
    }

    private static int[] GetRefineryGameloops(IReadOnlyList<int> refinerySeconds)
    {
        if (refinerySeconds.Count == 0)
        {
            return [];
        }

        int[] gameloops = new int[refinerySeconds.Count];
        for (int i = 0; i < gameloops.Length; i++)
        {
            gameloops[i] = ToGameloop(refinerySeconds[i]);
        }

        return gameloops;
    }

    private static int[] GetTierUpgradeGameloops(IReadOnlyList<int> tierUpgradeSeconds)
    {
        if (tierUpgradeSeconds.Count == 0)
        {
            return [];
        }

        int[] gameloops = new int[tierUpgradeSeconds.Count];
        for (int i = 0; i < gameloops.Length; i++)
        {
            gameloops[i] = ToGameloop(tierUpgradeSeconds[i]);
        }

        Array.Sort(gameloops);
        return gameloops;
    }

    private static int GetMaxSimultaneousActiveSpawns(List<(int Gameloop, int Delta)> spawnEvents)
    {
        int activeSpawns = 0;
        int maxActiveSpawns = 0;
        spawnEvents.Sort(static (left, right) =>
        {
            int gameloopComparison = left.Gameloop.CompareTo(right.Gameloop);
            return gameloopComparison != 0
                ? gameloopComparison
                : left.Delta.CompareTo(right.Delta);
        });

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            activeSpawns = Math.Max(0, activeSpawns + spawnEvents[i].Delta);
            maxActiveSpawns = Math.Max(maxActiveSpawns, activeSpawns);
        }

        return maxActiveSpawns;
    }

    private static int ToGameloop(int seconds)
    {
        return seconds <= 0
            ? 0
            : (int)Math.Round(seconds * GameloopsPerSecond);
    }

    private static int? ToGameloopOrNull(int seconds)
    {
        return seconds <= 0 ? null : ToGameloop(seconds);
    }

    private readonly record struct UnitKindKey(Commander Commander, string UnitName);

    private readonly record struct PlayerUnitSummaryKey(int PlayerIndex, int UnitKindIndex);

    private readonly record struct SpawnRange(int StartGameloop, int EndGameloop);

    private struct BoundsBuilder
    {
        private double minX = double.MaxValue;
        private double minY = double.MaxValue;
        private double maxX = double.MinValue;
        private double maxY = double.MinValue;

        public BoundsBuilder()
        {
        }

        public void AddPoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        public readonly SpawnPlaybackBounds ToBounds()
        {
            if (minX == double.MaxValue)
            {
                return new(0, 0, MapWidth, MapHeight);
            }

            const double padding = 8;
            return new(
                Math.Floor(Math.Max(0, minX - padding)),
                Math.Floor(Math.Max(0, minY - padding)),
                Math.Ceiling(Math.Min(MapWidth, maxX + padding)),
                Math.Ceiling(Math.Min(MapHeight, maxY + padding)));
        }
    }
}
