using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class MlnetService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<MlnetService> logger;

    public MlnetService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MlnetService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task DetermineCmdrStrength()
    {
        var replays = await GetCmdrStrengthData();

        logger.LogInformation($"got {replays.Count} replays.");

        var group = from r in replays
                    from rp in r.ReplayPlayers
                    group rp by rp.Race into g
                    select new
                    {
                        g.Key,
                        Count = g.Count(),
                        Sum = g.Sum(x => x.ReplayPlayerRatingInfo?.Rating),
                        Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                    };

        Dictionary<Commander, double> cmdrAvgPlayerEloRating =
            group.ToDictionary(k => k.Key, v => v.Count == 0 || v.Sum == null ? 0 : (double)(v.Sum / v.Count));

        Dictionary<Commander, double> cmdrWinrate =
            group.ToDictionary(k => k.Key, v => v.Count == 0 ? 0 : (double)(v.Wins * 100.0 / v.Count));


        StringBuilder sb = new();
        sb.AppendLine("Commander => AvgPlayerEloRating, Winrate, OthersEloRating");

        foreach (var cmdr in cmdrAvgPlayerEloRating.OrderByDescending(o => o.Value))
        {
            var othersSum = cmdrAvgPlayerEloRating
                .Where(x => x.Key != cmdr.Key)
                .Sum(s => s.Value);

            sb.AppendLine($"{cmdr.Key} => {Math.Round(cmdr.Value, 2)}, {Math.Round(cmdrWinrate[cmdr.Key], 2)}%, {Math.Round(othersSum / cmdrAvgPlayerEloRating.Count - 1, 2)}");
        }

        logger.LogInformation(sb.ToString());
    }

    public async Task DetermineCmdrStrength2()
    {
        var replays = await GetCmdrStrengthData();

        Dictionary<Commander, CmdrStrengthHelper> cmdrStrengthDic = new();

        foreach (Commander cmdr in Data.GetCommanders(Data.CmdrGet.NoStd))
        {
            cmdrStrengthDic[cmdr] = new() { Commander = cmdr };
        }

        foreach (var replay in replays)
        {
            var cmdrs = replay.ReplayPlayers.Select(s => s.Race).Distinct().ToList();
            foreach (var cmdr in cmdrs)
            {
                cmdrStrengthDic[cmdr].Replays++;
            }

            foreach (var replayPlayer in replay.ReplayPlayers)
            {
                var cmdrStrength = cmdrStrengthDic[replayPlayer.Race];

                cmdrStrength.Matchups++;
                if (replayPlayer.PlayerResult == PlayerResult.Win)
                {
                    cmdrStrength.Wins++;
                }

                cmdrStrength.RatingSum += replayPlayer.ReplayPlayerRatingInfo?.Rating ?? 0;


            }
        }
    }

    public async Task<CmdrStrengthResult> GetCmdrStrengthResults(RatingType ratingType, DateTime startDate, DateTime endDate)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.LeaverType == LeaverType.None
                && x.ReplayRatingInfo.RatingType == ratingType);

        if (endDate != DateTime.MinValue && (DateTime.Today - endDate).TotalDays > 2)
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = from r in replays
                    from rp in r.ReplayPlayers
                    group rp by rp.Race into g
                    select new CmdrStrengthItem()
                    {
                        Commander = g.Key,
                        Matchups = g.Count(),
                        AvgRating = Math.Round(g.Sum(s => s.ReplayPlayerRatingInfo.Rating) / g.Count(), 2),
                        Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                    };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return new()
        {
            Items = await group.ToListAsync()
        };
    }

    private async Task<List<ReplayCmdrDto>> GetCmdrStrengthData()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Where(x => x.GameTime > new DateTime(2023, 1, 1)
                && x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.LeaverType == LeaverType.None
                && x.ReplayRatingInfo.RatingType == RatingType.Cmdr)
            .ProjectTo<ReplayCmdrDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}




public record CmdrStrengthHelper
{
    public Commander Commander { get; set; }
    public int Replays { get; set; }
    public int Matchups { get; set; }
    public int Wins { get; set; }
    public int OtherMatchups { get; set; }
    public double RatingSum { get; set; }
    public double OthersSum { get; set; }
}

public record ReplayCmdrDto
{
    public DateTime GameTime { get; init; }
    public int WinnerTeam { get; init; }
    public List<ReplayPlayerCmdrDto> ReplayPlayers { get; init; } = new();
    public ReplayRatingCmdrDto? ReplayRatingInfo { get; init; }
}

public record ReplayPlayerCmdrDto
{
    public int GamePos { get; init; }
    public int Team { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public PlayerResult PlayerResult { get; init; }
    public RepPlayerRatingCmdrDto? ReplayPlayerRatingInfo { get; init; }
}

public record ReplayRatingCmdrDto
{
    public RatingType RatingType { get; init; }
    public LeaverType LeaverType { get; init; }
}

public record RepPlayerRatingCmdrDto
{
    public int GamePos { get; init; }
    public float Rating { get; init; }
}