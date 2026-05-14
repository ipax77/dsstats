using dsstats.db;
using dsstats.shared;
using dsstats.shared.InHouse;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices.InHouse;

public sealed class InHouseGameSessionService(
    DsstatsContext context,
    IImportService importService) : IInHouseGameSessionService
{
    public async Task<List<InHouseGameSessionListDto>> GetActiveSessionsAsync(CancellationToken token)
    {
        return await context.InHouseGameSessions
            .AsNoTracking()
            .Where(session => session.ClosedAt == null)
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new InHouseGameSessionListDto
            {
                SessionId = session.PublicId,
                Name = session.Name,
                CreatedByUserId = session.CreatedBy!.PublicId,
                CreatedByDisplayName = session.CreatedBy.DisplayName,
                CreatedAt = session.CreatedAt,
                ClosedAt = session.ClosedAt,
                Games = session.Replays.Count,
                Players = session.PlayerSummaries.Count,
            })
            .ToListAsync(token);
    }

    public async Task<InHouseGameSessionDetailDto> CreateSessionAsync(
        int userId,
        InHouseCreateGameSessionRequest request,
        CancellationToken token)
    {
        var user = await context.InHouseUsers
            .FirstOrDefaultAsync(user => user.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        var name = NormalizeSessionName(request.Name, user.DisplayName);
        var session = new InHouseGameSession
        {
            PublicId = Guid.NewGuid(),
            Name = name,
            CreatedByInHouseUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        context.InHouseGameSessions.Add(session);
        await context.SaveChangesAsync(token);

        return await GetSessionAsync(session.PublicId, userId, token)
            ?? throw new InvalidOperationException("Created session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto?> GetSessionAsync(Guid sessionId, int userId, CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .AsNoTracking()
            .Where(session => session.PublicId == sessionId)
            .Select(session => new { session.InHouseGameSessionId })
            .FirstOrDefaultAsync(token);

        if (session is null)
        {
            return null;
        }

        await RefreshSummariesAsync(session.InHouseGameSessionId, token);
        return await LoadDetailAsync(sessionId, userId, token);
    }

    public async Task<InHouseGameSessionDetailDto> UploadReplayAsync(
        Guid sessionId,
        int userId,
        InHouseReplayUploadRequest request,
        CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .FirstOrDefaultAsync(session => session.PublicId == sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");

        if (session.ClosedAt is not null)
        {
            throw new InvalidOperationException("This InHouse session is closed.");
        }

        if (request.Replay.Players.Count == 0)
        {
            throw new InvalidOperationException("The uploaded replay has no players.");
        }

        var replayHash = request.Replay.ComputeHash();
        await importService.InsertReplays([request.Replay]);

        var replay = await context.Replays
            .Include(replay => replay.Players)
                .ThenInclude(player => player.Player)
            .FirstOrDefaultAsync(replay => replay.ReplayHash == replayHash, token)
            ?? throw new InvalidOperationException("The uploaded replay could not be imported.");

        var existing = await context.InHouseGameSessionReplays
            .AnyAsync(sessionReplay => sessionReplay.InHouseGameSessionId == session.InHouseGameSessionId
                && sessionReplay.ReplayId == replay.ReplayId, token);

        if (!existing)
        {
            var sessionReplay = new InHouseGameSessionReplay
            {
                InHouseGameSessionId = session.InHouseGameSessionId,
                ReplayId = replay.ReplayId,
                UploadedByInHouseUserId = userId,
                UploadedAt = DateTime.UtcNow,
                Players = CreateReplayPlayers(replay, request.Observers),
            };

            context.InHouseGameSessionReplays.Add(sessionReplay);
            await context.SaveChangesAsync(token);
        }

        await RefreshSummariesAsync(session.InHouseGameSessionId, token);
        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto> CloseSessionAsync(Guid sessionId, int userId, CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .FirstOrDefaultAsync(session => session.PublicId == sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");

        if (session.CreatedByInHouseUserId != userId)
        {
            throw new InvalidOperationException("Only the session creator can close this InHouse session.");
        }

        session.ClosedAt ??= DateTime.UtcNow;
        await context.SaveChangesAsync(token);
        await RefreshSummariesAsync(session.InHouseGameSessionId, token);

        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    private List<InHouseGameSessionReplayPlayer> CreateReplayPlayers(
        Replay replay,
        IReadOnlyCollection<InHouseReplayObserverDto> observers)
    {
        List<InHouseGameSessionReplayPlayer> players = new(replay.Players.Count + observers.Count);
        foreach (var replayPlayer in replay.Players)
        {
            var player = replayPlayer.Player
                ?? throw new InvalidOperationException("Replay player is missing player data.");

            players.Add(new()
            {
                ReplayPlayerId = replayPlayer.ReplayPlayerId,
                PlayerId = replayPlayer.PlayerId,
                Name = replayPlayer.Name,
                ToonId = CloneToonId(player.ToonId),
                Observer = false,
                TeamId = replayPlayer.TeamId,
                GamePos = replayPlayer.GamePos,
                Result = replayPlayer.Result,
            });
        }

        foreach (var observer in observers.Where(HasValidToonId))
        {
            var playerId = importService.GetOrCreatePlayerId(
                observer.Name,
                observer.ToonId.Region,
                observer.ToonId.Realm,
                observer.ToonId.Id,
                context);

            players.Add(new()
            {
                PlayerId = playerId,
                Name = observer.Name,
                ToonId = ToEntity(observer.ToonId),
                Observer = true,
                TeamId = 0,
                GamePos = observer.SlotId,
                Result = PlayerResult.None,
            });
        }

        return players;
    }

    private async Task RefreshSummariesAsync(int sessionId, CancellationToken token)
    {
        var sessionReplays = await context.InHouseGameSessionReplays
            .Include(sessionReplay => sessionReplay.Replay)
                .ThenInclude(replay => replay!.Ratings)
                    .ThenInclude(rating => rating.ReplayPlayerRatings)
            .Include(sessionReplay => sessionReplay.Players)
            .Where(sessionReplay => sessionReplay.InHouseGameSessionId == sessionId)
            .OrderBy(sessionReplay => sessionReplay.Replay!.Gametime)
            .ThenBy(sessionReplay => sessionReplay.InHouseGameSessionReplayId)
            .ToListAsync(token);

        var existing = await context.InHouseGameSessionPlayerSummaries
            .Where(summary => summary.InHouseGameSessionId == sessionId)
            .ToListAsync(token);
        context.InHouseGameSessionPlayerSummaries.RemoveRange(existing);

        if (sessionReplays.Count == 0)
        {
            await context.SaveChangesAsync(token);
            return;
        }

        var latestReplayId = sessionReplays[^1].InHouseGameSessionReplayId;
        Dictionary<ToonKey, SummaryBuilder> summaries = [];

        foreach (var sessionReplay in sessionReplays)
        {
            var replay = sessionReplay.Replay
                ?? throw new InvalidOperationException("Session replay is missing replay data.");
            var rating = GetBestRating(replay);

            foreach (var participant in sessionReplay.Players)
            {
                var key = new ToonKey(participant.ToonId.Region, participant.ToonId.Realm, participant.ToonId.Id);
                if (!summaries.TryGetValue(key, out var summary))
                {
                    summary = new SummaryBuilder(participant);
                    summaries.Add(key, summary);
                }

                summary.Name = participant.Name;
                summary.PlayerId ??= participant.PlayerId;
                var isLatestReplay = sessionReplay.InHouseGameSessionReplayId == latestReplayId;
                if (participant.Observer)
                {
                    summary.Observes++;
                    summary.ObservedLatestGame |= isLatestReplay;
                    continue;
                }

                summary.Games++;
                if (participant.Result == PlayerResult.Win)
                {
                    summary.Wins++;
                }

                summary.PlayedLatestGame |= isLatestReplay;

                var playerRating = rating?.ReplayPlayerRatings
                    .FirstOrDefault(playerRating => playerRating.ReplayPlayerId == participant.ReplayPlayerId
                        || playerRating.PlayerId == participant.PlayerId);
                if (playerRating is null)
                {
                    summary.RatingsPending = true;
                    continue;
                }

                summary.RatingGames++;
                summary.RatingDelta += playerRating.RatingDelta;
                summary.RatingStart ??= playerRating.RatingBefore;
                summary.RatingEnd = playerRating.RatingBefore + playerRating.RatingDelta;
            }
        }

        context.InHouseGameSessionPlayerSummaries.AddRange(summaries.Values.Select(summary => summary.ToEntity(sessionId)));
        await context.SaveChangesAsync(token);
    }

    private async Task<InHouseGameSessionDetailDto?> LoadDetailAsync(Guid sessionId, int userId, CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Include(session => session.PlayerSummaries)
            .Include(session => session.Replays)
                .ThenInclude(sessionReplay => sessionReplay.Replay)
                    .ThenInclude(replay => replay!.Players)
            .Include(session => session.Replays)
                .ThenInclude(sessionReplay => sessionReplay.Replay)
                    .ThenInclude(replay => replay!.Ratings)
                        .ThenInclude(rating => rating.ReplayPlayerRatings)
            .Where(session => session.PublicId == sessionId)
            .FirstOrDefaultAsync(token);

        if (session is null)
        {
            return null;
        }

        return new()
        {
            SessionId = session.PublicId,
            Name = session.Name,
            CreatedByUserId = session.CreatedBy!.PublicId,
            CreatedByDisplayName = session.CreatedBy.DisplayName,
            CreatedAt = session.CreatedAt,
            ClosedAt = session.ClosedAt,
            CanClose = session.CreatedByInHouseUserId == userId && session.ClosedAt == null,
            Players = session.PlayerSummaries
                .OrderByDescending(summary => summary.Games)
                .ThenByDescending(summary => summary.RatingDelta ?? 0)
                .ThenBy(summary => summary.Name)
                .Select(ToDto)
                .ToList(),
            Replays = session.Replays
                .Where(sessionReplay => sessionReplay.Replay is not null)
                .OrderByDescending(sessionReplay => sessionReplay.Replay!.Gametime)
                .Select(ToDto)
                .ToList(),
        };
    }

    private static InHouseGameSessionPlayerSummaryDto ToDto(InHouseGameSessionPlayerSummary summary)
    {
        return new()
        {
            Name = summary.Name,
            ToonId = new ToonIdDto
            {
                Region = summary.ToonId.Region,
                Realm = summary.ToonId.Realm,
                Id = summary.ToonId.Id,
            },
            Games = summary.Games,
            Wins = summary.Wins,
            Observes = summary.Observes,
            Winrate = summary.Games == 0 ? 0 : (double)summary.Wins / summary.Games,
            RatingStart = summary.RatingStart,
            RatingEnd = summary.RatingEnd,
            RatingDelta = summary.RatingDelta,
            AverageGain = summary.AverageGain,
            PlayedLatestGame = summary.PlayedLatestGame,
            ObservedLatestGame = summary.ObservedLatestGame,
            RatingsPending = summary.RatingsPending,
        };
    }

    private static InHouseGameSessionReplayDto ToDto(InHouseGameSessionReplay sessionReplay)
    {
        var replay = sessionReplay.Replay
            ?? throw new InvalidOperationException("Session replay is missing replay data.");
        var rating = GetBestRating(replay);

        return new()
        {
            ReplayHash = replay.ReplayHash,
            Gametime = replay.Gametime,
            GameMode = replay.GameMode,
            Duration = replay.Duration,
            WinnerTeam = replay.WinnerTeam,
            CommandersTeam1 = replay.Players
                .Where(player => player.TeamId == 1)
                .OrderBy(player => player.GamePos)
                .Select(player => player.Race)
                .ToList(),
            CommandersTeam2 = replay.Players
                .Where(player => player.TeamId == 2)
                .OrderBy(player => player.GamePos)
                .Select(player => player.Race)
                .ToList(),
            ExpectedWinProbability = rating?.ExpectedWinProbability,
            AvgRating = rating?.AvgRating,
            RatingsPending = rating is null,
        };
    }

    private static ReplayRating? GetBestRating(Replay replay)
    {
        var preferred = GetPreferredRatingType(replay);
        var fallback = GetFallbackRatingType(preferred);

        return replay.Ratings.FirstOrDefault(rating => rating.RatingType == preferred)
            ?? replay.Ratings.FirstOrDefault(rating => rating.RatingType == fallback)
            ?? replay.Ratings.FirstOrDefault();
    }

    private static RatingType GetPreferredRatingType(Replay replay)
    {
        var isCommander = Data.IsCommanderGameMode(replay.GameMode);
        return isCommander
            ? replay.TE ? RatingType.CommandersTE : RatingType.Commanders
            : replay.TE ? RatingType.StandardTE : RatingType.Standard;
    }

    private static RatingType GetFallbackRatingType(RatingType ratingType)
    {
        return ratingType switch
        {
            RatingType.CommandersTE => RatingType.Commanders,
            RatingType.StandardTE => RatingType.Standard,
            RatingType.Commanders => RatingType.CommandersTE,
            RatingType.Standard => RatingType.StandardTE,
            _ => RatingType.All,
        };
    }

    private static string NormalizeSessionName(string requestedName, string displayName)
    {
        var name = string.IsNullOrWhiteSpace(requestedName)
            ? $"{displayName}'s InHouse"
            : requestedName.Trim();
        return name.Length <= 80 ? name : name[..80];
    }

    private static bool HasValidToonId(InHouseReplayObserverDto observer)
        => observer.ToonId is { Region: > 0, Realm: > 0, Id: > 0 };

    private static ToonId CloneToonId(ToonId toonId)
        => new()
        {
            Region = toonId.Region,
            Realm = toonId.Realm,
            Id = toonId.Id,
        };

    private static ToonId ToEntity(ToonIdDto toonId)
        => new()
        {
            Region = toonId.Region,
            Realm = toonId.Realm,
            Id = toonId.Id,
        };

    private sealed class SummaryBuilder(InHouseGameSessionReplayPlayer participant)
    {
        public int? PlayerId { get; set; } = participant.PlayerId;
        public string Name { get; set; } = participant.Name;
        public ToonId ToonId { get; } = CloneToonId(participant.ToonId);
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Observes { get; set; }
        public double? RatingStart { get; set; }
        public double? RatingEnd { get; set; }
        public double RatingDelta { get; set; }
        public int RatingGames { get; set; }
        public bool PlayedLatestGame { get; set; }
        public bool ObservedLatestGame { get; set; }
        public bool RatingsPending { get; set; }

        public InHouseGameSessionPlayerSummary ToEntity(int sessionId)
        {
            return new()
            {
                InHouseGameSessionId = sessionId,
                PlayerId = PlayerId,
                Name = Name,
                ToonId = CloneToonId(ToonId),
                Games = Games,
                Wins = Wins,
                Observes = Observes,
                RatingStart = RatingStart,
                RatingEnd = RatingEnd,
                RatingDelta = RatingGames == 0 ? null : RatingDelta,
                AverageGain = RatingGames == 0 ? null : RatingDelta / RatingGames,
                PlayedLatestGame = PlayedLatestGame,
                ObservedLatestGame = ObservedLatestGame,
                RatingsPending = RatingsPending,
            };
        }
    }

    private readonly record struct ToonKey(int Region, int Realm, int Id);
}
