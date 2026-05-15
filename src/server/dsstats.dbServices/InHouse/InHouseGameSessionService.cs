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

        await EnsureSessionDerivedStateAsync(session.InHouseGameSessionId, token);
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
            .Include(replay => replay.Ratings)
                .ThenInclude(rating => rating.ReplayPlayerRatings)
            .FirstOrDefaultAsync(replay => replay.ReplayHash == replayHash, token)
            ?? throw new InvalidOperationException("The uploaded replay could not be imported.");

        var replayCountBeforeUpload = await context.InHouseGameSessionReplays
            .CountAsync(sessionReplay => sessionReplay.InHouseGameSessionId == session.InHouseGameSessionId, token);
        var existing = await context.InHouseGameSessionReplays
            .AnyAsync(sessionReplay => sessionReplay.InHouseGameSessionId == session.InHouseGameSessionId
                && sessionReplay.ReplayId == replay.ReplayId, token);

        if (!existing)
        {
            var players = CreateReplayPlayers(replay, request.Observers);
            var sessionReplay = new InHouseGameSessionReplay
            {
                InHouseGameSessionId = session.InHouseGameSessionId,
                ReplayId = replay.ReplayId,
                UploadedByInHouseUserId = userId,
                UploadedAt = DateTime.UtcNow,
                Players = players,
            };

            context.InHouseGameSessionReplays.Add(sessionReplay);
            await UpsertRosterPlayersFromReplayAsync(session.InHouseGameSessionId, userId, replay, players, replayCountBeforeUpload, token);
            await context.SaveChangesAsync(token);
        }

        await RefreshSummariesAsync(session.InHouseGameSessionId, token);
        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto> AddRosterPlayerAsync(
        Guid sessionId,
        int userId,
        InHouseRosterPlayerUpsertRequest request,
        CancellationToken token)
    {
        var session = await GetMutableSessionAsync(sessionId, token);
        var toonId = ValidateToonId(request.ToonId);
        var name = NormalizeRosterPlayerName(request.Name);
        var existing = await GetRosterPlayerByToonIdAsync(session.InHouseGameSessionId, toonId, token);
        var now = DateTime.UtcNow;
        var replayCount = await context.InHouseGameSessionReplays
            .CountAsync(replay => replay.InHouseGameSessionId == session.InHouseGameSessionId, token);

        if (existing is null)
        {
            context.InHouseGameSessionRosterPlayers.Add(new()
            {
                PublicId = Guid.NewGuid(),
                InHouseGameSessionId = session.InHouseGameSessionId,
                PlayerId = request.PlayerId,
                Name = name,
                ToonId = ToEntity(toonId),
                InitialRating = request.InitialRating ?? 1000,
                JoinedReplayCount = replayCount,
                IsSitter = request.IsSitter,
                IsManual = true,
                AddSource = "manual",
                AddedByInHouseUserId = userId,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.PlayerId ??= request.PlayerId;
            existing.Name = name;
            existing.InitialRating = request.InitialRating ?? existing.InitialRating;
            existing.IsSitter = request.IsSitter;
            existing.IsManual = true;
            existing.AddSource = "manual";
            existing.AddedByInHouseUserId ??= userId;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(token);
        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto> UpdateRosterPlayerAsync(
        Guid sessionId,
        Guid rosterPlayerId,
        int userId,
        InHouseRosterPlayerUpsertRequest request,
        CancellationToken token)
    {
        var session = await GetMutableSessionAsync(sessionId, token);
        var rosterPlayer = await GetRosterPlayerAsync(session.InHouseGameSessionId, rosterPlayerId, token);
        var toonId = ValidateToonId(request.ToonId);
        var conflict = await GetRosterPlayerByToonIdAsync(session.InHouseGameSessionId, toonId, token);
        if (conflict is not null && conflict.InHouseGameSessionRosterPlayerId != rosterPlayer.InHouseGameSessionRosterPlayerId)
        {
            throw new InvalidOperationException("That player is already on the session roster.");
        }

        rosterPlayer.Name = NormalizeRosterPlayerName(request.Name);
        rosterPlayer.ToonId = ToEntity(toonId);
        rosterPlayer.PlayerId = request.PlayerId;
        rosterPlayer.InitialRating = request.InitialRating ?? 1000;
        rosterPlayer.IsSitter = request.IsSitter;
        rosterPlayer.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(token);
        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto> SetRosterPlayerSitterAsync(
        Guid sessionId,
        Guid rosterPlayerId,
        int userId,
        bool isSitter,
        CancellationToken token)
    {
        var session = await GetMutableSessionAsync(sessionId, token);
        var rosterPlayer = await GetRosterPlayerAsync(session.InHouseGameSessionId, rosterPlayerId, token);
        rosterPlayer.IsSitter = isSitter;
        rosterPlayer.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(token);
        return await LoadDetailAsync(sessionId, userId, token)
            ?? throw new InvalidOperationException("InHouse session could not be loaded.");
    }

    public async Task<InHouseGameSessionDetailDto> RemoveRosterPlayerAsync(
        Guid sessionId,
        Guid rosterPlayerId,
        int userId,
        CancellationToken token)
    {
        var session = await GetMutableSessionAsync(sessionId, token);
        var rosterPlayer = await GetRosterPlayerAsync(session.InHouseGameSessionId, rosterPlayerId, token);
        context.InHouseGameSessionRosterPlayers.Remove(rosterPlayer);

        await context.SaveChangesAsync(token);
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

    private async Task UpsertRosterPlayersFromReplayAsync(
        int sessionId,
        int userId,
        Replay replay,
        IReadOnlyCollection<InHouseGameSessionReplayPlayer> participants,
        int joinedReplayCount,
        CancellationToken token)
    {
        var existing = await context.InHouseGameSessionRosterPlayers
            .Where(player => player.InHouseGameSessionId == sessionId)
            .ToListAsync(token);
        var existingByToon = existing.ToDictionary(player => new ToonKey(player.ToonId.Region, player.ToonId.Realm, player.ToonId.Id));
        var rating = GetBestRating(replay);
        var now = DateTime.UtcNow;

        foreach (var participant in participants)
        {
            var key = new ToonKey(participant.ToonId.Region, participant.ToonId.Realm, participant.ToonId.Id);
            if (existingByToon.TryGetValue(key, out var rosterPlayer))
            {
                rosterPlayer.Name = participant.Name;
                rosterPlayer.PlayerId ??= participant.PlayerId;
                rosterPlayer.UpdatedAt = now;
                continue;
            }

            var playerRating = rating?.ReplayPlayerRatings
                .FirstOrDefault(playerRating => playerRating.ReplayPlayerId == participant.ReplayPlayerId
                    || playerRating.PlayerId == participant.PlayerId);
            context.InHouseGameSessionRosterPlayers.Add(new()
            {
                PublicId = Guid.NewGuid(),
                InHouseGameSessionId = sessionId,
                PlayerId = participant.PlayerId,
                Name = participant.Name,
                ToonId = CloneToonId(participant.ToonId),
                InitialRating = playerRating?.RatingBefore ?? 1000,
                JoinedReplayCount = joinedReplayCount,
                IsSitter = false,
                IsManual = false,
                AddSource = participant.Observer ? "observer" : "replay",
                AddedByInHouseUserId = userId,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    private async Task<InHouseGameSession> GetMutableSessionAsync(Guid sessionId, CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .FirstOrDefaultAsync(session => session.PublicId == sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");

        if (session.ClosedAt is not null)
        {
            throw new InvalidOperationException("This InHouse session is closed.");
        }

        return session;
    }

    private async Task<InHouseGameSessionRosterPlayer> GetRosterPlayerAsync(
        int sessionId,
        Guid rosterPlayerId,
        CancellationToken token)
        => await context.InHouseGameSessionRosterPlayers
            .FirstOrDefaultAsync(player => player.InHouseGameSessionId == sessionId
                && player.PublicId == rosterPlayerId, token)
            ?? throw new InvalidOperationException("Unknown roster player.");

    private async Task<InHouseGameSessionRosterPlayer?> GetRosterPlayerByToonIdAsync(
        int sessionId,
        ToonIdDto toonId,
        CancellationToken token)
        => await context.InHouseGameSessionRosterPlayers
            .FirstOrDefaultAsync(player => player.InHouseGameSessionId == sessionId
                && player.ToonId.Region == toonId.Region
                && player.ToonId.Realm == toonId.Realm
                && player.ToonId.Id == toonId.Id, token);

    private async Task EnsureSessionDerivedStateAsync(int sessionId, CancellationToken token)
    {
        if (await NeedsSummaryRefreshAsync(sessionId, token))
        {
            await RefreshSummariesAsync(sessionId, token);
            return;
        }

        await EnsureRosterPlayersFromExistingSummariesAsync(sessionId, token);
    }

    private async Task<bool> NeedsSummaryRefreshAsync(int sessionId, CancellationToken token)
    {
        var replayCount = await context.InHouseGameSessionReplays
            .CountAsync(replay => replay.InHouseGameSessionId == sessionId, token);
        var summaryCount = await context.InHouseGameSessionPlayerSummaries
            .CountAsync(summary => summary.InHouseGameSessionId == sessionId, token);

        if (replayCount == 0)
        {
            return summaryCount > 0;
        }

        if (summaryCount == 0)
        {
            return true;
        }

        var distinctParticipantCount = await context.InHouseGameSessionReplayPlayers
            .Where(player => player.SessionReplay!.InHouseGameSessionId == sessionId)
            .Select(player => new
            {
                player.ToonId.Region,
                player.ToonId.Realm,
                player.ToonId.Id,
            })
            .Distinct()
            .CountAsync(token);
        if (distinctParticipantCount != summaryCount)
        {
            return true;
        }

        var hasPendingRatings = await context.InHouseGameSessionPlayerSummaries
            .AnyAsync(summary => summary.InHouseGameSessionId == sessionId && summary.RatingsPending, token);
        if (!hasPendingRatings)
        {
            return false;
        }

        return await context.InHouseGameSessionReplays
            .Where(sessionReplay => sessionReplay.InHouseGameSessionId == sessionId)
            .AnyAsync(sessionReplay => sessionReplay.Replay!.Ratings.Any(), token);
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

        var summaryEntities = summaries.Values.Select(summary => summary.ToEntity(sessionId)).ToList();
        context.InHouseGameSessionPlayerSummaries.AddRange(summaryEntities);
        await context.SaveChangesAsync(token);
        await EnsureRosterPlayersFromSummariesAsync(sessionId, summaryEntities, sessionReplays.Count, token);
    }

    private async Task EnsureRosterPlayersFromSummariesAsync(
        int sessionId,
        IReadOnlyCollection<InHouseGameSessionPlayerSummary> summaries,
        int replayCount,
        CancellationToken token)
    {
        if (summaries.Count == 0)
        {
            return;
        }

        var existing = await context.InHouseGameSessionRosterPlayers
            .Where(player => player.InHouseGameSessionId == sessionId)
            .ToListAsync(token);
        var existingByToon = existing.ToDictionary(player => new ToonKey(player.ToonId.Region, player.ToonId.Realm, player.ToonId.Id));
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var summary in summaries)
        {
            var key = new ToonKey(summary.ToonId.Region, summary.ToonId.Realm, summary.ToonId.Id);
            if (existingByToon.TryGetValue(key, out var rosterPlayer))
            {
                var rowChanged = false;
                if (rosterPlayer.Name != summary.Name)
                {
                    rosterPlayer.Name = summary.Name;
                    rowChanged = true;
                }
                if (rosterPlayer.PlayerId is null && summary.PlayerId is not null)
                {
                    rosterPlayer.PlayerId = summary.PlayerId;
                    rowChanged = true;
                }
                if (summary.RatingStart is not null
                    && Math.Abs(rosterPlayer.InitialRating - summary.RatingStart.Value) > 0.001
                    && !rosterPlayer.IsManual)
                {
                    rosterPlayer.InitialRating = summary.RatingStart.Value;
                    rowChanged = true;
                }
                if (rowChanged)
                {
                    rosterPlayer.UpdatedAt = now;
                    changed = true;
                }
                continue;
            }

            context.InHouseGameSessionRosterPlayers.Add(new()
            {
                PublicId = Guid.NewGuid(),
                InHouseGameSessionId = sessionId,
                PlayerId = summary.PlayerId,
                Name = summary.Name,
                ToonId = CloneToonId(summary.ToonId),
                InitialRating = summary.RatingStart ?? 1000,
                JoinedReplayCount = Math.Max(0, replayCount - summary.Games - summary.Observes),
                IsSitter = false,
                IsManual = false,
                AddSource = "summary",
                CreatedAt = now,
                UpdatedAt = now,
            });
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync(token);
        }
    }

    private async Task EnsureRosterPlayersFromExistingSummariesAsync(int sessionId, CancellationToken token)
    {
        var summaries = await context.InHouseGameSessionPlayerSummaries
            .Where(summary => summary.InHouseGameSessionId == sessionId)
            .ToListAsync(token);
        if (summaries.Count == 0)
        {
            return;
        }

        var rosterToons = await context.InHouseGameSessionRosterPlayers
            .Where(player => player.InHouseGameSessionId == sessionId)
            .Select(player => new
            {
                player.ToonId.Region,
                player.ToonId.Realm,
                player.ToonId.Id,
            })
            .ToListAsync(token);
        var rosterKeys = rosterToons.Select(toon => new ToonKey(toon.Region, toon.Realm, toon.Id)).ToHashSet();
        var hasMissingSummaryPlayer = summaries
            .Any(summary => !rosterKeys.Contains(new ToonKey(summary.ToonId.Region, summary.ToonId.Realm, summary.ToonId.Id)));
        if (!hasMissingSummaryPlayer)
        {
            return;
        }

        var replayCount = await context.InHouseGameSessionReplays
            .CountAsync(replay => replay.InHouseGameSessionId == sessionId, token);
        await EnsureRosterPlayersFromSummariesAsync(sessionId, summaries, replayCount, token);
    }

    private async Task<InHouseGameSessionDetailDto?> LoadDetailAsync(Guid sessionId, int userId, CancellationToken token)
    {
        var session = await context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Include(session => session.RosterPlayers)
            .Include(session => session.PlayerSummaries)
            .Include(session => session.Replays)
                .ThenInclude(sessionReplay => sessionReplay.Players)
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
            RosterPlayers = session.RosterPlayers
                .OrderBy(player => player.IsSitter)
                .ThenBy(player => player.Name)
                .Select(player => ToDto(player, session.PlayerSummaries))
                .ToList(),
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

    private static InHouseRosterPlayerDto ToDto(
        InHouseGameSessionRosterPlayer rosterPlayer,
        IEnumerable<InHouseGameSessionPlayerSummary> summaries)
    {
        var summary = summaries.FirstOrDefault(summary =>
            summary.ToonId.Region == rosterPlayer.ToonId.Region
            && summary.ToonId.Realm == rosterPlayer.ToonId.Realm
            && summary.ToonId.Id == rosterPlayer.ToonId.Id);

        return new()
        {
            RosterPlayerId = rosterPlayer.PublicId,
            Name = rosterPlayer.Name,
            ToonId = new ToonIdDto
            {
                Region = rosterPlayer.ToonId.Region,
                Realm = rosterPlayer.ToonId.Realm,
                Id = rosterPlayer.ToonId.Id,
            },
            PlayerId = rosterPlayer.PlayerId,
            InitialRating = rosterPlayer.InitialRating,
            JoinedReplayCount = rosterPlayer.JoinedReplayCount,
            IsSitter = rosterPlayer.IsSitter,
            IsManual = rosterPlayer.IsManual,
            AddSource = rosterPlayer.AddSource,
            CreatedAt = rosterPlayer.CreatedAt,
            UpdatedAt = rosterPlayer.UpdatedAt,
            Games = summary?.Games ?? 0,
            Wins = summary?.Wins ?? 0,
            Observes = summary?.Observes ?? 0,
            Winrate = summary?.Games > 0 ? (double)summary.Wins / summary.Games : 0,
            PlayedLatestGame = summary?.PlayedLatestGame ?? false,
            ObservedLatestGame = summary?.ObservedLatestGame ?? false,
            RatingsPending = summary?.RatingsPending ?? false,
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
            Players = sessionReplay.Players
                .OrderBy(player => player.GamePos)
                .Select(player => new InHouseGameSessionReplayPlayerDto
                {
                    Name = player.Name,
                    ToonId = new ToonIdDto
                    {
                        Region = player.ToonId.Region,
                        Realm = player.ToonId.Realm,
                        Id = player.ToonId.Id,
                    },
                    Observer = player.Observer,
                    TeamId = player.TeamId,
                    GamePos = player.GamePos,
                })
                .ToList(),
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

    private static string NormalizeRosterPlayerName(string requestedName)
    {
        var name = string.IsNullOrWhiteSpace(requestedName)
            ? throw new InvalidOperationException("Roster player name is required.")
            : requestedName.Trim();
        return name.Length <= 80 ? name : name[..80];
    }

    private static ToonIdDto ValidateToonId(ToonIdDto toonId)
    {
        if (toonId is not { Region: > 0, Realm: > 0, Id: > 0 })
        {
            throw new InvalidOperationException("Roster player ToonId is required.");
        }

        return new()
        {
            Region = toonId.Region,
            Realm = toonId.Realm,
            Id = toonId.Id,
        };
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
