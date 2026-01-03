using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        int playerId = importService.GetPlayerId(request.ToonId);

        var stats = await GetBasicPlayerStats(playerId, context, token);

        if (stats == null)
        {
            return new() { ToonId = request.ToonId };
        }

        var details = await GetRatingDetails(request, token);

        stats.RatingDetails.Add(details);

        return stats with { ToonId = request.ToonId };
    }

    //public async Task<RatingDetails> GetRatingDetails(PlayerStatsRequest request, CancellationToken token = default)
    //{
    //    Stopwatch sw = Stopwatch.StartNew();
    //    using var scope = scopeFactory.CreateScope();
    //    var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
    //    var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

    //    int playerId = importService.GetPlayerId(request.ToonId);
    //    RatingDetails ratingDetails = new() { RatingType = request.RatingType };

    //    ratingDetails.GameModes = await GetGameModeCounts(request, playerId, context, token);
    //    ratingDetails.Commanders = await GetCommanderCounts(request, playerId, context, token);
    //    ratingDetails.Ratings = await GetRatings(request, playerId, context, token);
    //    ratingDetails.Replays = await GetReplays(request, playerId, context, token);
    //    var avgGain = await GetCommandersPerformance(request, token);
    //    ratingDetails.AvgGainResponses.Add(avgGain);

    //    ratingDetails.TeammateStats = await GetPlayerIdPlayerTeammates(playerId, request.RatingType, context, token);
    //    ratingDetails.OpponentStats = await GetPlayerIdPlayerOpponents(playerId, request.RatingType, context, token);

    //    sw.Stop();
    //    logger.LogWarning("GetRatingDetails for PlayerId {PlayerId} took {ElapsedMilliseconds} ms", playerId, sw.ElapsedMilliseconds);
    //    return ratingDetails;
    //}

    private static async Task<List<GameModeCount>> GetGameModeCounts(PlayerStatsRequest request, int playerId, DsstatsContext context, CancellationToken token)
    {
        var gameModeGroup = from r in context.Replays
                            from rp in r.Players
                            where rp.PlayerId == playerId
                            group r by new { r.GameMode } into g
                            select new GameModeCount()
                            {
                                GameMode = g.Key.GameMode,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    private static async Task<List<CommanderCount>> GetCommanderCounts(PlayerStatsRequest request, int playerId, DsstatsContext context, CancellationToken token)
    {
        var comanderGroup = from r in context.Replays
                            from rp in r.Players
                            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                            where rp.PlayerId == playerId
                                && rr.RatingType == request.RatingType
                            group rp by rp.Race into g
                            select new CommanderCount()
                            {
                                Commander = g.Key,
                                Count = g.Count(),
                            };
        return await comanderGroup.ToListAsync(token);
    }

    private static async Task<List<ReplayListDto>> GetReplays(PlayerStatsRequest request, int playerId, DsstatsContext context, CancellationToken token)
    {
        var query = from r in context.Replays
                    from rp in r.Players.Where(x => x.PlayerId == playerId)
                    from rr in context.ReplayRatings
                        .Where(x => x.ReplayId == r.ReplayId && x.RatingType == request.RatingType)
                        .DefaultIfEmpty()
                    orderby r.Gametime descending
                    select new ReplayList()
                    {
                        ReplayHash = r.ReplayHash,
                        GameTime = r.Gametime,
                        GameMode = r.GameMode,
                        Duration = r.Duration,
                        WinnerTeam = r.WinnerTeam,
                        Players = r.Players.Select(s => new ReplayPlayerList()
                        {
                            Race = s.Race,
                            Team = s.TeamId,
                        }).ToList(),
                        RatingList = rr == null ? null : new()
                        {
                            Exp2Win = rr.ExpectedWinProbability,
                            AvgRating = rr.AvgRating,
                            LeaverType = rr.LeaverType,
                        },
                        PlayerPos = rp.GamePos,
                    };
        var replays = await query.Take(10).ToListAsync(token);
        return replays.Select(s => s.GetDto()).ToList();
    }

    private static async Task<List<RatingAtDateTime>> GetRatings(PlayerStatsRequest request, int playerId, DsstatsContext context, CancellationToken token)
    {
        var query = from rp in context.ReplayPlayers.Where(x => x.PlayerId == playerId)
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, RatingType = rr.RatingType }
                        equals new { rpr.ReplayPlayerId, rpr.RatingType }
                    where rr.RatingType == request.RatingType
                    group new { r, rpr } by new { r.Gametime.Year, Week = context.Week(r.Gametime) } into g
                    select new RatingAtDateTime()
                    {
                        Year = g.Key.Year,
                        Week = g.Key.Week,
                        Rating = (float)Math.Round(g.Average(a => a.rpr.RatingBefore + a.rpr.RatingDelta), 2),
                        Games = g.Max(m => m.rpr.Games)
                    };
        return await query.ToListAsync(token);
    }

    private static async Task<PlayerStatsResponse?> GetBasicPlayerStats(int playerId, DsstatsContext context, CancellationToken token)
    {
        return await context.Players
            .Where(x => x.PlayerId == playerId)
            .Select(x => new PlayerStatsResponse()
            {
                Name = x.Name,
                RegionId = x.ToonId.Region,
                Ratings = x.Ratings.Select(s => new PlayerRatingListItem()
                {
                    RatingType = s.RatingType,
                    PlayerId = s.PlayerId,
                    RegionId = s.Player!.ToonId.Region,
                    Name = s.Player!.Name,
                    Pos = s.Position,
                    Games = s.Games,
                    Wins = s.Wins,
                    Mvps = s.Mvps,
                    Change = s.Change,
                    Main = s.Main,
                    MainCount = s.MainCount,
                    Rating = s.Rating,
                    Cons = s.Consistency,
                    Conf = s.Confidence,
                }).ToList(),
            })
            .FirstOrDefaultAsync(token);
    }

    public async Task<CmdrAvgGainResponse> GetCommandersPerformance(PlayerStatsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        int playerId = importService.GetPlayerId(request.ToonId);
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, rr.RatingType }
                        equals new { rpr.ReplayPlayerId, rpr.RatingType }
                    where p.PlayerId == playerId
                        && r.Gametime >= timeInfo.Start
                        && (!timeInfo.HasEnd || rp.Replay!.Gametime < timeInfo.End)
                        && rr.RatingType == request.RatingType
                    group new
                    {
                        rp,
                        rpr
                    } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.Result == PlayerResult.Win)
                    };

        var items = await group.ToListAsync(token);
        return new()
        {
            TimePeriod = request.TimePeriod,
            AvgGains = items
        };
    }

    private static async Task<List<OtherPlayerStats>> GetPlayerIdPlayerTeammates(int playerId, RatingType ratingType, DsstatsContext context, CancellationToken token)
    {
        var teammateGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from t in rp.Replay!.Players
                            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                            join rpr in context.ReplayPlayerRatings
                                on new { rp.ReplayPlayerId, rr.RatingType }
                                equals new { rpr.ReplayPlayerId, rpr.RatingType }
                            where p.PlayerId == playerId
                                && rr.RatingType == ratingType
                            where t != rp && t.TeamId == rp.TeamId
                            group new { t, rpr } by new { t.Player!.ToonId, t.Player.Name } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                ToonId = new ToonIdDto() { Id = g.Key.ToonId.Id, Realm = g.Key.ToonId.Realm, Region = g.Key.ToonId.Region },
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.t.Result == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.rpr.RatingDelta), 2)
                            };


        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new OtherPlayerStats()
        {
            Player = new()
            {
                ToonId = s.ToonId,
                Name = s.Name
            },
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = (float)s.AvgGain
        }).ToList();
    }

    private static async Task<List<OtherPlayerStats>> GetPlayerIdPlayerOpponents(int playerId, RatingType ratingType, DsstatsContext context, CancellationToken token)
    {
        var opponentGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from t in rp.Replay!.Players
                            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                            join opponentRpr in context.ReplayPlayerRatings
                                on new { t.ReplayPlayerId, rr.RatingType }
                                equals new { opponentRpr.ReplayPlayerId, opponentRpr.RatingType }
                            where p.PlayerId == playerId
                                && rr.RatingType == ratingType
                            where t.TeamId != rp.TeamId
                            group new { t, opponentRpr } by new { t.Player!.ToonId, t.Player.Name } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                ToonId = new ToonIdDto() { Id = g.Key.ToonId.Id, Realm = g.Key.ToonId.Realm, Region = g.Key.ToonId.Region },
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.t.Result == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.opponentRpr.RatingDelta), 2)
                            };

        var results = await opponentGroup
            .ToListAsync(token);


        return results.Select(s => new OtherPlayerStats()
        {
            Player = new()
            {
                ToonId = s.ToonId,
                Name = s.Name
            },
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = (float)s.AvgGain
        }).ToList();
    }
}

internal class PlayerTeamResultHelper
{
    public ToonIdDto ToonId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
}