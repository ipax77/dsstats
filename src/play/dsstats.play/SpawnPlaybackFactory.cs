using dsstats.shared.Units;
using ExternalDirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;
using SharedCommander = dsstats.shared.Commander;

namespace dsstats.play;

public static class SpawnPlaybackFactory
{
    public const double GameloopsPerSecond = 22.4;
    public const int DefaultStepSeconds = 5;
    public const int MapWidth = 256;
    public const int MapHeight = 240;

    private static readonly SpawnPlaybackLandmark[] DefaultLandmarks =
    [
        new("Nexus", "Base", 1, 190, 184, 14, "#5DADEC", 0),
        new("Cannon", "Defense", 1, 151, 141, 9, "#F8D34A", 0),
        new("Bunker", "Defense", 2, 105, 99, 9, "#F59E0B", 0),
        new("Planetary", "Base", 2, 66, 56, 14, "#F87171", 0),
    ];

    public static SpawnPlaybackReplay Create(
        ExternalDirectStrikeReplay replay,
        IReadOnlyList<SpawnPlaybackLandmark>? landmarks = null,
        IReadOnlyDictionary<SpawnPlaybackUnitKey, int>? unitKills = null,
        IReadOnlyList<SpawnPlaybackBuildUnit>? buildUnits = null)
    {
        ArgumentNullException.ThrowIfNull(replay);

        Dictionary<(SharedCommander Commander, string UnitName), (double Radius, string Color)> displayCache = [];
        List<SpawnPlaybackPlayer> players = new(replay.Players.Count);
        IReadOnlyList<SpawnPlaybackLandmark> playbackLandmarks = landmarks?.Count > 0
            ? landmarks
            : DefaultLandmarks;
        int durationGameloop = Math.Max(1, ToGameloop(replay.Duration));

        int spawnCount = 0;
        int unitsWithDiedEvent = 0;
        int unitsWithDiedPosition = 0;
        List<(int Gameloop, int Delta)> spawnEvents = [];

        foreach (var player in replay.Players.OrderBy(p => p.TeamId).ThenBy(p => p.GamePos))
        {
            SharedCommander commander = (SharedCommander)(int)player.Commander;
            List<SpawnPlaybackUnit> units = new(player.Spawns.Sum(spawn => spawn.Units.Count));

            foreach (var spawn in player.Spawns)
            {
                if (spawn.Units.Count > 0)
                {
                    spawnCount++;
                    spawnEvents.Add((spawn.StartGameloop, 1));
                    spawnEvents.Add((spawn.EndGameloop, -1));
                }

                durationGameloop = Math.Max(durationGameloop, spawn.EndGameloop);

                foreach (var unit in spawn.Units)
                {
                    durationGameloop = Math.Max(durationGameloop, unit.Gameloop);
                    if (unit.DiedGameloop is int diedGameloop)
                    {
                        unitsWithDiedEvent++;
                        durationGameloop = Math.Max(durationGameloop, diedGameloop);
                    }
                    if (unit.DiedX is not null && unit.DiedY is not null)
                    {
                        unitsWithDiedPosition++;
                    }

                    var displayInfo = GetDisplayInfo(displayCache, commander, unit.Name);
                    var target = GetTarget(unit.X, unit.Y, unit.DiedX, unit.DiedY);
                    var unitInfo = UnitMap.GetUnitInfo(unit.Name, commander);

                    units.Add(new(
                        unit.UnitIndex,
                        unitInfo.Name,
                        unit.Gameloop,
                        unit.X,
                        unit.Y,
                        unit.DiedGameloop,
                        unit.DiedX,
                        unit.DiedY,
                        target.X,
                        target.Y,
                        displayInfo.Radius,
                        displayInfo.Color,
                        GetUnitKills(unitKills, unit)));
                }
            }

            players.Add(new(
                player.Name,
                player.TeamId,
                player.GamePos,
                commander.ToString(),
                units));
        }

        return new(
            durationGameloop,
            (int)Math.Round(DefaultStepSeconds * GameloopsPerSecond),
            GetBounds(players, playbackLandmarks),
            new(
                replay.Players.Count,
                spawnCount,
                players.Sum(player => player.Units.Count),
                unitsWithDiedEvent,
                unitsWithDiedPosition,
                GetMaxSimultaneousActiveSpawns(spawnEvents)),
            playbackLandmarks,
            buildUnits ?? [],
            players);
    }

    public static SpawnPlaybackUnitKey GetUnitKey(
        int unitIndex,
        int spawnGameloop,
        int spawnX,
        int spawnY,
        string name)
    {
        return new(unitIndex, spawnGameloop, spawnX, spawnY, name);
    }

    private static (double Radius, string Color) GetDisplayInfo(
        Dictionary<(SharedCommander Commander, string UnitName), (double Radius, string Color)> cache,
        SharedCommander commander,
        string unitName)
    {
        var key = (commander, unitName);
        if (cache.TryGetValue(key, out var displayInfo))
        {
            return displayInfo;
        }

        displayInfo = UnitMapNg.GetColorAndRadius(unitName, commander);
        cache[key] = displayInfo;
        return displayInfo;
    }

    private static (double X, double Y) GetTarget(int spawnX, int spawnY, int? diedX, int? diedY)
    {
        if (diedX is int x && diedY is int y)
        {
            return (x, y);
        }

        return (MapWidth - spawnX, MapHeight - spawnY);
    }

    private static int GetUnitKills(
        IReadOnlyDictionary<SpawnPlaybackUnitKey, int>? unitKills,
        Sc2DirectStrike.Parser.DirectStrikeSpawnUnit unit)
    {
        if (unitKills is null)
        {
            return 0;
        }

        var key = GetUnitKey(unit.UnitIndex, unit.Gameloop, unit.X, unit.Y, unit.Name);
        return unitKills.GetValueOrDefault(key);
    }

    private static SpawnPlaybackBounds GetBounds(
        IReadOnlyList<SpawnPlaybackPlayer> players,
        IReadOnlyList<SpawnPlaybackLandmark> landmarks)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var unit in players.SelectMany(player => player.Units))
        {
            AddPoint(unit.SpawnX, unit.SpawnY);
            AddPoint(unit.TargetX, unit.TargetY);
            if (unit.DiedX is double diedX && unit.DiedY is double diedY)
            {
                AddPoint(diedX, diedY);
            }
        }

        foreach (var landmark in landmarks)
        {
            AddPoint(landmark.X, landmark.Y);
        }

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

        void AddPoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
    }

    private static int GetMaxSimultaneousActiveSpawns(List<(int Gameloop, int Delta)> spawnEvents)
    {
        int activeSpawns = 0;
        int maxActiveSpawns = 0;

        foreach (var spawnEvent in spawnEvents.OrderBy(e => e.Gameloop).ThenBy(e => e.Delta))
        {
            activeSpawns = Math.Max(0, activeSpawns + spawnEvent.Delta);
            maxActiveSpawns = Math.Max(maxActiveSpawns, activeSpawns);
        }

        return maxActiveSpawns;
    }

    private static int ToGameloop(TimeSpan value)
    {
        return value <= TimeSpan.Zero
            ? 0
            : (int)Math.Round(value.TotalSeconds * GameloopsPerSecond);
    }
}
