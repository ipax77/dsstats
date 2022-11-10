using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;
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

    public event EventHandler<MmrRecalculatedEvent>? Recalculated;
    protected virtual void OnRecalculated(MmrRecalculatedEvent e)
    {
        EventHandler<MmrRecalculatedEvent>? handler = Recalculated;
        handler?.Invoke(this, e);
    }
    private static readonly double eloK = 64 * 3; // default 32
    private static readonly double eloK_mult = 12.5 / 3;
    private static readonly double clip = eloK * eloK_mult;
    public static readonly double startMmr = 1000.0;
    private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;
    private Dictionary<CmdrMmmrKey, CommanderMmr> cmdrMmrDic = new();

    // todo get rid of it
    private readonly Dictionary<int, float> replayPlayerMmrChanges = new();

    private static bool useCommanderMmr = false;
    private static bool useConsistency = true;
    private static bool useFactorToTeamMates = true;

    public async Task ReCalculate(DateTime startTime)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await ClearRatingsInDb();

        await ResetGlobals();

        var playerRatingsCmdr = await CalculateCmdr(startTime);
        await SaveCommanderData();
        await SavePlayersData(playerRatingsCmdr);
        await SaveReplayPlayersData(replayPlayerMmrChanges);

        // (var playerRatingsStd, var replayPlayerMmrChanges) = await CalculateStd(startTime);
        // await SavePlayersData(playerRatingsStd);
        // await SaveReplayPlayersData(replayPlayerMmrChanges);

        sw.Stop();
        OnRecalculated(new() { Duration = sw.Elapsed });
    }

    public async Task ContinueCalculate(DateTime startTime) //ToDo - load maxMmr's for continuation!!!
    {
        Stopwatch sw = Stopwatch.StartNew();
        await ResetGlobals();
        
        var playerRatingsCmdr = await CalculateCmdr(startTime);
        await SaveCommanderData();
        await SavePlayersData(playerRatingsCmdr);
        await SaveReplayPlayersData(replayPlayerMmrChanges);

        // (var playerRatingsStd, var replayPlayerMmrChanges) = await CalculateStd(startTime);
        // await SavePlayersData(playerRatingsStd);
        // await SaveReplayPlayersData(replayPlayerMmrChanges);

        sw.Stop();
        OnRecalculated(new() { Duration = sw.Elapsed });
    }

    public async Task ReCalculateWithTimes(DateTime startTime)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await ClearRatingsInDb();
        sw.Stop();
        logger.LogWarning($"cleared db in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        await ResetGlobals();
        sw.Stop();
        logger.LogWarning($"reset globals in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        var playerRatingsCmdr = await CalculateCmdr(startTime);
        sw.Stop();
        logger.LogWarning($"calculated in {sw.ElapsedMilliseconds} ms");
        sw.Restart();

        //await CalculateStd(startTime);

        await SavePlayersData(playerRatingsCmdr);
        sw.Stop();
        logger.LogWarning($"players saved in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        await SaveCommanderData();
        sw.Stop();
        logger.LogWarning($"cmdrs saved in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        await SaveReplayPlayersData(replayPlayerMmrChanges);
        sw.Stop();
        logger.LogWarning($"replayplayers saved in {sw.ElapsedMilliseconds} ms");
        sw.Restart();

        // var playerRatingsStd = await CalculateStd(startTime);
        // await SavePlayersData(playerRatingsStd);

        sw.Stop();
        OnRecalculated(new() { Duration = sw.Elapsed });
    }

    private async Task ResetGlobals()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var commanderRatings = await context.CommanderMmrs //SeedCommanders has to be called before!
            .AsNoTracking()
            .ToListAsync();

        cmdrMmrDic = commanderRatings.ToDictionary(k => new CmdrMmmrKey() { Race = k.Race, Opprace = k.OppRace }, v => v);

        replayPlayerMmrChanges.Clear();
    }

    private async Task SaveReplayPlayersData(Dictionary<int, float> replayPlayerMmrChanges)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var ent in replayPlayerMmrChanges)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.ReplayPlayers)}" +
                $" SET {nameof(ReplayPlayer.MmrChange)} = {ent.Value.ToString(CultureInfo.InvariantCulture)}" +
                $" WHERE {nameof(ReplayPlayer.ReplayPlayerId)} = {ent.Key}; ");
            i++;
            if (i % 1000 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    private async Task SavePlayersData(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var ent in playerRatingsCmdr)
        {
            var lastRating = ent.Value.Last();
            var mmr = lastRating.Mmr == double.NaN ? "0" : lastRating.Mmr.ToString(CultureInfo.InvariantCulture);

            sb.Append($"UPDATE {nameof(ReplayContext.Players)}" +
                $" SET {nameof(Player.Mmr)} = {mmr}, {nameof(Player.MmrOverTime)} = '{GetOverTimeRating(ent.Value)}'" +
                $" WHERE {nameof(Player.PlayerId)} = {ent.Key}; ");

            i++;
            if (i % 1000 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }


        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    private async Task SaveCommanderData()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var commanderMmrs = await context.CommanderMmrs.ToListAsync();
        foreach (var commanderMmr in commanderMmrs)
        {
            var commanderRating = cmdrMmrDic[new CmdrMmmrKey() { Race = commanderMmr.Race, Opprace = commanderMmr.OppRace }];

            commanderMmr.SynergyMmr = commanderRating.SynergyMmr;
            commanderMmr.AntiSynergyMmr = commanderRating.AntiSynergyMmr;
        }

        await context.SaveChangesAsync();
    }


    private static string? GetOverTimeRating(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0)
        {
            return null;
        }
        else if (dsRCheckpoints.Count == 1)
        {
            return $"{Math.Round(dsRCheckpoints[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2)
        {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++)
            {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999)
        {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static double PlayerToTeamMates(double teamMmr, double playerMmr)
    {
        if (teamMmr < 1)
        {
            return 1.0;
        }

        return (playerMmr / teamMmr);
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double mcv)
    {
        return (double)(eloK * mcv * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
        //return ((1 - consistencyImpact) + (consistencyImpact * raw_revConsistency)); //Equal to above
    }

    private static double GetTeamMmr(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayPlayerDsRDto[] replayPlayers, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var replayPlayer in replayPlayers)
        {
            if (!playerRatingsCmdr.ContainsKey(replayPlayer.Player.PlayerId))
            {
                playerRatingsCmdr[replayPlayer.Player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatingsCmdr[replayPlayer.Player.PlayerId].Last().Mmr;
            }
        }
        return teamMmr / 3.0;
    }

    private static void FixMmrEquality(TeamData teamData, TeamData oppTeamData)
    {
        double absSumTeamMmrDelta = teamData.PlayersMmrDelta.Sum();
        double absSumOppTeamMmrDelta = oppTeamData.PlayersMmrDelta.Sum();
        double absSumMmrAllDelta = absSumTeamMmrDelta + absSumOppTeamMmrDelta;

        if (teamData.Players.Length != oppTeamData.Players.Length)
        {
            throw new Exception("Not same player amount.");
        }

        for (int i = 0; i < teamData.Players.Length; i++)
        {
            teamData.PlayersMmrDelta[i] = teamData.PlayersMmrDelta[i] *
                ((absSumMmrAllDelta) / (absSumTeamMmrDelta * 2));

            oppTeamData.PlayersMmrDelta[i] = oppTeamData.PlayersMmrDelta[i] *
                ((absSumMmrAllDelta) / (absSumOppTeamMmrDelta * 2));
        }
    }
}


internal record CmdrMmmrKey
{
    public Commander Race { get; init; }
    public Commander Opprace { get; init; }
}

public class MmrRecalculatedEvent : EventArgs
{
    public TimeSpan Duration { get; set; }
}