using dsstats.shared;

namespace dsstats.weblib.Replays;

public static class BuildChartTarget
{
    public static string GetKey(string replayHash, int gamePos, Breakpoint breakpoint)
    {
        return $"{replayHash}_{gamePos}_bp_{(int)breakpoint}";
    }

    public static string GetWaveKey(string replayHash, int gamePos, int spawnNumber, int startGameloop)
    {
        return $"{replayHash}_{gamePos}_spawn_{spawnNumber}_{startGameloop}";
    }

    public static string GetSpawnPlaybackKey(string replayHash, int gamePos, int team, Commander commander)
    {
        return $"{replayHash}_{gamePos}_spawn_{team}_{(int)commander}";
    }

    public static string? GetVisibleKey(bool isVisible, string replayHash, int gamePos, Breakpoint breakpoint)
    {
        return isVisible
            ? GetKey(replayHash, gamePos, breakpoint)
            : null;
    }

    public static string? GetVisibleWaveKey(
        bool isVisible,
        string replayHash,
        int gamePos,
        int spawnNumber,
        int startGameloop)
    {
        return isVisible
            ? GetWaveKey(replayHash, gamePos, spawnNumber, startGameloop)
            : null;
    }

    public static string? GetVisibleSpawnPlaybackKey(
        bool isVisible,
        string replayHash,
        int gamePos,
        int team,
        Commander commander)
    {
        return isVisible
            ? GetSpawnPlaybackKey(replayHash, gamePos, team, commander)
            : null;
    }
}
