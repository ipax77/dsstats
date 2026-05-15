using System.Collections.Concurrent;
using System.Text.Json;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.InHouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices.InHouse;

public sealed class InHouseGameSessionService(
    IServiceScopeFactory scopeFactory,
    IImportService importService) : IInHouseGameSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<Guid, InHouseRuntimeSession> sessions = [];
    private readonly SemaphoreSlim createSessionGate = new(1, 1);

    public async Task<List<InHouseGameSessionListDto>> GetActiveSessionsAsync(CancellationToken token)
    {
        await LoadActiveSessionsAsync(token);
        return sessions.Values
            .Select(session => session.State)
            .Where(state => state.ClosedAt is null)
            .OrderByDescending(state => state.CreatedAt)
            .Select(state => state.ToListDto())
            .ToList();
    }

    public async Task<InHouseClosedGameSessionsPageDto> GetClosedSessionsAsync(
        InHouseClosedGameSessionsRequest request,
        CancellationToken token)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var skip = (page - 1) * pageSize;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var query = context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Where(session => session.ClosedAt != null);

        var total = await query.CountAsync(token);
        var items = await query
            .OrderByDescending(session => session.ClosedAt)
            .ThenByDescending(session => session.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(token);

        return new()
        {
            Items = items.Select(ToClosedListDto).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }

    public async Task<InHouseGameSessionDetailDto> CreateSessionAsync(
        int userId,
        InHouseCreateGameSessionRequest request,
        CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var user = await context.InHouseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        await createSessionGate.WaitAsync(token);
        try
        {
            var existingEmptySession = await GetExistingEmptyActiveSessionAsync(context, userId, token);
            if (existingEmptySession is not null)
            {
                var existingState = RestoreState(existingEmptySession);
                var runtimeSession = sessions.GetOrAdd(existingState.SessionId, _ => new InHouseRuntimeSession(existingState));
                return runtimeSession.State.ToDetailDto(userId);
            }

            var now = DateTime.UtcNow;
            var session = new InHouseGameSessionSimplified
            {
                PublicId = Guid.NewGuid(),
                Name = NormalizeSessionName(request.Name, user.DisplayName),
                CreatedByInHouseUserId = userId,
                CreatedAt = now,
            };

            context.InHouseGameSessions.Add(session);
            await context.SaveChangesAsync(token);

            var state = InHouseSessionState.Create(session, user.PublicId, user.DisplayName, now);
            context.InHouseGameSessionStateSnapshots.Add(new()
            {
                InHouseGameSessionId = session.InHouseGameSessionId,
                Json = state.ToJson(),
                CreatedAt = now,
                UpdatedAt = now,
            });
            await context.SaveChangesAsync(token);

            sessions[state.SessionId] = new InHouseRuntimeSession(state);
            return state.ToDetailDto(userId);
        }
        finally
        {
            createSessionGate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto?> GetSessionAsync(Guid sessionId, int userId, CancellationToken token)
    {
        var session = await LoadSessionAsync(sessionId, token);
        if (session is null)
        {
            return null;
        }

        await RefreshPendingRatingsAsync(session, token);
        return session.State.ToDetailDto(userId);
    }

    public async Task<InHouseGameSessionMutationResult> UploadReplayAsync(
        Guid sessionId,
        int userId,
        InHouseReplayUploadRequest request,
        CancellationToken token)
    {
        if (request.Replay.Players.Count == 0)
        {
            throw new InvalidOperationException("The uploaded replay has no players.");
        }

        var session = await LoadSessionAsync(sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");
        var replayHash = request.Replay.ComputeHash();
        var compatHash = request.Replay.ComputeCandidateHash();
        var fingerprint = InHouseReplayFingerprint.FromReplay(request.Replay);

        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            if (session.State.IsDuplicate(replayHash, compatHash, fingerprint))
            {
                return new(session.State.ToDetailDto(userId), false);
            }
        }
        finally
        {
            session.Gate.Release();
        }

        await importService.InsertReplays([request.Replay]);
        var replay = await LoadReplayAsync(replayHash, token)
            ?? throw new InvalidOperationException("The uploaded replay could not be imported.");

        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            if (session.State.IsDuplicate(replay.ReplayHash, replay.CompatHash, fingerprint))
            {
                return new(session.State.ToDetailDto(userId), false);
            }

            var observerPlayers = await GetObserverPlayersAsync(request.Observers, token);
            session.State.AddReplay(replay, request.Observers, observerPlayers, fingerprint);
            await RefreshPendingRatingsCoreAsync(session.State, token);
            await PersistStateAsync(session.State, replay.ReplayId, observerPlayers.Select(player => player.PlayerId).ToArray(), token);
            return new(session.State.ToDetailDto(userId), true);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto> RemoveReplayAsync(
        Guid sessionId,
        string replayHash,
        int userId,
        bool isAdmin,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(replayHash))
        {
            throw new InvalidOperationException("Replay hash is required.");
        }

        var session = await LoadSessionAsync(sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");
        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            if (session.State.CreatedByInHouseUserId != userId && !isAdmin)
            {
                throw new InvalidOperationException("Only the session creator or an InHouse admin can delete replays from this session.");
            }

            var remainingReplayIds = session.State.GetReplayIdsAfterRemoving(replayHash);
            var remainingReplays = remainingReplayIds.Count == 0
                ? []
                : await LoadReplaysAsync(remainingReplayIds, token);
            session.State.RemoveReplay(replayHash, remainingReplayIds, remainingReplays);
            await PersistStateAsync(session.State, replayId: null, observerPlayerIds: null, token);
            return session.State.ToDetailDto(userId, isAdmin);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto> AddRosterPlayerAsync(
        Guid sessionId,
        int userId,
        InHouseRosterPlayerUpsertRequest request,
        CancellationToken token)
    {
        var session = await GetMutableRuntimeSessionAsync(sessionId, token);
        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            session.State.AddOrUpdateRosterPlayer(request);
            return session.State.ToDetailDto(userId);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto> SetRosterPlayerSitterAsync(
        Guid sessionId,
        Guid rosterPlayerId,
        int userId,
        bool isSitter,
        CancellationToken token)
    {
        var session = await GetMutableRuntimeSessionAsync(sessionId, token);
        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            session.State.SetSitter(rosterPlayerId, isSitter);
            return session.State.ToDetailDto(userId);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto> RemoveRosterPlayerAsync(
        Guid sessionId,
        Guid rosterPlayerId,
        int userId,
        CancellationToken token)
    {
        var session = await GetMutableRuntimeSessionAsync(sessionId, token);
        await session.Gate.WaitAsync(token);
        try
        {
            session.State.ThrowIfClosed();
            session.State.RemoveRosterPlayer(rosterPlayerId);
            return session.State.ToDetailDto(userId);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<InHouseGameSessionDetailDto> CloseSessionAsync(Guid sessionId, int userId, bool isAdmin, CancellationToken token)
    {
        var session = await LoadSessionAsync(sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");
        await session.Gate.WaitAsync(token);
        try
        {
            if (session.State.CreatedByInHouseUserId != userId && !isAdmin)
            {
                throw new InvalidOperationException("Only the session creator or an InHouse admin can close this session.");
            }

            session.State.Close();
            await PersistStateAsync(session.State, replayId: null, observerPlayerIds: null, token);
            sessions.TryRemove(session.State.SessionId, out _);
            return session.State.ToDetailDto(userId, isAdmin);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task DeleteSessionAsync(Guid sessionId, int userId, bool isAdmin, CancellationToken token)
    {
        if (!isAdmin)
        {
            throw new InvalidOperationException("Only InHouse admins can delete game sessions.");
        }

        if (sessions.TryGetValue(sessionId, out var runtimeSession))
        {
            await runtimeSession.Gate.WaitAsync(token);
            try
            {
                if (!await DeleteSessionRowAsync(sessionId, token))
                {
                    throw new KeyNotFoundException("Unknown InHouse session.");
                }

                sessions.TryRemove(sessionId, out _);
                return;
            }
            finally
            {
                runtimeSession.Gate.Release();
            }
        }

        if (!await DeleteSessionRowAsync(sessionId, token))
        {
            throw new KeyNotFoundException("Unknown InHouse session.");
        }
    }

    public async Task<List<InHouseGameSessionDetailDto>> CloseInactiveSessionsAsync(TimeSpan inactiveFor, CancellationToken token)
    {
        await LoadActiveSessionsAsync(token);
        var cutoff = DateTime.UtcNow - inactiveFor;
        List<InHouseGameSessionDetailDto> closed = [];

        foreach (var session in sessions.Values.Where(session => session.State is { ClosedAt: null } && session.State.LastActivityAt < cutoff))
        {
            await session.Gate.WaitAsync(token);
            try
            {
                if (session.State.ClosedAt is not null || session.State.LastActivityAt >= cutoff)
                {
                    continue;
                }

                session.State.Close();
                await PersistStateAsync(session.State, replayId: null, observerPlayerIds: null, token);
                closed.Add(session.State.ToDetailDto(session.State.CreatedByInHouseUserId));
                sessions.TryRemove(session.State.SessionId, out _);
            }
            finally
            {
                session.Gate.Release();
            }
        }

        return closed;
    }

    private async Task<bool> DeleteSessionRowAsync(Guid sessionId, CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var deleted = await context.InHouseGameSessions
            .Where(session => session.PublicId == sessionId)
            .ExecuteDeleteAsync(token);
        return deleted > 0;
    }

    private async Task<InHouseRuntimeSession> GetMutableRuntimeSessionAsync(Guid sessionId, CancellationToken token)
    {
        var session = await LoadSessionAsync(sessionId, token)
            ?? throw new InvalidOperationException("Unknown InHouse session.");
        session.State.ThrowIfClosed();
        return session;
    }

    private async Task LoadActiveSessionsAsync(CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var active = await context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Include(session => session.StateSnapshot)
            .Where(session => session.ClosedAt == null)
            .ToListAsync(token);

        foreach (var session in active)
        {
            sessions.GetOrAdd(session.PublicId, _ => new InHouseRuntimeSession(RestoreState(session)));
        }
    }

    private async Task<InHouseRuntimeSession?> LoadSessionAsync(Guid sessionId, CancellationToken token)
    {
        if (sessions.TryGetValue(sessionId, out var runtimeSession))
        {
            return runtimeSession;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var session = await context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Include(session => session.StateSnapshot)
            .FirstOrDefaultAsync(session => session.PublicId == sessionId, token);
        if (session is null)
        {
            return null;
        }

        var restored = new InHouseRuntimeSession(RestoreState(session));
        if (restored.State.ClosedAt is not null)
        {
            return restored;
        }

        return sessions.GetOrAdd(sessionId, restored);
    }

    private static async Task<InHouseGameSessionSimplified?> GetExistingEmptyActiveSessionAsync(
        DsstatsContext context,
        int userId,
        CancellationToken token)
    {
        var activeSessions = await context.InHouseGameSessions
            .AsNoTracking()
            .Include(session => session.CreatedBy)
            .Include(session => session.StateSnapshot)
            .Where(session => session.CreatedByInHouseUserId == userId && session.ClosedAt == null)
            .OrderByDescending(session => session.CreatedAt)
            .ToListAsync(token);

        return activeSessions.FirstOrDefault(session =>
        {
            var state = RestoreState(session);
            return state is { ClosedAt: null }
                && state.ReplayIds.Count == 0
                && state.Replays.Count == 0;
        });
    }

    private async Task PersistStateAsync(
        InHouseSessionState state,
        int? replayId,
        int[]? observerPlayerIds,
        CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var session = await context.InHouseGameSessions
            .Include(session => session.StateSnapshot)
            .FirstAsync(session => session.InHouseGameSessionId == state.InHouseGameSessionId, token);
        var now = DateTime.UtcNow;

        session.ReplayIds = state.ReplayIds.ToArray();
        session.ClosedAt = state.ClosedAt;
        if (session.StateSnapshot is null)
        {
            session.StateSnapshot = new()
            {
                InHouseGameSessionId = session.InHouseGameSessionId,
                CreatedAt = now,
            };
        }

        session.StateSnapshot.Json = state.ToJson();
        session.StateSnapshot.UpdatedAt = now;

        if (replayId is not null && observerPlayerIds is { Length: > 0 })
        {
            var existing = await context.ReplayObservers
                .FirstOrDefaultAsync(observer => observer.ReplayId == replayId.Value, token);
            if (existing is null)
            {
                context.ReplayObservers.Add(new()
                {
                    ReplayId = replayId.Value,
                    PlayerIds = observerPlayerIds,
                });
            }
            else
            {
                existing.PlayerIds = observerPlayerIds;
            }
        }

        await context.SaveChangesAsync(token);
    }

    private async Task<List<ObserverPlayerState>> GetObserverPlayersAsync(
        IReadOnlyCollection<InHouseReplayObserverDto> observers,
        CancellationToken token)
    {
        var validObservers = observers.Where(HasValidToonId).ToList();
        if (validObservers.Count == 0)
        {
            return [];
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        List<ObserverPlayerState> players = new(validObservers.Count);
        foreach (var observer in validObservers)
        {
            token.ThrowIfCancellationRequested();
            var playerId = importService.GetOrCreatePlayerId(
                observer.Name,
                observer.ToonId.Region,
                observer.ToonId.Realm,
                observer.ToonId.Id,
                context);
            players.Add(new(playerId, observer.ToonId.Region, observer.ToonId.Realm, observer.ToonId.Id));
        }

        return players;
    }

    private async Task RefreshPendingRatingsAsync(InHouseRuntimeSession session, CancellationToken token)
    {
        if (!session.State.HasPendingRatings)
        {
            return;
        }

        await session.Gate.WaitAsync(token);
        try
        {
            if (session.State.HasPendingRatings && await RefreshPendingRatingsCoreAsync(session.State, token))
            {
                session.State.Touch();
            }
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private async Task<bool> RefreshPendingRatingsCoreAsync(InHouseSessionState state, CancellationToken token)
    {
        if (!state.HasPendingRatings || state.ReplayIds.Count == 0)
        {
            return false;
        }

        var replays = await LoadReplaysAsync(state.ReplayIds, token);
        return state.RefreshRatings(replays);
    }

    private async Task<ReplayRuntimeState?> LoadReplayAsync(string replayHash, CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        return await ProjectReplayQuery(context.Replays
                .AsNoTracking()
                .Where(replay => replay.ReplayHash == replayHash))
            .FirstOrDefaultAsync(token);
    }

    private async Task<List<ReplayRuntimeState>> LoadReplaysAsync(IReadOnlyCollection<int> replayIds, CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        return await ProjectReplayQuery(context.Replays
                .AsNoTracking()
                .Where(replay => replayIds.Contains(replay.ReplayId)))
            .ToListAsync(token);
    }

    private static IQueryable<ReplayRuntimeState> ProjectReplayQuery(IQueryable<Replay> query)
        => query.Select(replay => new ReplayRuntimeState
        {
            ReplayId = replay.ReplayId,
            ReplayHash = replay.ReplayHash,
            CompatHash = replay.CompatHash,
            Gametime = replay.Gametime,
            GameMode = replay.GameMode,
            TE = replay.TE,
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
            Players = replay.Players
                .OrderBy(player => player.GamePos)
                .Select(player => new ReplayPlayerRuntimeState
                {
                    ReplayPlayerId = player.ReplayPlayerId,
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    ToonId = new ToonIdDto
                    {
                        Region = player.Player!.ToonId.Region,
                        Realm = player.Player.ToonId.Realm,
                        Id = player.Player.ToonId.Id,
                    },
                    Race = player.Race,
                    TeamId = player.TeamId,
                    GamePos = player.GamePos,
                    Result = player.Result,
                })
                .ToList(),
            Ratings = replay.Ratings.Select(rating => new ReplayRatingRuntimeState
            {
                RatingType = rating.RatingType,
                ExpectedWinProbability = rating.ExpectedWinProbability,
                AvgRating = rating.AvgRating,
                PlayerRatings = rating.ReplayPlayerRatings.Select(playerRating => new ReplayPlayerRatingRuntimeState
                {
                    ReplayPlayerId = playerRating.ReplayPlayerId,
                    PlayerId = playerRating.PlayerId,
                    RatingBefore = playerRating.RatingBefore,
                    RatingDelta = playerRating.RatingDelta,
                }).ToList(),
            }).ToList(),
        });

    private static InHouseSessionState RestoreState(InHouseGameSessionSimplified session)
    {
        if (!string.IsNullOrWhiteSpace(session.StateSnapshot?.Json))
        {
            var state = JsonSerializer.Deserialize<InHouseSessionState>(session.StateSnapshot.Json, JsonOptions);
            if (state is not null)
            {
                state.SyncFromDb(session);
                return state;
            }
        }

        return InHouseSessionState.Create(
            session,
            session.CreatedBy?.PublicId ?? Guid.Empty,
            session.CreatedBy?.DisplayName ?? string.Empty,
            DateTime.UtcNow);
    }

    private static InHouseClosedGameSessionListDto ToClosedListDto(InHouseGameSessionSimplified session)
        => new()
        {
            SessionId = session.PublicId,
            Name = session.Name,
            CreatedByUserId = session.CreatedBy?.PublicId ?? Guid.Empty,
            CreatedByDisplayName = session.CreatedBy?.DisplayName ?? string.Empty,
            CreatedAt = session.CreatedAt,
            ClosedAt = session.ClosedAt ?? session.CreatedAt,
            Games = session.ReplayIds.Length,
        };

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

        return CloneToonId(toonId);
    }

    private static bool HasValidToonId(InHouseReplayObserverDto observer)
        => observer.ToonId is { Region: > 0, Realm: > 0, Id: > 0 };

    private static ToonIdDto CloneToonId(ToonIdDto toonId)
        => new()
        {
            Region = toonId.Region,
            Realm = toonId.Realm,
            Id = toonId.Id,
        };

    private sealed class InHouseRuntimeSession(InHouseSessionState state)
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public InHouseSessionState State { get; } = state;
    }

    private sealed class InHouseSessionState
    {
        public int InHouseGameSessionId { get; set; }
        public Guid SessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CreatedByInHouseUserId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByDisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public long Revision { get; set; } = 1;
        public List<int> ReplayIds { get; set; } = [];
        public List<string> ReplayHashes { get; set; } = [];
        public List<string> CompatHashes { get; set; } = [];
        public List<InHouseReplayFingerprint> ReplayFingerprints { get; set; } = [];
        public List<RosterPlayerState> RosterPlayers { get; set; } = [];
        public List<PlayerSummaryState> Players { get; set; } = [];
        public List<ReplayState> Replays { get; set; } = [];

        public bool HasPendingRatings => Replays.Any(replay => replay.RatingsPending) || Players.Any(player => player.RatingsPending);

        public static InHouseSessionState Create(
            InHouseGameSessionSimplified session,
            Guid createdByUserId,
            string createdByDisplayName,
            DateTime now)
            => new()
            {
                InHouseGameSessionId = session.InHouseGameSessionId,
                SessionId = session.PublicId,
                Name = session.Name,
                CreatedByInHouseUserId = session.CreatedByInHouseUserId,
                CreatedByUserId = createdByUserId,
                CreatedByDisplayName = createdByDisplayName,
                CreatedAt = session.CreatedAt,
                ClosedAt = session.ClosedAt,
                LastActivityAt = now,
                ReplayIds = session.ReplayIds.ToList(),
            };

        public void SyncFromDb(InHouseGameSessionSimplified session)
        {
            InHouseGameSessionId = session.InHouseGameSessionId;
            SessionId = session.PublicId;
            Name = session.Name;
            CreatedByInHouseUserId = session.CreatedByInHouseUserId;
            CreatedByUserId = session.CreatedBy?.PublicId ?? CreatedByUserId;
            CreatedByDisplayName = session.CreatedBy?.DisplayName ?? CreatedByDisplayName;
            CreatedAt = session.CreatedAt;
            ClosedAt = session.ClosedAt;
            ReplayIds = session.ReplayIds.ToList();
        }

        public void AddReplay(
            ReplayRuntimeState replay,
            IReadOnlyCollection<InHouseReplayObserverDto> observers,
            IReadOnlyCollection<ObserverPlayerState> observerPlayers,
            InHouseReplayFingerprint fingerprint)
        {
            var replayCountBefore = Replays.Count;
            var observerPlayerIds = observerPlayers.ToDictionary(player => new ToonKey(player.Region, player.Realm, player.Id), player => player.PlayerId);
            var participants = replay.Players
                .Select(player => ParticipantState.FromReplayPlayer(player, observer: false))
                .Concat(observers
                    .Where(HasValidToonId)
                    .Select(observer =>
                    {
                        var key = new ToonKey(observer.ToonId.Region, observer.ToonId.Realm, observer.ToonId.Id);
                        return ParticipantState.FromObserver(observer, observerPlayerIds.GetValueOrDefault(key));
                    }))
                .ToList();
            var rating = GetBestRating(replay);

            foreach (var participant in participants)
            {
                EnsureRosterPlayer(participant, rating, replayCountBefore);
            }

            foreach (var summary in Players)
            {
                summary.PlayedLatestGame = false;
                summary.ObservedLatestGame = false;
            }

            foreach (var participant in participants)
            {
                var summary = GetOrCreateSummary(participant);
                summary.Name = participant.Name;
                summary.PlayerId ??= participant.PlayerId;
                if (participant.Observer)
                {
                    summary.Observes++;
                    summary.ObservedLatestGame = true;
                    continue;
                }

                summary.Games++;
                summary.Wins += participant.Result == PlayerResult.Win ? 1 : 0;
                summary.PlayedLatestGame = true;
                var playerRating = GetParticipantRating(participant, rating);
                if (playerRating is null)
                {
                    summary.RatingsPending = true;
                    continue;
                }

                summary.RatingGames++;
                summary.RatingStart ??= playerRating.RatingBefore;
                summary.RatingEnd = playerRating.RatingBefore + playerRating.RatingDelta;
                summary.RatingDelta = (summary.RatingDelta ?? 0) + playerRating.RatingDelta;
                summary.AverageGain = summary.RatingDelta / summary.RatingGames;
            }

            ReplayIds.Add(replay.ReplayId);
            ReplayHashes.Add(replay.ReplayHash);
            if (!string.IsNullOrWhiteSpace(replay.CompatHash))
            {
                CompatHashes.Add(replay.CompatHash);
            }
            ReplayFingerprints.Add(fingerprint);
            Replays.Add(ReplayState.FromReplay(replay, participants, rating));
            Touch();
        }

        public bool RefreshRatings(List<ReplayRuntimeState> replays)
        {
            var replayById = replays.ToDictionary(replay => replay.ReplayId);
            var changed = false;
            foreach (var replayState in Replays.Where(replay => replay.RatingsPending))
            {
                var replayId = ReplayIds.ElementAtOrDefault(Replays.IndexOf(replayState));
                if (!replayById.TryGetValue(replayId, out var replay))
                {
                    continue;
                }

                var rating = GetBestRating(replay);
                if (rating is null)
                {
                    continue;
                }

                replayState.ExpectedWinProbability = rating.ExpectedWinProbability;
                replayState.AvgRating = rating.AvgRating;
                replayState.RatingsPending = false;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            RecalculateSummariesFromReplays(replays);
            return true;
        }

        public IReadOnlyList<int> GetReplayIdsAfterRemoving(string replayHash)
        {
            var replayIndex = Replays.FindIndex(replay => replay.ReplayHash == replayHash);
            if (replayIndex < 0)
            {
                throw new InvalidOperationException("This replay is not attached to the InHouse session.");
            }

            List<int> remainingReplayIds = new(Math.Max(0, ReplayIds.Count - 1));
            for (var i = 0; i < ReplayIds.Count; i++)
            {
                if (i != replayIndex)
                {
                    remainingReplayIds.Add(ReplayIds[i]);
                }
            }

            return remainingReplayIds;
        }

        public void RemoveReplay(
            string replayHash,
            IReadOnlyList<int> remainingReplayIds,
            IReadOnlyCollection<ReplayRuntimeState> remainingRuntimeReplays)
        {
            var replayIndex = Replays.FindIndex(replay => replay.ReplayHash == replayHash);
            if (replayIndex < 0)
            {
                throw new InvalidOperationException("This replay is not attached to the InHouse session.");
            }

            Replays.RemoveAt(replayIndex);
            ReplayIds = remainingReplayIds.ToList();
            ReplayHashes = Replays.Select(replay => replay.ReplayHash).ToList();
            CompatHashes = remainingRuntimeReplays
                .Select(replay => replay.CompatHash)
                .Where(hash => !string.IsNullOrWhiteSpace(hash))
                .ToList();
            if (replayIndex < ReplayFingerprints.Count)
            {
                ReplayFingerprints.RemoveAt(replayIndex);
            }

            RebuildDerivedState(remainingRuntimeReplays);
            Touch();
        }

        public void AddOrUpdateRosterPlayer(InHouseRosterPlayerUpsertRequest request)
        {
            var toonId = ValidateToonId(request.ToonId);
            var name = NormalizeRosterPlayerName(request.Name);
            var key = new ToonKey(toonId);
            var existing = RosterPlayers.FirstOrDefault(player => new ToonKey(player.ToonId) == key);
            if (existing is null)
            {
                RosterPlayers.Add(new()
                {
                    RosterPlayerId = Guid.NewGuid(),
                    Name = name,
                    ToonId = toonId,
                    PlayerId = request.PlayerId,
                    InitialRating = request.InitialRating ?? 1000,
                    JoinedReplayCount = Replays.Count,
                    IsSitter = request.IsSitter,
                    IsManual = true,
                    AddSource = "manual",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Name = name;
                existing.ToonId = toonId;
                existing.PlayerId ??= request.PlayerId;
                existing.InitialRating = request.InitialRating ?? existing.InitialRating;
                existing.IsSitter = request.IsSitter;
                existing.IsManual = true;
                existing.AddSource = "manual";
                existing.UpdatedAt = DateTime.UtcNow;
            }

            Touch();
        }

        public void SetSitter(Guid rosterPlayerId, bool isSitter)
        {
            var rosterPlayer = RosterPlayers.FirstOrDefault(player => player.RosterPlayerId == rosterPlayerId)
                ?? throw new InvalidOperationException("Unknown roster player.");
            rosterPlayer.IsSitter = isSitter;
            rosterPlayer.UpdatedAt = DateTime.UtcNow;
            Touch();
        }

        public void RemoveRosterPlayer(Guid rosterPlayerId)
        {
            var removed = RosterPlayers.RemoveAll(player => player.RosterPlayerId == rosterPlayerId);
            if (removed == 0)
            {
                throw new InvalidOperationException("Unknown roster player.");
            }

            Touch();
        }

        public void Close()
        {
            ClosedAt ??= DateTime.UtcNow;
            Touch();
        }

        public void Touch()
        {
            LastActivityAt = DateTime.UtcNow;
            Revision++;
        }

        public void ThrowIfClosed()
        {
            if (ClosedAt is not null)
            {
                throw new InvalidOperationException("This InHouse session is closed.");
            }
        }

        public bool IsDuplicate(string replayHash, string compatHash, InHouseReplayFingerprint fingerprint)
            => ReplayHashes.Contains(replayHash)
                || (!string.IsNullOrWhiteSpace(compatHash) && CompatHashes.Contains(compatHash))
                || ReplayFingerprints.Contains(fingerprint);

        public InHouseGameSessionListDto ToListDto()
            => new()
            {
                SessionId = SessionId,
                Revision = Revision,
                Name = Name,
                CreatedByUserId = CreatedByUserId,
                CreatedByDisplayName = CreatedByDisplayName,
                CreatedAt = CreatedAt,
                ClosedAt = ClosedAt,
                LastActivityAt = LastActivityAt,
                Games = Replays.Count,
                Players = Players.Count,
            };

        public InHouseGameSessionDetailDto ToDetailDto(int userId, bool isAdmin = false)
            => new()
            {
                SessionId = SessionId,
                Revision = Revision,
                Name = Name,
                CreatedByUserId = CreatedByUserId,
                CreatedByDisplayName = CreatedByDisplayName,
                CreatedAt = CreatedAt,
                ClosedAt = ClosedAt,
                LastActivityAt = LastActivityAt,
                CanClose = ClosedAt is null && (CreatedByInHouseUserId == userId || isAdmin),
                RosterPlayers = RosterPlayers
                    .OrderBy(player => player.IsSitter)
                    .ThenBy(player => player.Name)
                    .Select(ToDto)
                    .ToList(),
                Players = Players
                    .OrderByDescending(player => player.Games)
                    .ThenByDescending(player => player.RatingDelta ?? 0)
                    .ThenBy(player => player.Name)
                    .Select(player => player.ToDto())
                    .ToList(),
                Replays = Replays
                    .OrderByDescending(replay => replay.Gametime)
                    .Select(replay => replay.ToDto())
                    .ToList(),
            };

        public string ToJson()
            => JsonSerializer.Serialize(this, JsonOptions);

        private void EnsureRosterPlayer(ParticipantState participant, ReplayRatingRuntimeState? rating, int replayCountBefore)
        {
            var key = new ToonKey(participant.ToonId);
            var existing = RosterPlayers.FirstOrDefault(player => new ToonKey(player.ToonId) == key);
            if (existing is not null)
            {
                var existingPlayerRating = participant.Observer ? null : GetParticipantRating(participant, rating);
                if (!existing.IsManual
                    && existingPlayerRating is not null
                    && Math.Abs(existing.InitialRating - existingPlayerRating.RatingBefore) > 0.001)
                {
                    existing.InitialRating = existingPlayerRating.RatingBefore;
                }

                existing.Name = participant.Name;
                existing.PlayerId ??= participant.PlayerId;
                existing.UpdatedAt = DateTime.UtcNow;
                return;
            }

            var playerRating = participant.Observer ? null : GetParticipantRating(participant, rating);
            RosterPlayers.Add(new()
            {
                RosterPlayerId = Guid.NewGuid(),
                Name = participant.Name,
                ToonId = CloneToonId(participant.ToonId),
                PlayerId = participant.PlayerId,
                InitialRating = playerRating?.RatingBefore ?? 1000,
                JoinedReplayCount = replayCountBefore,
                AddSource = participant.Observer ? "observer" : "replay",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        private void EnsureRebuiltRosterPlayer(
            ParticipantState participant,
            ReplayRatingRuntimeState? rating,
            int replayCountBefore,
            IReadOnlyDictionary<ToonKey, RosterPlayerState> previousRosterByToon)
        {
            var key = new ToonKey(participant.ToonId);
            var existing = RosterPlayers.FirstOrDefault(player => new ToonKey(player.ToonId) == key);
            if (existing is null
                && previousRosterByToon.TryGetValue(key, out var previousRoster)
                && !previousRoster.IsManual)
            {
                existing = CloneRosterPlayer(previousRoster);
                RosterPlayers.Add(existing);
            }

            if (existing is not null)
            {
                var existingPlayerRating = participant.Observer ? null : GetParticipantRating(participant, rating);
                if (!existing.IsManual
                    && existingPlayerRating is not null
                    && Math.Abs(existing.InitialRating - existingPlayerRating.RatingBefore) > 0.001)
                {
                    existing.InitialRating = existingPlayerRating.RatingBefore;
                }

                existing.Name = participant.Name;
                existing.PlayerId ??= participant.PlayerId;
                existing.UpdatedAt = DateTime.UtcNow;
                return;
            }

            var playerRating = participant.Observer ? null : GetParticipantRating(participant, rating);
            RosterPlayers.Add(new()
            {
                RosterPlayerId = Guid.NewGuid(),
                Name = participant.Name,
                ToonId = CloneToonId(participant.ToonId),
                PlayerId = participant.PlayerId,
                InitialRating = playerRating?.RatingBefore ?? 1000,
                JoinedReplayCount = replayCountBefore,
                AddSource = participant.Observer ? "observer" : "replay",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        private void RebuildDerivedState(IReadOnlyCollection<ReplayRuntimeState> runtimeReplays)
        {
            var previousRosterByToon = new Dictionary<ToonKey, RosterPlayerState>(RosterPlayers.Count);
            foreach (var rosterPlayer in RosterPlayers)
            {
                previousRosterByToon.TryAdd(new ToonKey(rosterPlayer.ToonId), CloneRosterPlayer(rosterPlayer));
            }

            var manualRosterPlayers = RosterPlayers
                .Where(player => player.IsManual)
                .Select(CloneRosterPlayer)
                .ToList();
            RosterPlayers.Clear();
            RosterPlayers.AddRange(manualRosterPlayers);
            Players.Clear();

            var runtimeByHash = runtimeReplays.ToDictionary(replay => replay.ReplayHash);
            for (var replayIndex = 0; replayIndex < Replays.Count; replayIndex++)
            {
                var replay = Replays[replayIndex];
                runtimeByHash.TryGetValue(replay.ReplayHash, out var runtimeReplay);
                var rating = runtimeReplay is null ? null : GetBestRating(runtimeReplay);

                replay.ExpectedWinProbability = rating?.ExpectedWinProbability;
                replay.AvgRating = rating?.AvgRating;
                replay.RatingsPending = rating is null;

                foreach (var participant in replay.Players)
                {
                    EnsureRebuiltRosterPlayer(participant, rating, replayIndex, previousRosterByToon);
                }

                foreach (var summary in Players)
                {
                    summary.PlayedLatestGame = false;
                    summary.ObservedLatestGame = false;
                }

                foreach (var participant in replay.Players)
                {
                    var summary = GetOrCreateSummary(participant);
                    summary.Name = participant.Name;
                    summary.PlayerId ??= participant.PlayerId;
                    if (participant.Observer)
                    {
                        summary.Observes++;
                        summary.ObservedLatestGame = true;
                        continue;
                    }

                    summary.Games++;
                    summary.Wins += participant.Result == PlayerResult.Win ? 1 : 0;
                    summary.PlayedLatestGame = true;
                    var playerRating = GetParticipantRating(participant, rating);
                    if (playerRating is null)
                    {
                        summary.RatingsPending = true;
                        continue;
                    }

                    summary.RatingGames++;
                    summary.RatingStart ??= playerRating.RatingBefore;
                    summary.RatingEnd = playerRating.RatingBefore + playerRating.RatingDelta;
                    summary.RatingDelta = (summary.RatingDelta ?? 0) + playerRating.RatingDelta;
                    summary.AverageGain = summary.RatingDelta / summary.RatingGames;
                }
            }

            SyncRosterInitialRatingsFromSummaries();
        }

        private static RosterPlayerState CloneRosterPlayer(RosterPlayerState rosterPlayer)
            => new()
            {
                RosterPlayerId = rosterPlayer.RosterPlayerId,
                Name = rosterPlayer.Name,
                ToonId = CloneToonId(rosterPlayer.ToonId),
                PlayerId = rosterPlayer.PlayerId,
                InitialRating = rosterPlayer.InitialRating,
                JoinedReplayCount = rosterPlayer.JoinedReplayCount,
                IsSitter = rosterPlayer.IsSitter,
                IsManual = rosterPlayer.IsManual,
                AddSource = rosterPlayer.AddSource,
                CreatedAt = rosterPlayer.CreatedAt,
                UpdatedAt = rosterPlayer.UpdatedAt,
            };

        private PlayerSummaryState GetOrCreateSummary(ParticipantState participant)
        {
            var key = new ToonKey(participant.ToonId);
            var existing = Players.FirstOrDefault(player => new ToonKey(player.ToonId) == key);
            if (existing is not null)
            {
                return existing;
            }

            var created = new PlayerSummaryState
            {
                Name = participant.Name,
                ToonId = CloneToonId(participant.ToonId),
                PlayerId = participant.PlayerId,
            };
            Players.Add(created);
            return created;
        }

        private InHouseRosterPlayerDto ToDto(RosterPlayerState rosterPlayer)
        {
            var summary = Players.FirstOrDefault(player => new ToonKey(player.ToonId) == new ToonKey(rosterPlayer.ToonId));
            return new()
            {
                RosterPlayerId = rosterPlayer.RosterPlayerId,
                Name = rosterPlayer.Name,
                ToonId = CloneToonId(rosterPlayer.ToonId),
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

        private void RecalculateSummariesFromReplays(List<ReplayRuntimeState> runtimeReplays)
        {
            // Replay DTO state is already compact, so refresh only rating fields here.
            foreach (var summary in Players)
            {
                summary.RatingStart = null;
                summary.RatingEnd = null;
                summary.RatingDelta = null;
                summary.AverageGain = null;
                summary.RatingGames = 0;
                summary.RatingsPending = false;
            }

            var runtimeByHash = runtimeReplays.ToDictionary(replay => replay.ReplayHash);
            foreach (var replay in Replays.OrderBy(replay => replay.Gametime))
            {
                if (!runtimeByHash.TryGetValue(replay.ReplayHash, out var runtimeReplay))
                {
                    continue;
                }

                var rating = GetBestRating(runtimeReplay);
                foreach (var participant in replay.Players.Where(player => !player.Observer))
                {
                    var summary = Players.FirstOrDefault(player => new ToonKey(player.ToonId) == new ToonKey(participant.ToonId));
                    if (summary is null)
                    {
                        continue;
                    }

                    var playerRating = GetParticipantRating(participant, rating);
                    if (playerRating is null)
                    {
                        summary.RatingsPending = true;
                        continue;
                    }

                    summary.RatingGames++;
                    summary.RatingStart ??= playerRating.RatingBefore;
                    summary.RatingEnd = playerRating.RatingBefore + playerRating.RatingDelta;
                    summary.RatingDelta = (summary.RatingDelta ?? 0) + playerRating.RatingDelta;
                    summary.AverageGain = summary.RatingDelta / summary.RatingGames;
                }
            }

            SyncRosterInitialRatingsFromSummaries();
        }

        private void SyncRosterInitialRatingsFromSummaries()
        {
            var summariesByToon = new Dictionary<ToonKey, PlayerSummaryState>(Players.Count);
            foreach (var player in Players)
            {
                if (player.RatingStart is not null)
                {
                    summariesByToon[new ToonKey(player.ToonId)] = player;
                }
            }

            foreach (var rosterPlayer in RosterPlayers)
            {
                if (rosterPlayer.IsManual
                    || !summariesByToon.TryGetValue(new ToonKey(rosterPlayer.ToonId), out var summary)
                    || summary.RatingStart is not double ratingStart
                    || Math.Abs(rosterPlayer.InitialRating - ratingStart) <= 0.001)
                {
                    continue;
                }

                rosterPlayer.InitialRating = ratingStart;
                rosterPlayer.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static ReplayPlayerRatingRuntimeState? GetParticipantRating(
            ParticipantState participant,
            ReplayRatingRuntimeState? rating)
        {
            if (rating is null)
            {
                return null;
            }

            foreach (var playerRating in rating.PlayerRatings)
            {
                if ((participant.ReplayPlayerId is int replayPlayerId && playerRating.ReplayPlayerId == replayPlayerId)
                    || (participant.PlayerId is int playerId && playerRating.PlayerId == playerId))
                {
                    return playerRating;
                }
            }

            return null;
        }
    }

    private sealed class RosterPlayerState
    {
        public Guid RosterPlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ToonIdDto ToonId { get; set; } = new();
        public int? PlayerId { get; set; }
        public double InitialRating { get; set; } = 1000;
        public int JoinedReplayCount { get; set; }
        public bool IsSitter { get; set; }
        public bool IsManual { get; set; }
        public string AddSource { get; set; } = "replay";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class PlayerSummaryState
    {
        public int? PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ToonIdDto ToonId { get; set; } = new();
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Observes { get; set; }
        public double? RatingStart { get; set; }
        public double? RatingEnd { get; set; }
        public double? RatingDelta { get; set; }
        public double? AverageGain { get; set; }
        public int RatingGames { get; set; }
        public bool PlayedLatestGame { get; set; }
        public bool ObservedLatestGame { get; set; }
        public bool RatingsPending { get; set; }

        public InHouseGameSessionPlayerSummaryDto ToDto()
            => new()
            {
                Name = Name,
                ToonId = CloneToonId(ToonId),
                Games = Games,
                Wins = Wins,
                Observes = Observes,
                Winrate = Games == 0 ? 0 : (double)Wins / Games,
                RatingStart = RatingStart,
                RatingEnd = RatingEnd,
                RatingDelta = RatingDelta,
                AverageGain = AverageGain,
                PlayedLatestGame = PlayedLatestGame,
                ObservedLatestGame = ObservedLatestGame,
                RatingsPending = RatingsPending,
            };
    }

    private sealed class ReplayState
    {
        public string ReplayHash { get; set; } = string.Empty;
        public DateTime Gametime { get; set; }
        public GameMode GameMode { get; set; }
        public bool TE { get; set; }
        public int Duration { get; set; }
        public int WinnerTeam { get; set; }
        public List<Commander> CommandersTeam1 { get; set; } = [];
        public List<Commander> CommandersTeam2 { get; set; } = [];
        public double? ExpectedWinProbability { get; set; }
        public int? AvgRating { get; set; }
        public bool RatingsPending { get; set; }
        public List<ParticipantState> Players { get; set; } = [];

        public static ReplayState FromReplay(
            ReplayRuntimeState replay,
            List<ParticipantState> participants,
            ReplayRatingRuntimeState? rating)
            => new()
            {
                ReplayHash = replay.ReplayHash,
                Gametime = replay.Gametime,
                GameMode = replay.GameMode,
                TE = replay.TE,
                Duration = replay.Duration,
                WinnerTeam = replay.WinnerTeam,
                CommandersTeam1 = replay.CommandersTeam1,
                CommandersTeam2 = replay.CommandersTeam2,
                ExpectedWinProbability = rating?.ExpectedWinProbability,
                AvgRating = rating?.AvgRating,
                RatingsPending = rating is null,
                Players = participants,
            };

        public InHouseGameSessionReplayDto ToDto()
            => new()
            {
                ReplayHash = ReplayHash,
                Gametime = Gametime,
                GameMode = GameMode,
                Duration = Duration,
                WinnerTeam = WinnerTeam,
                CommandersTeam1 = CommandersTeam1,
                CommandersTeam2 = CommandersTeam2,
                ExpectedWinProbability = ExpectedWinProbability,
                AvgRating = AvgRating,
                RatingsPending = RatingsPending,
                Players = Players
                    .OrderBy(player => player.GamePos)
                    .Select(player => new InHouseGameSessionReplayPlayerDto
                    {
                        Name = player.Name,
                        ToonId = CloneToonId(player.ToonId),
                        Observer = player.Observer,
                        TeamId = player.TeamId,
                        GamePos = player.GamePos,
                    })
                    .ToList(),
            };
    }

    private sealed class ParticipantState
    {
        public int? ReplayPlayerId { get; set; }
        public int? PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ToonIdDto ToonId { get; set; } = new();
        public bool Observer { get; set; }
        public int TeamId { get; set; }
        public int GamePos { get; set; }
        public PlayerResult Result { get; set; }

        public static ParticipantState FromReplayPlayer(ReplayPlayerRuntimeState player, bool observer)
            => new()
            {
                ReplayPlayerId = player.ReplayPlayerId,
                PlayerId = player.PlayerId,
                Name = player.Name,
                ToonId = CloneToonId(player.ToonId),
                Observer = observer,
                TeamId = player.TeamId,
                GamePos = player.GamePos,
                Result = player.Result,
            };

        public static ParticipantState FromObserver(InHouseReplayObserverDto observer, int playerId)
            => new()
            {
                PlayerId = playerId == 0 ? null : playerId,
                Name = observer.Name,
                ToonId = CloneToonId(observer.ToonId),
                Observer = true,
                TeamId = 0,
                GamePos = observer.SlotId,
                Result = PlayerResult.None,
            };
    }

    private sealed class ReplayRuntimeState
    {
        public int ReplayId { get; set; }
        public string ReplayHash { get; set; } = string.Empty;
        public string CompatHash { get; set; } = string.Empty;
        public DateTime Gametime { get; set; }
        public GameMode GameMode { get; set; }
        public bool TE { get; set; }
        public int Duration { get; set; }
        public int WinnerTeam { get; set; }
        public List<Commander> CommandersTeam1 { get; set; } = [];
        public List<Commander> CommandersTeam2 { get; set; } = [];
        public List<ReplayPlayerRuntimeState> Players { get; set; } = [];
        public List<ReplayRatingRuntimeState> Ratings { get; set; } = [];
    }

    private sealed class ReplayPlayerRuntimeState
    {
        public int ReplayPlayerId { get; set; }
        public int PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ToonIdDto ToonId { get; set; } = new();
        public Commander Race { get; set; }
        public int TeamId { get; set; }
        public int GamePos { get; set; }
        public PlayerResult Result { get; set; }
    }

    private sealed class ReplayRatingRuntimeState
    {
        public RatingType RatingType { get; set; }
        public double ExpectedWinProbability { get; set; }
        public int AvgRating { get; set; }
        public List<ReplayPlayerRatingRuntimeState> PlayerRatings { get; set; } = [];
    }

    private sealed class ReplayPlayerRatingRuntimeState
    {
        public int ReplayPlayerId { get; set; }
        public int PlayerId { get; set; }
        public double RatingBefore { get; set; }
        public double RatingDelta { get; set; }
    }

    private sealed record ObserverPlayerState(int PlayerId, int Region, int Realm, int Id);

    private sealed record InHouseReplayFingerprint(
        DateTime GametimeBucket,
        GameMode GameMode,
        int Duration,
        int WinnerTeam,
        string Players)
    {
        public static InHouseReplayFingerprint FromReplay(ReplayDto replay)
        {
            var players = string.Join('|', replay.Players
                .OrderBy(player => player.GamePos)
                .Select(player =>
                {
                    var toonId = player.Player?.ToonId ?? new();
                    return $"{player.GamePos}:{player.TeamId}:{(int)player.Race}:{(int)player.Result}:{toonId.Region}:{toonId.Realm}:{toonId.Id}";
                }));
            return new(
                ReplayDtoExtensions.FloorToBucketMinutes(replay.Gametime, 3),
                replay.GameMode,
                replay.Duration,
                replay.WinnerTeam,
                players);
        }
    }

    private readonly record struct ToonKey(int Region, int Realm, int Id)
    {
        public ToonKey(ToonIdDto toonId) : this(toonId.Region, toonId.Realm, toonId.Id)
        {
        }
    }

    private static ReplayRatingRuntimeState? GetBestRating(ReplayRuntimeState replay)
    {
        var preferred = GetPreferredRatingType(replay);
        var fallback = GetFallbackRatingType(preferred);
        return replay.Ratings.FirstOrDefault(rating => rating.RatingType == preferred)
            ?? replay.Ratings.FirstOrDefault(rating => rating.RatingType == fallback)
            ?? replay.Ratings.FirstOrDefault();
    }

    private static RatingType GetPreferredRatingType(ReplayRuntimeState replay)
    {
        var isCommander = Data.IsCommanderGameMode(replay.GameMode);
        return isCommander
            ? replay.TE ? RatingType.CommandersTE : RatingType.Commanders
            : replay.TE ? RatingType.StandardTE : RatingType.Standard;
    }

    private static RatingType GetFallbackRatingType(RatingType ratingType)
        => ratingType switch
        {
            RatingType.CommandersTE => RatingType.Commanders,
            RatingType.StandardTE => RatingType.Standard,
            RatingType.Commanders => RatingType.CommandersTE,
            RatingType.Standard => RatingType.StandardTE,
            _ => RatingType.All,
        };
}
