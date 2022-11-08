using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<MmrService> logger;

    public MmrService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MmrService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    private static readonly double eloK = 128; // default 32
    private static readonly double eloK_mult = 12.5;
    private static readonly double clip = eloK * eloK_mult;
    public static readonly double startMmr = 1000.0;
    private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;

    private readonly Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();
    private readonly Dictionary<int, float> replayPlayerMmrChanges = new();
    private double maxMmr;

    public async Task ReCalculate(DateTime startTime)
    {
        await ClearRatingsInDb();
        
        await ResetGlobals();

        await CalculateCmdr(startTime);

        //await CalculateStd(startTime);
    }

    private async Task ResetGlobals()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        commanderRatings = await context.CommanderMmrs //SeedCommanders has to be called before!
            .AsNoTracking()
            .ToListAsync();

        playerRatingsCmdr.Clear();
        playerRatingsStd.Clear();
        replayPlayerMmrChanges.Clear();
    }

    private async Task ClearRatingsInDb()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.ReplayPlayers)} SET {nameof(ReplayPlayer.MmrChange)} = NULL");

        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.Mmr)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStd)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrOverTime)} = NULL");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStdOverTime)} = NULL");

        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.SynergyMmr)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyMmr)} = {startMmr}");
    }


    private static string? GetOverTimeRating(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0) {
            return null;
        } else if (dsRCheckpoints.Count == 1) {
            return $"{Math.Round(dsRCheckpoints[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2) {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++) {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr) {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999) {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static void FixMmr_Equality(double[] team1_mmrDelta, double[] team2_mmrDelta)
    {
        double abs_sumTeam1_mmrDelta = Math.Abs(team1_mmrDelta.Sum());
        double abs_sumTeam2_mmrDelta = Math.Abs(team2_mmrDelta.Sum());

        for (int i = 0; i < team1_mmrDelta.Length; i++) {
            team1_mmrDelta[i] = team1_mmrDelta[i] *
                ((abs_sumTeam1_mmrDelta + abs_sumTeam2_mmrDelta) / (abs_sumTeam1_mmrDelta * 2));
            team2_mmrDelta[i] = team2_mmrDelta[i] *
                ((abs_sumTeam2_mmrDelta + abs_sumTeam1_mmrDelta) / (abs_sumTeam2_mmrDelta * 2));

            if (abs_sumTeam1_mmrDelta == 0 || abs_sumTeam2_mmrDelta == 0) {
                team1_mmrDelta[i] = 0;
                team2_mmrDelta[i] = 0;
            }
        }
    }
}


