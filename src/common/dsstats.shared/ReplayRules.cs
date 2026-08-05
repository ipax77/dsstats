namespace dsstats.shared;

public static class ReplayRules
{
    public const int LeaverGracePeriodSeconds = 90;

    public static bool IsLeaver(int replayDuration, int playerDuration)
        => playerDuration < replayDuration - LeaverGracePeriodSeconds;
}
