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
    public static Dictionary<int, PlayerRatingDto> ToonIdRatings { get; private set; } = new();
    public static Dictionary<int, string> ToonIdCmdrRatingOverTime { get; private set; } = new();
    public static Dictionary<int, float> ReplayPlayerMmrChanges { get; private set; } = new();


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
    private static DateTime LatestReplayGameTime = DateTime.MinValue;

    private static bool useCommanderMmr = false;
    private static bool useConsistency = true;
    private static bool useFactorToTeamMates = true;
    SemaphoreSlim ss = new(1, 1);

    //public async Task ReCalculate(DateTime startTime, DateTime endTime)
    //{
    //    Stopwatch sw = Stopwatch.StartNew();

    //    await ClearRatingsInDb();

    //    await ResetGlobals();

    //    var playerRatingsCmdr = await CalculateCmdr(startTime, endTime);
    //    await SaveCommanderData();
    //    await SavePlayersData(playerRatingsCmdr);
    //    await SaveReplayPlayersData(replayPlayerMmrChanges);

    //    // (var playerRatingsStd, var replayPlayerMmrChanges) = await CalculateStd(startTime);
    //    // await SavePlayersData(playerRatingsStd);
    //    // await SaveReplayPlayersData(replayPlayerMmrChanges);

    //    sw.Stop();
    //    OnRecalculated(new() { Duration = sw.Elapsed });
    //}

    public async Task ReCalculateWithDictionary(DateTime startTime, DateTime endTime)
    {
        await ss.WaitAsync();
        try {
            Stopwatch sw = Stopwatch.StartNew();

            await ResetGlobals();

            var playerRatingsCmdr = await ReCalculateCmdr(startTime, endTime);
            var playerInfos = await GetPlayerInfos();

            await SetGlobals(playerRatingsCmdr, playerInfos);

            //var json = JsonSerializer.Serialize(PlayerIdRatings);
            //File.WriteAllText("/data/ds/playeridratings.json", json);

            sw.Stop();
            logger.LogInformation($"recalcualated in {sw.ElapsedMilliseconds} ms");
            OnRecalculated(new() { Duration = sw.Elapsed });
        }
        finally {
            ss.Release();
        }
    }

    public async Task<bool> ContinueCalculateWithDictionary(List<Replay> newReplays)
    {
        if (newReplays.Any(x => x.GameTime < LatestReplayGameTime)) {
            //ReCalculateWithDictionary(startTime, DateTime.Today.AddDays(1));
            return false;
        }

        var newReplaysCmdr = newReplays.Where(x => x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic)
            .Select(s => mapper.Map<ReplayDsRDto>(s)).ToList();

        await ss.WaitAsync();
        try {
            Stopwatch sw = Stopwatch.StartNew();

            var playerRatingsCmdr = ContinueCalculateCmdr(GetPlayerRatingsCmdr(), newReplaysCmdr);
            var playerInfos = await GetPlayerInfos();

            await ContinueGlobals(playerRatingsCmdr, playerInfos);

            //var json = JsonSerializer.Serialize(PlayerIdRatings);
            //File.WriteAllText("/data/ds/playeridratings.json", json);

            sw.Stop();
            logger.LogInformation($"continue calculation in {sw.ElapsedMilliseconds} ms");
            OnRecalculated(new() { Duration = sw.Elapsed });
        } finally {
            ss.Release();
        }

        return true;
    }

    private static Dictionary<int, List<DsRCheckpoint>> GetPlayerRatingsCmdr()
    {
        return new();
    }

    private async Task SetGlobals(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, Dictionary<int, PlayerInfoDto> playerInfos)
    {
        ToonIdRatings.Clear();
        ToonIdCmdrRatingOverTime.Clear();

        var toonIdPlayerIdMap = await GetToonIdPlayerIdMap();

        for (int i = 0; i < playerInfos.Count; i++) {
            var playerInfo = playerInfos.ElementAt(i);
            int playerId = 0;
            string name = "";
            if (toonIdPlayerIdMap.ContainsKey(playerInfo.Key)) {
                var tpMap = toonIdPlayerIdMap[playerInfo.Key];
                playerId = tpMap.Key;
                name = tpMap.Value;
            }

            MmrInfo? mmrInfo = null;
            if (playerId > 0 && playerRatingsCmdr.ContainsKey(playerId)) {
                var plRat = playerRatingsCmdr[playerId];
                mmrInfo = new() {
                    CmdrMmr = plRat.LastOrDefault()?.Mmr ?? 0,
                    CmdrOverTime = GetOverTimeRating(plRat) ?? ""
                };
                ToonIdCmdrRatingOverTime[playerInfo.Key] = mmrInfo.CmdrOverTime;
            }

            ToonIdRatings[playerInfo.Key] = new PlayerRatingDto() {
                PlayerId = playerId,
                Name = name,
                ToonId = playerInfo.Key,
                Mmr = mmrInfo?.CmdrMmr ?? 0,
                GamesCmdr = playerInfo.Value.GamesCmdr,
                WinsCmdr = playerInfo.Value.WinsCmdr,
                MvpCmdr = playerInfo.Value.MvpCmdr,
                TeamGamesCmdr = playerInfo.Value.TeamGamesCmdr,
                GamesStd = playerInfo.Value.GamesStd,
                WinsStd = playerInfo.Value.WinsStd,
                MvpStd = playerInfo.Value.MvpStd,
                TeamGamesStd = playerInfo.Value.TeamGamesStd,
            };
        }
    }
    private async Task ContinueGlobals(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, Dictionary<int, PlayerInfoDto> playerInfos)
    {
        
    }

    private async Task<Dictionary<int, KeyValuePair<int, string>>> GetToonIdPlayerIdMap()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return (await context.Players
            .Select(s => new { s.PlayerId, s.ToonId, s.Name })
            .ToListAsync())
            .ToDictionary(k => k.ToonId, v => new KeyValuePair<int, string>(v.PlayerId, v.Name));
    }

    private async Task ResetGlobals()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var commanderRatings = await context.CommanderMmrs //SeedCommanders has to be called before!
            .AsNoTracking()
            .ToListAsync();

        cmdrMmrDic = commanderRatings.ToDictionary(k => new CmdrMmmrKey() { Race = k.Race, Opprace = k.OppRace }, v => v);

        ReplayPlayerMmrChanges.Clear();
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
            if (i % 500 == 0)
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
            if (!playerRatingsCmdr.ContainsKey(GetMmrId(replayPlayer.Player)))
            {
                playerRatingsCmdr[GetMmrId(replayPlayer.Player)] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatingsCmdr[GetMmrId(replayPlayer.Player)].Last().Mmr;
            }
        }
        return teamMmr / 3.0;
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        //todo
        return player.PlayerId;
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

public record MmrInfo
{
    public double CmdrMmr { get; set; }
    public double StdMmr { get; set; }
    public string CmdrOverTime { get; set; } = string.Empty;
    public string StdOverTime { get; set; } = string.Empty;
}