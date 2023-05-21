using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Collections.Concurrent;

namespace sc2dsstats.maui.Services;

public partial class DecodeService
{
    // private DateTime sessionStart = new DateTime(2023, 05, 17);
    private DateTime sessionStart = DateTime.MinValue;
    private ConcurrentDictionary<string, ReplayRatingDto?> replayOnlineRatings = new();

    public void SetSessionStart()
    {
        if (sessionStart == DateTime.MinValue)
        {
            sessionStart = DateTime.UtcNow;
        }
    }

    public async Task<SessionProgress> GetSessionProgress(bool online)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.ReplayPlayerRatingInfo)
            .Include(i => i.ReplayRatingInfo)
            .Where(x => x.GameTime >= sessionStart)
            .OrderBy(o => o.GameTime)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
    
        var toonIdInfos = UserSettingsService.UserSettings.BattleNetInfos
            .SelectMany(s => s.ToonIds)
            .Distinct()
            .ToList();

        if (online)
        {
            await SetOnlineRatings(replays);
        }

        return new()
        {
            SessionStart = sessionStart,
            SessionGames = replays.Select(s => GetSessionGameInfo(s, toonIdInfos, online)).ToList(),
        };
    }

    private SessionGameInfo GetSessionGameInfo(Replay replay, List<ToonIdInfo> toonIds, bool online)
    {
        var playerReplayPlayer = GetPlayerReplayPlayer(replay, toonIds);

        float ratingGain = 0;

        if (online && playerReplayPlayer != null
            && replayOnlineRatings.TryGetValue(replay.ReplayHash, out var onlineRating) 
            && onlineRating != null)
        {
            var onlinePlayerRating = onlineRating.RepPlayerRatings
                .FirstOrDefault(f => f.GamePos == playerReplayPlayer.GamePos);
            ratingGain = onlinePlayerRating?.RatingChange ?? 0;
        }
        else
        {
            ratingGain = playerReplayPlayer?.ReplayPlayerRatingInfo?.RatingChange ?? 0;
        }

        return new SessionGameInfo()
        {
            ReplayHash = replay.ReplayHash,
            GameTime = replay.GameTime,
            GameMode = replay.GameMode,
            RatingType = replay.ReplayRatingInfo?.RatingType ?? RatingType.None,
            RequestNames = playerReplayPlayer == null ? null
                : new(playerReplayPlayer.Name,
                    playerReplayPlayer.Player.ToonId,
                    playerReplayPlayer.Player.RegionId,
                    playerReplayPlayer.Player.RealmId),
            Commander = playerReplayPlayer?.Race ?? Commander.None,
            PlayerResult = playerReplayPlayer?.PlayerResult ?? PlayerResult.None,
            Duration = replay.Duration,
            RatingGain = ratingGain,
            ExpectationToWin = GetExpectationToWin(replay, playerReplayPlayer),
        };
    }

    private float GetExpectationToWin(Replay replay, ReplayPlayer? replayPlayer)
    {
        if (replayPlayer != null 
            && replayPlayer.PlayerResult == PlayerResult.Los
            && replay.ReplayRatingInfo != null)
        {
            return 1 - replay.ReplayRatingInfo.ExpectationToWin;
        }
        return replay.ReplayRatingInfo?.ExpectationToWin ?? 0;
    }

    private ReplayPlayer? GetPlayerReplayPlayer(Replay replay, List<ToonIdInfo> toonIdInfos)
    {
        return replay.ReplayPlayers.FirstOrDefault(rp =>
            toonIdInfos.Any(info =>
                 rp.Player.ToonId == info.ToonId
                && rp.Player.RealmId == info.RealmId
                && rp.Player.RegionId == info.RegionId
            )
        );
    }

    private async Task SetOnlineRatings(List<Replay> replays)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        foreach (var replay in replays)
        {
            if (replay.ReplayRatingInfo == null)
            {
                continue;
            }

            if (!replayOnlineRatings.TryGetValue(replay.ReplayHash, out ReplayRatingDto? onlineRating))
            {
                PlayerIdRatingRequest request = new()
                {
                    RatingType = replay.ReplayRatingInfo.RatingType,
                    PlayerIds = replay.ReplayPlayers
                        .Select(s => new PlayerId(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId))
                        .ToList()
                };

                var calcRatings = await dataService.GetPlayerIdCalcRatings(request, default);

                var replayDto = mapper.Map<ReplayDetailsDto>(replay);
                onlineRating = ratingRepository.GetOnlineRating(replayDto, calcRatings);
                replayOnlineRatings.TryAdd(replay.ReplayHash, onlineRating);
            }
        }
    }

    public async Task<ReplayRatingDto?> GetOnlineRating(ReplayDetailsDto replayDetailsDto)
    {
        if (!replayOnlineRatings.TryGetValue(replayDetailsDto.ReplayHash, out ReplayRatingDto? onlineRating))
        {
            if (replayDetailsDto.ReplayRatingInfo == null)
            {
                replayOnlineRatings.TryAdd(replayDetailsDto.ReplayHash, null);
                return null;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

            PlayerIdRatingRequest request = new()
            {
                RatingType = replayDetailsDto.ReplayRatingInfo.RatingType,
                PlayerIds = replayDetailsDto.ReplayPlayers
                    .Select(s => new PlayerId(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId))
                    .ToList()
            };

            var calcRatings = await dataService.GetPlayerIdCalcRatings(request, default);

            onlineRating = ratingRepository.GetOnlineRating(replayDetailsDto, calcRatings);
            replayOnlineRatings.TryAdd(replayDetailsDto.ReplayHash, onlineRating);
        }
        return onlineRating;
    }
}

public record SessionGameInfo
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime GameTime { get; set; }
    public GameMode GameMode { get; set; }
    public RatingType RatingType { get; set; }
    public RequestNames? RequestNames { get; set; }
    public Commander Commander { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int Duration { get; set; }
    public float RatingGain { get; set; }
    public float ExpectationToWin { get; set; }
}
public record SessionProgress
{
    public DateTime SessionStart { get; set; }
    public List<SessionGameInfo> SessionGames { get; set; } = new();
}

public record SessionSummary
{
    public RatingType RatingType { get; set; }
    public GameMode GameMode { get; set; }
    public RequestNames? RequestNames { get; set; }
    public List<float> Gains { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Duration { get; set; }
}