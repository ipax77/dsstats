using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ReplayRepository(IServiceScopeFactory scopeFactory, ILogger<ReplayRepository> logger) : IReplayRepository
{
    public async Task<ReplayDetails?> GetReplayDetails(string replayHash)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replay = await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .Include(i => i.Players)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Include(i => i.Players)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
                        .ThenInclude(i => i.Unit)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);

        if (replay is null)
        {
            return null;
        }

        var replayRatings = await context.ReplayRatings
            .Where(x => x.ReplayId == replay.ReplayId)
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


        return new()
        {
            ReplayHash = replay.ReplayHash,
            Replay = replay.ToDto(),
            ReplayRatings = replayRatings,
        };
    }

    public async Task<ReplayDetails?> GetNextReplay(bool after, string replayHash)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
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
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

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
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        try
        {
            int skip = ((request.Page - 1) * request.PageSize) + request.Skip;
            int take = request.Take;
            if (skip < 0 ||take <= 0)
            {
                return [];
            }

            IQueryable<ReplayList> query = GetReplaysQueriable(request, context, false);
            IOrderedQueryable<ReplayList> ordered = GetOrderedReplays(query, request);
            List<ReplayList> list = await ordered
                .Skip(skip)
                .Take(take)
                .ToListAsync(token);

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
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        try
        {
            var query = GetReplaysQueriable(request, context, true);
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
                case nameof(ReplayRatingList.LeaverType):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.RatingList == null ? LeaverType.None : x.RatingList.LeaverType) : query.OrderByDescending(x => x.RatingList == null ? LeaverType.None : x.RatingList.LeaverType))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.RatingList == null ? LeaverType.None : x.RatingList.LeaverType) : orderedQuery.ThenByDescending(x => x.RatingList == null ? LeaverType.None : x.RatingList.LeaverType));
                    break;
                case nameof(ReplayRatingList.AvgRating):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.RatingList == null ? 0 : x.RatingList.AvgRating) : query.OrderByDescending(x => x.RatingList == null ? 0 : x.RatingList.AvgRating))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.RatingList == null ? 0 : x.RatingList.AvgRating) : orderedQuery.ThenByDescending(x => x.RatingList == null ? 0 : x.RatingList.AvgRating));
                    break;
                case nameof(ReplayRatingList.Exp2Win):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.RatingList == null ? 0 : x.RatingList.Exp2Win) : query.OrderByDescending(x => x.RatingList == null ? 0 : x.RatingList.Exp2Win))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.RatingList == null ? 0 : x.RatingList.Exp2Win) : orderedQuery.ThenByDescending(x => x.RatingList == null ? 0 : x.RatingList.Exp2Win));
                    break;
            }
        }
        return orderedQuery ?? query.OrderByDescending(x => x.GameTime);
    }

    private static IQueryable<ReplayList> GetReplaysQueriable(ReplaysRequest request, DsstatsContext context, bool count)
    {
        var replays = GetFilteredReplays(request, context);
        var query = count ? from r in replays
                            select new ReplayList()
                            {
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
                            }
                    : from r in replays
                      from rr in context.ReplayRatings
                          .Where(x => x.ReplayId == r.ReplayId && x.RatingType == request.RatingType)
                          .DefaultIfEmpty()
                      select new ReplayList()
                      {
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
                          RatingList = rr == null ? null : new()
                          {
                              Exp2Win = rr.ExpectedWinProbability,
                              AvgRating = rr.AvgRating,
                              LeaverType = rr.LeaverType,
                          },
                      };

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
                    query = query.Where(x => x.Players.Any(p => p.Name.Contains(name) && p.Race == commander));
                }
                else if (name is not null)
                {
                    query = query.Where(x => x.Players.Any(p => p.Name.Contains(name)));
                }
                else if (commander is not null)
                {
                    query = query.Where(x => x.Players.Any(p => p.Race == commander));
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
                    query = query.Where(x => x.Players.Any(p => p.Name.Contains(name)));
                }
            }

            if (!string.IsNullOrEmpty(request.Commander))
            {
                var cmdrs = request.Commander.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var cmdr in cmdrs)
                {
                    if (Enum.TryParse<Commander>(cmdr, true, out var commander))
                    {
                        query = query.Where(x => x.Players.Any(p => p.Race == commander));
                    }
                }
            }
        }

        return query;
    }

    private static IQueryable<Replay> GetFilteredReplays(ReplaysRequest request, DsstatsContext context)
    {
        var query = context.Replays
            .Where(x => x.Gametime > new DateTime(2018, 1, 1))
            .AsQueryable();
        if (request.Filter is null)
        {
            return query;
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
                var toonId = GetPlayerId(name.Replace("%7C", "|"));

                if (toonId != null)
                {
                    query = from r in query
                            from rp in r.Players
                            join p in context.Players on rp.PlayerId equals p.PlayerId
                            where (posFilter.GamePos == 0 || rp.GamePos == posFilter.GamePos)
                            && (posFilter.Commander == Commander.None || rp.Race == posFilter.Commander)
                            && (posFilter.OppCommander == Commander.None || rp.OppRace == posFilter.OppCommander)
                            && p.ToonId.Id == toonId.Id && p.ToonId.Realm == toonId.Realm && p.ToonId.Region == toonId.Region
                            select r;
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
        return query;
    }

    public static ToonIdDto? GetPlayerId(string? playerIdString)
    {
        if (string.IsNullOrWhiteSpace(playerIdString))
        {
            return null;
        }

        var ents = playerIdString.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length != 3)
        {
            return null;
        }

        if (int.TryParse(ents[0], out int toonId)
            && int.TryParse(ents[1], out int realmId)
            && int.TryParse(ents[2], out int regionId))
        {
            return new()
            {
                Id = toonId,
                Realm = realmId,
                Region = regionId
            };
        }
        return null;
    }
}



internal sealed class ReplayList
{
    public string ReplayHash { get; init; } = string.Empty;
    public DateTime GameTime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public int PlayerPos { get; set; }
    public List<ReplayPlayerList> Players { get; init; } = [];
    public ReplayRatingList? RatingList { get; set; }

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
            Exp2Win = RatingList?.Exp2Win,
            AvgRating = RatingList?.AvgRating,
            LeaverType = RatingList?.LeaverType ?? LeaverType.None,
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

internal sealed class ReplayRatingList
{
    public double? Exp2Win { get; init; }
    public int? AvgRating { get; init; }
    public LeaverType LeaverType { get; init; }
}