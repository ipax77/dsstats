using dsstats.shared;

namespace dsstats.shared.Maui;

public static class MauiSessionProgressCalculator
{
    public static MauiConfigDto NormalizeSettings(MauiConfigDto config)
    {
        config.SessionWindowMode = config.SessionWindowMode is MauiSessionWindowMode.Time or MauiSessionWindowMode.Count
            ? config.SessionWindowMode
            : MauiSessionWindowMode.Time;

        config.SessionWindowHours = config.SessionWindowHours switch
        {
            3 or 6 or 12 or 24 => config.SessionWindowHours,
            _ => 6,
        };

        config.SessionWindowReplayCount = config.SessionWindowReplayCount switch
        {
            10 or 20 or 30 or 50 => config.SessionWindowReplayCount,
            _ => 10,
        };

        config.SessionWindowGameMode = Enum.IsDefined(config.SessionWindowGameMode)
            ? config.SessionWindowGameMode
            : GameMode.None;

        return config;
    }

    public static bool MatchesToonId(ToonIdDto left, ToonIdDto right)
        => left.Id == right.Id &&
           left.Region == right.Region &&
           left.Realm == right.Realm;

    public static double GetRatingGain(ToonIdDto toonId, ReplayRatingDto? rating)
    {
        if (rating is null)
        {
            return 0;
        }

        var playerRating = rating.ReplayPlayerRatings.FirstOrDefault(player => MatchesToonId(player.ToonId, toonId));
        return playerRating?.RatingDelta ?? 0;
    }

    public static bool IsWin(ReplayDto replay, ToonIdDto toonId)
    {
        var player = replay.Players.FirstOrDefault(p => MatchesToonId(p.Player.ToonId, toonId));
        return player is not null && player.TeamId == replay.WinnerTeam;
    }
}
