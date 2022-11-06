using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    }

    private async Task ResetGlobals()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        commanderRatings = await context.CommanderMmrs
            .AsNoTracking()
            .ToListAsync();

        playerRatingsCmdr.Clear();
        playerRatingsStd.Clear();
        replayPlayerMmrChanges.Clear();

        maxMmr = startMmr;
    }

    private async Task ClearRatingsInDb()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.Mmr)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStd)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrOverTime)} = NULL");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStdOverTime)} = NULL");

        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.SynergyMmr)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyMmr_1)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyMmr_2)} = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyElo_1)} = 0.5");
        await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyElo_2)} = 0.5");
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }
}


