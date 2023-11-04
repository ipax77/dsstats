using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class ReplaysService : IReplaysService
{
    private readonly ReplayContext context;
    private readonly IMapper mapper;

    public ReplaysService(ReplayContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash, bool comboRating)
    {
        if (comboRating)
        {
            return await context.Replays
                    .Where(x => x.ReplayHash == replayHash && x.ComboReplayRating != null)
                    .Select(s => new ReplayRatingDto()
                    {
                        RatingType = s.ComboReplayRating!.RatingType,
                        LeaverType = s.ComboReplayRating!.LeaverType,
                        ExpectationToWin = s.ComboReplayRating!.ExpectationToWin,
                        IsPreRating = s.ComboReplayRating!.IsPreRating,
                        ReplayId = s.ReplayId,
                        RepPlayerRatings = s.ReplayPlayers.Select(t => new RepPlayerRatingDto()
                        {
                            GamePos = t.ComboReplayPlayerRating!.GamePos,
                            Rating = t.ComboReplayPlayerRating.Rating,
                            RatingChange = t.ComboReplayPlayerRating!.Change,
                            Games = t.ComboReplayPlayerRating.Games,
                            Consistency = t.ComboReplayPlayerRating!.Consistency,
                            Confidence = t.ComboReplayPlayerRating!.Confidence,
                            ReplayPlayerId = t.ReplayPlayerId
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
        }
        else
        {
            return await context.Replays
                    .Where(x => x.ReplayHash == replayHash && x.ReplayRatingInfo != null)
                    .Select(s => new ReplayRatingDto()
                    {
                        RatingType = s.ReplayRatingInfo!.RatingType,
                        LeaverType = s.ReplayRatingInfo!.LeaverType,
                        ExpectationToWin = s.ReplayRatingInfo!.ExpectationToWin,
                        IsPreRating = s.ReplayRatingInfo!.IsPreRating,
                        ReplayId = s.ReplayId,
                        RepPlayerRatings = s.ReplayPlayers.Select(t => new RepPlayerRatingDto()
                        {
                            GamePos = t.ReplayPlayerRatingInfo!.GamePos,
                            Rating = (int)t.ReplayPlayerRatingInfo.Rating,
                            RatingChange = t.ReplayPlayerRatingInfo!.RatingChange,
                            Games = t.ReplayPlayerRatingInfo.Games,
                            Consistency = t.ReplayPlayerRatingInfo!.Consistency,
                            Confidence = t.ReplayPlayerRatingInfo!.Confidence,
                            ReplayPlayerId = t.ReplayPlayerId
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
        }
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, bool dry = false, CancellationToken token = default)
    {
        var replay = await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash, token);

        if (replay == null)
        {
            return null;
        }

        if (!dry)
        {
            context.ReplayViewCounts.Add(new ReplayViewCount()
            {
                ReplayHash = replay.ReplayHash
            });
            await context.SaveChangesAsync(token);
        }
        return replay with { Views = replay.Views + 1 };
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        if (request.Arcade)
        {
            return await GetArcadeReplaysCount(request, token);
        }

        var query = GetReplaysQueriable(request);
        query = FilterReplays(request, query);

        return await query.CountAsync(token);
    }

    public async Task<ReplaysResponse> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        if (request.Arcade)
        {
            return await GetArcadeReplays(request, token);
        }

        try
        {

            var query = GetReplaysQueriable(request);
            query = FilterReplays(request, query);
            query = SortReplays(request, query);


            if (request.PlayerId is not null)
            {
                return await GetReplaysForPlayerId(query, request, token);
            }

            var replays = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(s => new ReplayListDto()
                {
                    GameTime = s.GameTime,
                    Duration = s.Duration,
                    WinnerTeam = s.WinnerTeam,
                    GameMode = s.GameMode,
                    TournamentEdition = s.TournamentEdition,
                    ReplayHash = s.ReplayHash,
                    DefaultFilter = s.DefaultFilter,
                    CommandersTeam1 = s.CommandersTeam1,
                    CommandersTeam2 = s.CommandersTeam2,
                    MaxLeaver = s.Maxleaver
                })
                .ToListAsync(token);

            return new() { Replays = replays };
        }
        catch (OperationCanceledException) { }

        return new();
    }

    private async Task<ReplaysResponse> GetReplaysForPlayerId(IQueryable<Replay> query,
                                                              ReplaysRequest request,
                                                              CancellationToken token)
    {
        if (request.PlayerId is null)
        {
            return new();
        }

        var replays = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(s => new ReplayListRatingDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver,
                Exp2Win = s.ComboReplayRating != null ? s.ComboReplayRating.ExpectationToWin : 0,
                ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerListDto()
                {
                    Name = t.Name,
                    GamePos = t.GamePos,
                    Race = t.Race,
                    ReplayPlayerRating = t.ComboReplayPlayerRating == null ? null : new ReplayPlayerRatingListDto()
                    {
                        RatingChange = t.ComboReplayPlayerRating.Change
                    },
                    Player = new PlayerId()
                    {
                        ToonId = t.Player.ToonId,
                        RealmId = t.Player.RealmId,
                        RegionId = t.Player.RegionId
                    }
                }).ToList()
            })
            .ToListAsync(token);

        ReplaysResponse response = new() { PlayerId = request.PlayerId };

        foreach (var replay in replays)
        {
            ReplayPlayerInfo? info = null;

            var player = replay.ReplayPlayers.FirstOrDefault(f => f.Player.ToonId == request.PlayerId.ToonId
                && f.Player.RealmId == request.PlayerId.RealmId
                && f.Player.RegionId == request.PlayerId.RegionId);

            if (player is not null)
            {
                info = new()
                {
                    Name = player.Name,
                    Pos = player.GamePos,
                    RatingChange = player.ReplayPlayerRating?.RatingChange ?? 0,
                    Commander = player.Race,
                };
            }

            response.Replays.Add(new()
            {
                GameTime = replay.GameTime,
                Duration = replay.Duration,
                WinnerTeam = replay.WinnerTeam,
                GameMode = replay.GameMode,
                ReplayHash = replay.ReplayHash,
                DefaultFilter = replay.DefaultFilter,
                CommandersTeam1 = replay.CommandersTeam1,
                CommandersTeam2 = replay.CommandersTeam2,
                MaxLeaver = replay.MaxLeaver,
                Exp2Win = replay.Exp2Win,
                PlayerInfo = info
            });
        }

        return response;
    }

    private IQueryable<Replay> GetReplaysQueriable(ReplaysRequest request)
    {
        var replays = context.Replays
            .Where(x => x.GameTime > new DateTime(2018, 1, 1));

        if (request.PlayerId is not null)
        {
            if (request.PlayerIdVs is not null)
            {
                replays = from r in replays
                          from rp in r.ReplayPlayers
                          from rp1 in r.ReplayPlayers
                          where rp.Player.ToonId == request.PlayerId.ToonId
                            && rp.Player.RealmId == request.PlayerId.RealmId
                            && rp.Player.RegionId == request.PlayerId.RegionId
                            && rp1.Player.ToonId == request.PlayerIdVs.ToonId
                            && rp1.Player.RealmId == request.PlayerIdVs.RealmId
                            && rp1.Player.RegionId == request.PlayerIdVs.RegionId
                            && rp1.Team != rp.Team
                          select r;
            }
            else if (request.PlayerIdWith is not null)
            {
                replays = from r in replays
                          from rp in r.ReplayPlayers
                          from rp1 in r.ReplayPlayers
                          where rp.Player.ToonId == request.PlayerId.ToonId
                            && rp.Player.RealmId == request.PlayerId.RealmId
                            && rp.Player.RegionId == request.PlayerId.RegionId
                            && rp1.Player.ToonId == request.PlayerIdWith.ToonId
                            && rp1.Player.RealmId == request.PlayerIdWith.RealmId
                            && rp1.Player.RegionId == request.PlayerIdWith.RegionId
                            && rp1.Team == rp.Team
                          select r;
            }
            else
            {
                replays = from r in replays
                          from rp in r.ReplayPlayers
                          where rp.Player.ToonId == request.PlayerId.ToonId
                            && rp.Player.RealmId == request.PlayerId.RealmId
                            && rp.Player.RegionId == request.PlayerId.RegionId
                            && rp.Replay.GameTime > new DateTime(2018, 1, 1)
                          select r;
            }
        }

        if (!request.Link)
        {
            replays = FilterCommanders(replays, request.Commanders);
            replays = FilterPlayers(replays, request.Players);
        }
        else
        {
            replays = FilterLinked(replays, request);
        }

        return replays;
    }

    private IQueryable<Replay> FilterLinked(IQueryable<Replay> replays, ReplaysRequest request)
    {
        var cmdrs = GetCommanders(request.Commanders);
        var players = request.Players.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var predicate = PredicateBuilder.New<Replay>();

        for (int i = 0; i < Math.Min(cmdrs.Count, players.Length); i++)
        {
            var cmdr = cmdrs[i];
            var player = players[i];

            predicate = predicate.And(replay =>
                replay.ReplayPlayers.Any(a =>
                    a.Race == cmdr && a.Name.Equals(player)));
        }

        if (cmdrs.Count > players.Length)
        {
            var remainingCmdrs = cmdrs.Skip(players.Length).Select(c => c.ToString());
            var cmdrsFilter = string.Join(' ', remainingCmdrs);
            replays = FilterCommanders(replays, cmdrsFilter);
        }

        if (players.Length > cmdrs.Count)
        {
            var remainingPlayers = players.Skip(cmdrs.Count);
            var playersFilter = string.Join(' ', remainingPlayers);
            replays = FilterPlayers(replays, playersFilter);
        }

        return replays.Where(predicate);
    }

    private IQueryable<Replay> FilterCommanders(IQueryable<Replay> replays, string commanders)
    {
        if (string.IsNullOrEmpty(commanders))
        {
            return replays;
        }

        var cmdrs = GetCommanders(commanders);

        if (cmdrs.Count == 0)
        {
            return replays;
        }

        var predicate = PredicateBuilder.New<Replay>();

        foreach (var cmdr in cmdrs)
        {
            predicate = predicate.And(replay =>
                replay.CommandersTeam1.Contains($"|{(int)cmdr}|") ||
                replay.CommandersTeam2.Contains($"|{(int)cmdr}|")
            );
        }

        replays = replays.Where(predicate);

        return replays;
    }

    private IQueryable<Replay> FilterPlayers(IQueryable<Replay> replays, string players)
    {
        if (string.IsNullOrEmpty(players))
        {
            return replays;
        }

        var names = players.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

        if (names.Count == 0)
        {
            return replays;
        }
        else if (names.Count == 1)
        {
            var name = names[0];
            replays = from r in replays
                      from rp in r.ReplayPlayers
                      where rp.Name == name
                      select r;
        }
        else
        {
            var name = names[0];
            replays = from r in replays
                      from rp in r.ReplayPlayers
                      where rp.Name == name
                      select r;

            for (int i = 1; i < names.Count; i++)
            {
                var iname = names[i];
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Name == iname));
            }
        }
        return replays.Distinct();

        //var predicate = PredicateBuilder.New<Replay>();

        //foreach (var player in playerEnts)
        //{
        //    // predicate = predicate.Or(replay => replay.ReplayPlayers.Any(rp => rp.Name.Contains(player)));
        //    predicate = predicate.And(replay => replay.ReplayPlayers.Any(rp => rp.Name.Equals(player)));
        //}

        //replays = replays.Where(predicate);

        //return replays;
    }

    private List<Commander> GetCommanders(string commanders)
    {
        var cmdrStrs = commanders.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        List<Commander> cmdrs = new();

        foreach (var cmdrStr in cmdrStrs)
        {
            if (Enum.TryParse(typeof(Commander), cmdrStr, ignoreCase: true, out var cmdrObj)
                && cmdrObj is Commander cmdr)
            {
                cmdrs.Add(cmdr);
            }
        }
        return cmdrs;
    }

    private IQueryable<Replay> SortReplays(ReplaysRequest request, IQueryable<Replay> replays)
    {
        if (request.Orders.Count == 0)
        {
            return replays.OrderByDescending(o => o.GameTime);
        }

        foreach (var order in request.Orders)
        {
            if (order.Property == "Group/Round")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.Round");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.Round");
                }
            }
            else if (order.Property == "Teams")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.WinnerTeam").AppendOrderBy("ReplayEvent.RunnerTeam");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.WinnerTeam").AppendOrderByDescending("ReplayEvent.RunnerTeam");
                }
            }
            else if (order.Property == "Event")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.Event.Name");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.Event.Name");
                }
            }
            else if (order.Property == "Exp2Win")
            {
                if (order.Ascending)
                {
                    replays = replays.OrderBy(o => o.ComboReplayRating == null ? 0 : o.ComboReplayRating.ExpectationToWin);
                }
                else
                {
                    replays = replays.OrderByDescending(o => o.ComboReplayRating == null ? 0 : o.ComboReplayRating.ExpectationToWin);
                }
            }
            else
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy(order.Property);
                }
                else
                {
                    replays = replays.AppendOrderByDescending(order.Property);
                }
            }
        }
        return replays;
    }
}
