using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ReplayRepository(IDbContextFactory<DsstatsContext> contextFactory, ILogger<ReplayRepository> logger) : IReplayRepository
{
    public async Task<ReplayDetails?> GetReplayDetails(string replayHash)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var replayEntity = await context.Replays
            .AsNoTracking()
            .Where(f => f.ReplayHash == replayHash)
            .FirstOrDefaultAsync();

        if (replayEntity is null)
        {
            return null;
        }

        var replay = new ReplayDetails
        {
            ReplayHash = replayEntity.ReplayHash,
            Replay = new ReplayDto
            {
                Title = replayEntity.Title,
                FileName = replayEntity.FileName ?? string.Empty,
                Version = replayEntity.Version,
                GameMode = replayEntity.GameMode,
                Gametime = replayEntity.Gametime,
                RegionId = replayEntity.RegionId,
                BaseBuild = replayEntity.BaseBuild,
                Duration = replayEntity.Duration,
                Cannon = replayEntity.Cannon,
                Bunker = replayEntity.Bunker,
                WinnerTeam = replayEntity.WinnerTeam,
                MiddleChanges = replayEntity.MiddleChanges.ToList(),
                CompatHash = replayEntity.CompatHash,
            }
        };

        var players = await context.ReplayPlayers
            .AsNoTracking()
            .Where(p => p.ReplayId == replayEntity.ReplayId)
            .Include(p => p.Player)
            .OrderBy(p => p.GamePos)
            .ToListAsync();

        var playerIds = players.Select(p => p.ReplayPlayerId).ToList();
        var spawns = await (
            from spawn in context.Spawns.AsNoTracking()
            where playerIds.Contains(spawn.ReplayPlayerId)
            select new ReplayDetailSpawnRow(
                spawn.ReplayPlayerId,
                spawn.SpawnId,
                spawn.Breakpoint,
                spawn.GasCount,
                spawn.Income,
                spawn.ArmyValue,
                spawn.KilledValue,
                spawn.UpgradeSpent))
            .ToListAsync();

        var spawnIds = spawns.Select(s => s.SpawnId).ToList();
        var units = await (
            from spawnUnit in context.SpawnUnits.AsNoTracking()
            where spawnIds.Contains(spawnUnit.SpawnId)
            select new ReplayDetailUnitRow(
                spawnUnit.SpawnId,
                spawnUnit.Count,
                spawnUnit.Unit!.Name))
            .ToListAsync();

        var upgrades = await (
            from upgrade in context.PlayerUpgrades.AsNoTracking()
            let replayPlayerId = EF.Property<int?>(upgrade, "ReplayPlayerId")
            where replayPlayerId.HasValue && playerIds.Contains(replayPlayerId.Value)
            select new ReplayDetailUpgradeRow(
                replayPlayerId.Value,
                upgrade.Gameloop,
                upgrade.Upgrade!.Name))
            .ToListAsync();

        var spawnsByPlayerId = spawns.ToLookup(spawn => spawn.ReplayPlayerId);
        var unitsBySpawnId = units.ToLookup(unit => unit.SpawnId);
        var upgradesByPlayerId = upgrades.ToLookup(upgrade => upgrade.ReplayPlayerId);

        replay.Replay.Players = players
            .Select(player => new ReplayPlayerDto
            {
                CompatHash = player.CompatHash,
                Name = player.Name,
                Clan = player.Clan,
                Race = player.Race,
                SelectedRace = player.SelectedRace,
                GamePos = player.GamePos,
                TeamId = player.TeamId,
                Result = player.Result,
                Duration = player.Duration,
                Apm = player.Apm,
                Messages = player.Messages,
                Pings = player.Pings,
                IsMvp = player.IsMvp,
                IsUploader = player.IsUploader,
                Spawns = spawnsByPlayerId[player.ReplayPlayerId]
                    .OrderBy(spawn => spawn.Breakpoint)
                    .Select(spawn => new SpawnDto
                    {
                        Breakpoint = spawn.Breakpoint,
                        GasCount = spawn.GasCount,
                        Income = spawn.Income,
                        ArmyValue = spawn.ArmyValue,
                        KilledValue = spawn.KilledValue,
                        UpgradeSpent = spawn.UpgradeSpent,
                        Units = unitsBySpawnId[spawn.SpawnId]
                            .Select(unit => new UnitDto
                            {
                                Count = unit.Count,
                                Name = unit.UnitName,
                                Positions = null
                            })
                            .ToList()
                    })
                    .ToList(),
                TierUpgrades = player.TierUpgrades.ToList(),
                Refineries = player.Refineries.ToList(),
                Upgrades = upgradesByPlayerId[player.ReplayPlayerId]
                    .Select(upgrade => new UpgradeDto
                    {
                        Gameloop = upgrade.Gameloop,
                        Name = upgrade.Name
                    })
                    .ToList(),
                Player = new PlayerDto
                {
                    PlayerId = player.Player!.PlayerId,
                    Name = player.Player.Name,
                    ToonId = new ToonIdDto
                    {
                        Region = player.Player.ToonId.Region,
                        Realm = player.Player.ToonId.Realm,
                        Id = player.Player.ToonId.Id
                    }
                }
            })
            .ToList();

        var replayRatings = await context.ReplayRatings
            .Where(x => x.Replay!.ReplayHash == replayHash)
            .Select(s => new ReplayRatingDto()
            {
                RatingType = s.RatingType,
                LeaverType = s.LeaverType,
                ExpectedWinProbability = s.ExpectedWinProbability,
                IsPreRating = s.IsPreRating,
                AvgRating = s.AvgRating,
                ReplayPlayerRatings = s.ReplayPlayerRatings.Select(t => new ReplayPlayerRatingDto()
                {
                    RatingType = t.RatingType,
                    RatingBefore = t.RatingBefore,
                    RatingDelta = t.RatingDelta,
                    Games = t.Games,
                    ToonId = new()
                    {
                        Realm = t.Player!.ToonId.Realm,
                        Region = t.Player.ToonId.Region,
                        Id = t.Player.ToonId.Id
                    }
                }).ToList(),
            })
            .ToListAsync();


        replay.Replay.SpawnPlayback = await context.ReplaySpawnPlaybacks
            .AsNoTracking()
            .Where(x => x.Replay!.ReplayHash == replayHash)
            .Select(x => new SpawnPlaybackInfoDto
            {
                Available = true,
                FormatVersion = x.FormatVersion,
                CompressedLength = x.CompressedLength,
                UncompressedLength = x.UncompressedLength,
                UnitCount = x.UnitCount
            })
            .FirstOrDefaultAsync();

        replay.ReplayRatings = replayRatings;
        return replay;
    }

    public async Task<byte[]?> GetReplaySpawnPlayback(string replayHash, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        return await context.ReplaySpawnPlaybacks
            .AsNoTracking()
            .Where(x => x.Replay!.ReplayHash == replayHash)
            .Select(x => x.Payload)
            .FirstOrDefaultAsync(token);
    }

    public async Task<ReplaySpawnPositionsDto?> GetReplaySpawnPositions(string replayHash, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var rows = await (
            from replay in context.Replays.AsNoTracking()
            from player in replay.Players
            from spawn in player.Spawns
            from unit in spawn.Units
            where replay.ReplayHash == replayHash
                && unit.Positions != null
            select new SpawnPositionRow(
                player.GamePos,
                spawn.Breakpoint,
                unit.Unit!.Name,
                unit.Positions!))
            .ToListAsync(token);

        if (rows.Count == 0)
        {
            return null;
        }

        return new()
        {
            Players = rows
                .GroupBy(row => row.GamePos)
                .OrderBy(group => group.Key)
                .Select(playerGroup => new ReplayPlayerSpawnPositionsDto
                {
                    GamePos = playerGroup.Key,
                    Spawns = playerGroup
                        .GroupBy(row => row.Breakpoint)
                        .OrderBy(group => group.Key)
                        .Select(spawnGroup => new SpawnPositionsDto
                        {
                            Breakpoint = spawnGroup.Key,
                            Units = spawnGroup
                                .OrderBy(row => row.UnitName, StringComparer.Ordinal)
                                .Select(row => new UnitPositionsDto
                                {
                                    Name = row.UnitName,
                                    Positions = row.Positions.ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task<ReplayDetails?> GetNextReplay(bool after, string replayHash)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var replayTime = await context.Replays
            .Where(w => w.ReplayHash == replayHash)
            .Select(s => s.Gametime)
            .FirstOrDefaultAsync();
        if (replayTime == default)
        {
            return null;
        }

        string? nextReplayHash;
        if (after)
        {
            nextReplayHash = await context.Replays
                .Where(w => w.Gametime > replayTime)
                .OrderBy(o => o.Gametime)
                .Select(s => s.ReplayHash)
                .FirstOrDefaultAsync();
        }
        else
        {
            nextReplayHash = await context.Replays
                .Where(w => w.Gametime < replayTime)
                .OrderByDescending(o => o.Gametime)
                .Select(s => s.ReplayHash)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrEmpty(nextReplayHash))
        {
            return null;
        }
        return await GetReplayDetails(nextReplayHash);
    }

    public async Task<ReplayDetails?> GetLatestReplay()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var replayHash = await context.Replays
            .OrderByDescending(o => o.Gametime)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(replayHash))
        {
            return null;
        }

        return await GetReplayDetails(replayHash);
    }

    public async Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        try
        {
            int skip = ((request.Page - 1) * request.PageSize) + request.Skip;
            int take = request.Take;
            if (skip < 0 || take <= 0)
            {
                return [];
            }

            var filteredReplays = GetFilteredReplays(request, context);
            bool requiresRatingJoin = RequiresRatingJoin(request);
            IQueryable<ReplayList> query = requiresRatingJoin
                ? GetReplaysQueriableWithRatings(filteredReplays, context, request.RatingType)
                : GetReplaysQueriable(filteredReplays);

            IOrderedQueryable<ReplayList> ordered = GetOrderedReplays(query, request);
            List<ReplayList> list = await ordered
                .Skip(skip)
                .Take(take)
                .ToListAsync(token);

            if (!requiresRatingJoin)
            {
                await LoadReplayRatings(list, context, request.RatingType, token);
            }

            return list.Select(s => s.GetDto()).ToList();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Error getting replays: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        try
        {
            var query = GetFilteredReplays(request, context);
            return await query.CountAsync(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Error getting replays count: {error}", ex.Message);
        }
        return 0;
    }

    private static IOrderedQueryable<ReplayList> GetOrderedReplays(IQueryable<ReplayList> query, ReplaysRequest request)
    {
        if (request.TableOrders is null || request.TableOrders.Count == 0)
        {
            return query.OrderByDescending(x => x.GameTime);
        }

        IOrderedQueryable<ReplayList>? orderedQuery = null;
        foreach (var order in request.TableOrders)
        {
            switch (order.Column)
            {
                case nameof(ReplayList.GameTime):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.GameTime) : query.OrderByDescending(x => x.GameTime))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.GameTime) : orderedQuery.ThenByDescending(x => x.GameTime));
                    break;
                case nameof(ReplayList.Duration):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Duration) : query.OrderByDescending(x => x.Duration))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Duration) : orderedQuery.ThenByDescending(x => x.Duration));
                    break;
                case nameof(ReplayList.GameMode):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.GameMode) : query.OrderByDescending(x => x.GameMode))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.GameMode) : orderedQuery.ThenByDescending(x => x.GameMode));
                    break;
                case nameof(ReplayList.LeaverType):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.LeaverType) : query.OrderByDescending(x => x.LeaverType))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.LeaverType) : orderedQuery.ThenByDescending(x => x.LeaverType));
                    break;
                case nameof(ReplayList.AvgRating):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.AvgRating ?? 0) : query.OrderByDescending(x => x.AvgRating ?? 0))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.AvgRating ?? 0) : orderedQuery.ThenByDescending(x => x.AvgRating ?? 0));
                    break;
                case nameof(ReplayList.Exp2Win):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Exp2Win ?? 0) : query.OrderByDescending(x => x.Exp2Win ?? 0))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Exp2Win ?? 0) : orderedQuery.ThenByDescending(x => x.Exp2Win ?? 0));
                    break;
            }
        }
        return orderedQuery ?? query.OrderByDescending(x => x.GameTime);
    }

    private static bool RequiresRatingJoin(ReplaysRequest request)
    {
        return request.TableOrders?.Any(order => order.Column is
            nameof(ReplayList.LeaverType) or
            nameof(ReplayList.AvgRating) or
            nameof(ReplayList.Exp2Win)) == true;
    }

    private static IQueryable<ReplayList> GetReplaysQueriable(IQueryable<Replay> replays)
    {
        return from r in replays
               select new ReplayList()
               {
                   ReplayId = r.ReplayId,
                   ReplayHash = r.ReplayHash,
                   GameTime = r.Gametime,
                   GameMode = r.GameMode,
                   Duration = r.Duration,
                   WinnerTeam = r.WinnerTeam,
                   Players = r.Players.Select(s => new ReplayPlayerList()
                   {
                       Name = s.Name,
                       Race = s.Race,
                       Team = s.TeamId,
                   }).ToList(),
               };
    }

    private static IQueryable<ReplayList> GetReplaysQueriableWithRatings(IQueryable<Replay> replays, DsstatsContext context, RatingType ratingType)
    {
        return from r in replays
               from rr in context.ReplayRatings
                   .AsNoTracking()
                   .Where(x => x.ReplayId == r.ReplayId && x.RatingType == ratingType)
                   .DefaultIfEmpty()
               select new ReplayList()
               {
                   ReplayId = r.ReplayId,
                   ReplayHash = r.ReplayHash,
                   GameTime = r.Gametime,
                   GameMode = r.GameMode,
                   Duration = r.Duration,
                   WinnerTeam = r.WinnerTeam,
                   Players = r.Players.Select(s => new ReplayPlayerList()
                   {
                       Name = s.Name,
                       Race = s.Race,
                       Team = s.TeamId,
                   }).ToList(),
                   Exp2Win = rr == null ? null : rr.ExpectedWinProbability,
                   AvgRating = rr == null ? null : rr.AvgRating,
                   LeaverType = rr == null ? LeaverType.None : rr.LeaverType,
               };
    }

    private static async Task LoadReplayRatings(List<ReplayList> replays, DsstatsContext context, RatingType ratingType, CancellationToken token)
    {
        if (replays.Count == 0)
        {
            return;
        }

        var replayIds = replays.Select(s => s.ReplayId).ToList();
        var ratings = await context.ReplayRatings
            .AsNoTracking()
            .Where(x => replayIds.Contains(x.ReplayId) && x.RatingType == ratingType)
            .Select(s => new
            {
                s.ReplayId,
                Exp2Win = (double?)s.ExpectedWinProbability,
                AvgRating = (int?)s.AvgRating,
                s.LeaverType,
            })
            .ToDictionaryAsync(x => x.ReplayId, token);

        foreach (var replay in replays)
        {
            if (ratings.TryGetValue(replay.ReplayId, out var rating))
            {
                replay.Exp2Win = rating.Exp2Win;
                replay.AvgRating = rating.AvgRating;
                replay.LeaverType = rating.LeaverType;
            }
        }
    }

    private static IQueryable<Replay> ApplyPlayerSearchFilters(IQueryable<Replay> query, ReplaysRequest request)
    {
        if (request.LinkCommanders)
        {
            var names = request.Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var cmdrs = request.Commander?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            for (int i = 0; i < Math.Max(names.Length, cmdrs.Length); i++)
            {
                var name = i < names.Length ? names[i] : null;
                var cmdr = i < cmdrs.Length ? cmdrs[i] : null;
                var commander = Enum.TryParse(typeof(Commander), cmdr, true, out var c) ? (Commander?)c : null;

                if (name is not null && commander is not null)
                {
                    query = query.Where(r => r.Players.Any(rp => rp.Name.Contains(name) && rp.Race == commander));
                }
                else if (name is not null)
                {
                    query = query.Where(r => r.Players.Any(rp => rp.Name.Contains(name)));
                }
                else if (commander is not null)
                {
                    query = query.Where(r => r.Players.Any(rp => rp.Race == commander));
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Name))
            {
                var names = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in names)
                {
                    query = query.Where(r => r.Players.Any(rp => rp.Name.Contains(name)));
                }
            }

            if (!string.IsNullOrEmpty(request.Commander))
            {
                var cmdrs = request.Commander.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var cmdr in cmdrs)
                {
                    if (Enum.TryParse<Commander>(cmdr, true, out var commander))
                    {
                        query = query.Where(r => r.Players.Any(rp => rp.Race == commander));
                    }
                }
            }
        }

        return query;
    }

    private static IQueryable<Replay> GetFilteredReplays(ReplaysRequest request, DsstatsContext context)
    {
        var query = context.Replays
            .AsNoTracking()
            .Where(x => x.Gametime > new DateTime(2018, 1, 1))
            .AsQueryable();
        if (request.Filter is null)
        {
            return ApplyPlayerSearchFilters(query, request);
        }

        if (request.Filter.Playercount != 0)
        {
            query = query.Where(x => x.PlayerCount == request.Filter.Playercount);
        }

        if (request.Filter.TournamentEdition)
        {
            query = query.Where(x => x.TE);
        }

        if (request.Filter.GameModes.Count > 0
            && !request.Filter.GameModes.Contains(GameMode.None))
        {
            query = query.Where(x => request.Filter.GameModes.Contains(x.GameMode));
        }

        if (request.Filter.PosFilters.Count > 0)
        {
            foreach (var posFilter in request.Filter.PosFilters)
            {
                string name = posFilter.PlayerNameOrId.Trim();
                var toonIds = GetPlayerIds(name.Replace("%7C", "|"));

                if (toonIds.Count == 1)
                {
                    var toonId = toonIds[0];
                    query = from r in query
                            from rp in r.Players
                            join p in context.Players on rp.PlayerId equals p.PlayerId
                            where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                            && (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                            && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                            && p.ToonId.Id == toonId.Id && p.ToonId.Realm == toonId.Realm && p.ToonId.Region == toonId.Region
                            select r;
                }
                else if (toonIds.Count > 1)
                {
                    var predicate = PredicateBuilder.New<ReplayPlayer>(false);
                    foreach (var t in toonIds)
                    {
                        var temp = t; // closure capture
                        predicate = predicate.Or(rp => (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                                                      && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                                                      && rp.Player!.ToonId.Id == temp.Id
                                                      && rp.Player.ToonId.Realm == temp.Realm
                                                      && rp.Player.ToonId.Region == temp.Region);
                    }

                    query = query.Where(r => r.Players.AsQueryable().Any(predicate));
                }
                else
                {
                    query = from r in query
                            from rp in r.Players
                            where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                            && (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                            && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                            && (string.IsNullOrEmpty(name) || rp.Name == name)
                            select r;
                }

                foreach (var unitFilter in posFilter.UnitFilters)
                {
                    if (string.IsNullOrEmpty(unitFilter.Name) || unitFilter.Count <= 0)
                    {
                        continue;
                    }
                    query = from r in query
                            from rp in r.Players
                            from sp in rp.Spawns
                            from su in sp.Units
                            where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                              && sp.Breakpoint == unitFilter.Breakpoint
                              && su.Unit!.Name == unitFilter.Name
                              && (unitFilter.Min ? su.Count >= unitFilter.Count : su.Count < unitFilter.Count)
                            select r;
                }
            }
            query = query.Distinct();
        }
        return ApplyPlayerSearchFilters(query, request);
    }

    public static List<ToonIdDto> GetPlayerIds(string? playerIdString)
    {
        if (string.IsNullOrWhiteSpace(playerIdString))
        {
            return [];
        }
        List<ToonIdDto> toonIds = [];
        var toonIdStrings = playerIdString.Split('|', StringSplitOptions.RemoveEmptyEntries);
        foreach (var toonId in toonIdStrings)
        {
            var ents = toonId.Split('x', StringSplitOptions.RemoveEmptyEntries);
            if (ents.Length != 3)
            {
                continue;
            }

            if (int.TryParse(ents[0], out int id)
                && int.TryParse(ents[1], out int regionId)
                && int.TryParse(ents[2], out int realmId))
            {
                toonIds.Add(new()
                {
                    Id = id,
                    Realm = realmId,
                    Region = regionId
                });
            }

        }
        return toonIds;
    }
}



internal sealed class ReplayList
{
    public int ReplayId { get; init; }
    public string ReplayHash { get; init; } = string.Empty;
    public DateTime GameTime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public int PlayerPos { get; set; }
    public List<ReplayPlayerList> Players { get; init; } = [];
    public double? Exp2Win { get; set; }
    public int? AvgRating { get; set; }
    public LeaverType LeaverType { get; set; }

    public ReplayListDto GetDto()
    {
        return new()
        {
            ReplayHash = ReplayHash,
            Gametime = GameTime,
            GameMode = GameMode,
            Duration = Duration,
            WinnerTeam = WinnerTeam,
            CommandersTeam1 = Players.Where(x => x.Team == 1).Select(s => s.Race).ToList(),
            CommandersTeam2 = Players.Where(x => x.Team == 2).Select(s => s.Race).ToList(),
            Exp2Win = Exp2Win,
            AvgRating = AvgRating,
            LeaverType = LeaverType,
            PlayerPos = PlayerPos,
        };
    }
}

internal sealed class ReplayPlayerList
{
    public string Name { get; set; } = string.Empty;
    public Commander Race { get; set; }
    public Commander OppRace { get; set; }
    public int Team { get; set; }
    public int GamePos { get; set; }
}

internal sealed record SpawnPositionRow(
    int GamePos,
    Breakpoint Breakpoint,
    string UnitName,
    int[] Positions);

internal sealed record ReplayDetailSpawnRow(
    int ReplayPlayerId,
    int SpawnId,
    Breakpoint Breakpoint,
    int GasCount,
    int Income,
    int ArmyValue,
    int KilledValue,
    int UpgradeSpent);

internal sealed record ReplayDetailUnitRow(
    int SpawnId,
    int Count,
    string UnitName);

internal sealed record ReplayDetailUpgradeRow(
    int ReplayPlayerId,
    int Gameloop,
    string Name);
