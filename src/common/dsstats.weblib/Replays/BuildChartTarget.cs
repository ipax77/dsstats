using dsstats.shared;

namespace dsstats.weblib.Replays;

public static class BuildChartTarget
{
    public static string GetKey(string replayHash, int gamePos, Breakpoint breakpoint)
    {
        return $"{replayHash}_{gamePos}_{(int)breakpoint}";
    }

    public static string? GetVisibleKey(bool isVisible, string replayHash, int gamePos, Breakpoint breakpoint)
    {
        return isVisible
            ? GetKey(replayHash, gamePos, breakpoint)
            : null;
    }
}
