using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly Dictionary<string, SessionReplayInfo> sessionReplayInfos = [];
    private readonly ConcurrentDictionary<string, ReplayRatingDto?> remoteRatings = [];

    private readonly DateTime sessionStart = DateTime.UtcNow;
    // private readonly DateTime sessionStart = new DateTime(2024, 11, 08);

    public async Task ReloadSessionReplayInfos(bool remote)
    {
        if (remote)
        {
            remoteRatings.Clear();
        }
        await GetSessionReplayInfos(remote);
    }

    public async Task<List<SessionReplayInfo>> GetSessionReplayInfos(bool remote)
    {
        var replayHashes = sessionReplayInfos.Keys.ToList();

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        var infos = await context.Replays
            .Where(x => x.GameTime > sessionStart
                && (replayHashes.Count == 0 || !replayHashes.Contains(x.ReplayHash)))
            .Select(s => new
            {
                s.ReplayHash,
                s.GameTime,
                s.Duration,
                s.GameMode,
                s.TournamentEdition,
                Players = s.ReplayPlayers.Select(t => new
                {
                    RequestNames = new RequestNames(t.Name, t.Player.ToonId, t.Player.RegionId, t.Player.RealmId),
                    Change = t.ReplayPlayerRatingInfo == null ? 0 : t.ReplayPlayerRatingInfo.RatingChange,
                    t.Race,
                    t.PlayerResult,
                    t.GamePos
                })
            })
            .ToListAsync();

        var requestNames = configService.GetRequestNames();

        foreach (var info in infos)
        {
            var player = info.Players.FirstOrDefault(f => requestNames.Contains(f.RequestNames));
            if (player is null)
            {
                continue;
            }
            sessionReplayInfos[info.ReplayHash] = new()
            {
                ReplayHash = info.ReplayHash,
                GameTime = info.GameTime,
                GameMode = info.GameMode,
                Duration = info.Duration,
                TournamentEdition = info.TournamentEdition,
                Commander = player.Race,
                PlayerResult = player.PlayerResult,
                GamePos = player.GamePos,
                LocalRatingGain = player.Change
            };
        }

        if (remote)
        {
            await AddRemoteRatings(requestNames);
        }

        return [.. sessionReplayInfos.Values];
    }

    public void AddRemoteRating(string replayHash, ReplayRatingDto? rating)
    {
        remoteRatings.AddOrUpdate(replayHash, rating, (k, v) => v = rating is null ? v : rating);
    }

    private async Task AddRemoteRatings(List<RequestNames> requestNames)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var replaysService = scope.ServiceProvider.GetRequiredKeyedService<IReplaysService>("remote");

        foreach (var info in sessionReplayInfos.Values)
        {
            if (info.RemoteRatingGain != 0)
            {
                continue;
            }

            if (!remoteRatings.TryGetValue(info.ReplayHash, out ReplayRatingDto? rating)
                || rating is null)
            {
                rating = await replaysService.GetReplayRating(info.ReplayHash, !info.TournamentEdition);
                AddRemoteRating(info.ReplayHash, rating);
            }

            if (rating is null)
            {
                continue;
            }

            var player = rating.RepPlayerRatings.FirstOrDefault(f => f.GamePos == info.GamePos);

            if (player is null)
            {
                continue;
            }

            info.RemoteRatingGain = (float)player.RatingChange;
        }
    }
}

public record SessionReplayInfo
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime GameTime { get; set; }
    public GameMode GameMode { get; set; }
    public int Duration { get; set; }
    public bool TournamentEdition { get; set; }
    public Commander Commander { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int GamePos { get; set; }
    public float LocalRatingGain { get; set; }
    public float RemoteRatingGain { get; set; }
}