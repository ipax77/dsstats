using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;



public partial class PlayerService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMapper mapper;
    private readonly ILogger<PlayerService> logger;

    public PlayerService(IServiceScopeFactory scopeFactory, IMapper mapper, ILogger<PlayerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<PlayerDetailSummary> GetPlayerSummary(int toonId, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return new()
        {
            GameModesPlayed = await GetGameModeCounts(context, toonId, token),
            Ratings = await GetRatings(context, toonId, token),
            Commanders = await GetCommandersPlayed(context, toonId, token)
        };
    }

    private static async Task<List<CommanderInfo>> GetCommandersPlayed(ReplayContext context, int toonId, CancellationToken token)
    {
        return await (from p in context.Players
                    from rp in p.ReplayPlayers
                    where p.ToonId == toonId
                    group rp by rp.Race into g
                    select new CommanderInfo()
                    {
                        Cmdr = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync(token);
    }

    private async Task<List<PlayerRatingDetailDto>> GetRatings(ReplayContext context, int toonId, CancellationToken token)
    {
        return await context.PlayerRatings
                .Where(x => x.Player.ToonId == toonId)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);
    }

    private static async Task<List<PlayerGameModeResult>> GetGameModeCounts(ReplayContext context, int toonId, CancellationToken token)
    {
        var gameModeGroup = from r in context.Replays
                            from rp in r.ReplayPlayers
                            where rp.Player.ToonId == toonId
                            group r by new { r.GameMode, r.Playercount } into g
                            select new PlayerGameModeResult()
                            {
                                GameMode = g.Key.GameMode,
                                PlayerCount = g.Key.Playercount,
                                Count = g.Count(),
                            };
        return await gameModeGroup.ToListAsync(token);
    }

    public async Task<PlayerDetailResponse> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        PlayerDetailResponse response = new();

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if ((int)request.TimePeriod < 3)
        {
            request.TimePeriod = TimePeriod.Past90Days;
        }

        response.CmdrStrengthItems = await GetCmdrStrengthItems(context, request, token);

        return response;
    }

    private async Task<List<CmdrStrengthItem>> GetCmdrStrengthItems(ReplayContext context, PlayerDetailRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.LeaverType == LeaverType.None
                && x.ReplayRatingInfo.RatingType == request.RatingType);

        if (endDate != DateTime.MinValue && (DateTime.Today - endDate).TotalDays > 2)
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = request.Interest == Commander.None
            ?
                from r in replays
                from rp in r.ReplayPlayers
                where rp.Player.ToonId == request.RequestNames.ToonId
                group rp by rp.Race into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                }
            :
                from r in replays
                from rp in r.ReplayPlayers
                where rp.Player.ToonId == request.RequestNames.ToonId
                    && rp.Race == request.Interest
                group rp by rp.OppRace into g
                select new CmdrStrengthItem()
                {
                    Commander = g.Key,
                    Matchups = g.Count(),
                    AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                    AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                    Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                }
        ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var items = await group.ToListAsync(token);

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            items = items.Where(x => (int)x.Commander > 3).ToList();
        }
        else if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            items = items.Where(x => (int)x.Commander <= 3).ToList();
        }
        return items;
    }
}

public static class PlayerServiceDeprecated
{
    public static async void GetExpectationCount(ReplayContext context)
    {
        int toonId = 226401; // PAX
        // int toonId = 1488340; // Feralan
        // int toonId = 8509078; // Firestorm

        var select = from r in context.Replays
                     from rp in r.ReplayPlayers
                     where rp.Player.ToonId == toonId
                      && r.ReplayRatingInfo != null
                      && r.ReplayRatingInfo.RatingType == shared.RatingType.CmdrTE && r.ReplayRatingInfo.LeaverType == LeaverType.None
                     select r;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replays = await select
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Include(i => i.ReplayRatingInfo)
                .ThenInclude(i => i.RepPlayerRatings)
            .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        List<double> expectations = new();

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            if (replay.ReplayRatingInfo == null)
            {
                continue;
            }

            var team1Mmr = replay.ReplayRatingInfo.RepPlayerRatings
                .Where(x => x.GamePos <= 3)
                .Sum(s => s.Rating - s.RatingChange);

            var team2Mmr = replay.ReplayRatingInfo.RepPlayerRatings
                .Where(x => x.GamePos > 3)
                .Sum(s => s.Rating - s.RatingChange);

            var replayPlayer = replay.ReplayPlayers.First(x => x.Player.ToonId == toonId);
            var playerTeam = replayPlayer.GamePos <= 3 ? 1 : 2;

            double expectationToWin;

            if (playerTeam == 1)
            {
                expectationToWin = EloExpectationToWin(team1Mmr, team2Mmr);
            }
            else
            {
                expectationToWin = EloExpectationToWin(team2Mmr, team1Mmr);
            }
            expectations.Add(expectationToWin);
        }

        Console.WriteLine($"AvgExpectationToWin: {Math.Round(expectations.Average(), 2)}");
        Console.WriteLine($"below 60 ExpectationToWins: {Math.Round(expectations.Where(x => x <= 0.6).Count() * 100 / (double)expectations.Count, 2)}");

    }
    public static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip = 1600)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

}
