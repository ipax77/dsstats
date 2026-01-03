using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Collections.Concurrent;

namespace dsstats.maui.Services;

public class SessionProgress(IReplayRepository replayRepository, [FromKeyedServices("api")] IReplayRepository apiReplayRepository, DsstatsService dsstatsService)
{
    private MauiConfig? _mauiConfig;
    private readonly ConcurrentDictionary<string, SessionReplay> sessionReplays = [];

    public SessionReplayResult GetSessionReplays()
    {
        if (sessionReplays.IsEmpty)
        {
            return new([], 0, 0, 0);
        }
        var replays = sessionReplays.Values.OrderByDescending(o => o.Gametime).ToList();
        var totalDuration = replays.Sum(s => s.Duration);
        var totalGain = replays.Sum(a => a.RatingGain);
        var wins = replays.Count(c => c.RatingGain > 0);
        return new(replays, totalDuration, totalGain, wins / (double)replays.Count);
    }

    public async Task<ReplayRatingDto?> AddSessionReplay(ReplayDetails replayDetails)
    {
        if (sessionReplays.ContainsKey(replayDetails.ReplayHash))
        {
            return null;
        }
        _mauiConfig = await dsstatsService.GetConfig();
        var rating = CanHaveRating(replayDetails.Replay) ? await UpdateRating(replayDetails.ReplayHash) : null;
        var ratingGain = GetRatingGain(_mauiConfig.Sc2Profiles, rating);
        SessionReplay sessionReplay = new()
        {
            ReplayHash = replayDetails.ReplayHash,
            Gametime = replayDetails.Replay.Gametime,
            GameMode = replayDetails.Replay.GameMode,
            Duration = replayDetails.Replay.Duration,
            RatingGain = ratingGain,
        };
        sessionReplays.AddOrUpdate(replayDetails.ReplayHash, sessionReplay, (k, v) => v = sessionReplay);
        return rating;
    }

    private static bool CanHaveRating(ReplayDto replay)
    {
        return replay.Players.Count > 1 && replay.Duration >= 300;
    }

    private static double GetRatingGain(IEnumerable<Sc2Profile> profiles, ReplayRatingDto? rating)
    {
        if (rating is null)
        {
            return 0;
        }
        foreach (var player in rating.ReplayPlayerRatings)
        {
            var configPlayer = profiles.FirstOrDefault(f =>
                f.ToonId.Id == player.ToonId.Id
                && f.ToonId.Realm == player.ToonId.Realm
                && f.ToonId.Region == player.ToonId.Region);
            if (configPlayer != null)
            {
                return player.RatingDelta;
            }
        }
        return 0;
    }

    public async Task<ReplayRatingDto?> UpdateRating(string replayHash)
    {
        try
        {
            var rating = await apiReplayRepository.GetReplayRating(replayHash);
            if (rating is null)
            {
                return null;
            }
            if (_mauiConfig != null && sessionReplays.TryGetValue(replayHash, out var replayDetails))
            {
                replayDetails.RatingGain = GetRatingGain(_mauiConfig.Sc2Profiles, rating);
            }
            await replayRepository.SaveReplayRatingAll(replayHash, rating);
            return rating;
        }
        catch { }
        return null;
    }
}

public sealed record SessionReplay
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime Gametime { get; set; }
    public GameMode GameMode { get; set; }
    public int Duration { get; set; }
    public double RatingGain { get; set; }
}

public sealed record SessionReplayResult(List<SessionReplay> Replays, long TotalDuration, double TotalGain, double Winrate);
