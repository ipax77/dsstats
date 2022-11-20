using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    public Dictionary<int, PlayerRatingDto> ToonIdRatings { get; private set; } = new();
    public Dictionary<int, string> ToonIdCmdrRatingOverTime { get; private set; } = new();
    public Dictionary<int, string> ToonIdStdRatingOverTime { get; private set; } = new();
    public Dictionary<int, float> ReplayPlayerMmrChanges { get; private set; } = new();


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

    private static readonly double eloK = 64;//64; // default 32
    private static readonly double eloK_mult = 12.5;
    private static readonly double clip = eloK * eloK_mult; //shouldn't be bigger than startMmr!
    public static readonly double startMmr = 1000.0;

    private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;
    private static readonly double confidenceImpact = 0.90;
    private static readonly double confidenceDeltaMult = 1.00;

    private Dictionary<CmdrMmmrKey, CommanderMmr> cmdrMmrDic = new();
    private DateTime LatestReplayGameTime = DateTime.MinValue;

    private readonly bool useCommanderMmr = false;
    private readonly bool useConsistency = true;
    private readonly bool useConfidence = true;
    private readonly bool useFactorToTeamMates = false;
    SemaphoreSlim ss = new(1, 1);

    public async Task ReCalculateWithDictionary(DateTime startTime = new(), DateTime endTime = new())
    {
        await ss.WaitAsync();
        IsProgressActive = false;
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            await ClearRatingsInDb();

            await ResetGlobals();

            var playerRatingsCmdr = await ReCalculateCmdr(startTime, endTime);
            var playerRatingsStd = await ReCalculateStd(startTime, endTime);

            var playerInfos = await GetPlayerInfos();

            await SetGlobals(playerRatingsCmdr, playerRatingsStd, playerInfos);

            await SaveCommanderData();

            sw.Stop();
            logger.LogInformation($"recalcualated in {sw.ElapsedMilliseconds} ms");
            OnRecalculated(new() { Duration = sw.Elapsed });
        }
        finally
        {
            ss.Release();
        }
    }

    private async Task SetGlobals(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr,
                              Dictionary<int, List<DsRCheckpoint>> playerRatingsStd,
                              Dictionary<int, PlayerInfoDto> playerInfos)
    {
        ToonIdRatings.Clear();
        ToonIdCmdrRatingOverTime.Clear();
        ToonIdStdRatingOverTime.Clear();

        var playerIdToonIdMap = await GetPlayerIdToonIdMap();

        foreach (var playerRatingEnt in playerRatingsCmdr.Concat(playerRatingsStd))
        {
            int playerId = playerRatingEnt.Key;

            int toonId = 0;
            string name = "";
            int regionId = 0;

            if (playerIdToonIdMap.ContainsKey(playerId))
            {
                var playerMap = playerIdToonIdMap[playerId];
                toonId = playerMap.ToonId;
                name = playerMap.Name;
                regionId = playerMap.RegionId;
            }

            PlayerInfoDto playerInfo;
            if (playerInfos.ContainsKey(toonId))
            {
                playerInfo = playerInfos[toonId];
            }
            else
            {
                playerInfo = new PlayerInfoDto()
                {
                    ToonId = toonId
                };
                playerInfos[toonId] = playerInfo;
            }

            MmrInfo? mmrInfoCmdr = GetMmrInfo(ToonIdCmdrRatingOverTime, playerRatingsCmdr, toonId, playerId);
            MmrInfo? mmrInfoStd = GetMmrInfo(ToonIdStdRatingOverTime, playerRatingsStd, toonId, playerId);
            
            if (mmrInfoCmdr == null && mmrInfoStd == null)
            {
                continue;
            }

            ToonIdRatings[toonId] = new PlayerRatingDto()
            {
                PlayerId = playerId,
                Name = name,
                ToonId = toonId,
                RegionId = regionId,
                Main = playerInfo.Main,
                MainPercentage = playerInfo.MainPercentage,

                CmdrRatingStats = new()
                {
                    MmrGames = mmrInfoCmdr?.MmrGames ?? 0,
                    Mmr = mmrInfoCmdr?.Mmr ?? startMmr,
                    Games = playerInfo.GamesCmdr,
                    Wins = playerInfo.WinsCmdr,
                    Mvp = playerInfo.MvpCmdr,
                    TeamGames = playerInfo.TeamGamesCmdr,
                    Consistency = mmrInfoCmdr?.Consistency ?? 0,
                    Confidence = mmrInfoCmdr?.Confidence ?? 0
                },
                StdRatingStats = new()
                {
                    MmrGames = mmrInfoStd?.MmrGames ?? 0,
                    Mmr = mmrInfoStd?.Mmr ?? startMmr,
                    Games = playerInfo.GamesStd,
                    Wins = playerInfo.WinsStd,
                    Mvp = playerInfo.MvpStd,
                    TeamGames = playerInfo.TeamGamesStd,
                    Consistency = mmrInfoStd?.Consistency ?? 0,
                    Confidence = mmrInfoStd?.Confidence ?? 0
                }
            };
        }
    }

    private static MmrInfo? GetMmrInfo(Dictionary<int, string> toonIdRatingOverTime, Dictionary<int, List<DsRCheckpoint>> playerRatings, int toonId, int playerId)
    {
        MmrInfo? mmrInfo = null;

        if (playerId > 0 && playerRatings.ContainsKey(playerId)) {
            var plRat = playerRatings[playerId];
            var lastPlRat = plRat.LastOrDefault();

            mmrInfo = new() {
                Mmr = lastPlRat?.Mmr ?? startMmr,
                MmrGames = lastPlRat?.Index ?? 0,
                Consistency = lastPlRat?.Consistency ?? 0,
                Confidence = lastPlRat?.Confidence ?? 0
            };

            toonIdRatingOverTime[toonId] = GetOverTimeRating(plRat) ?? "";
        }
        return mmrInfo;
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

    private async Task<Dictionary<int, PlayerMapDto>> GetPlayerIdToonIdMap()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return (await context.Players
            .AsNoTracking()
            .ProjectTo<PlayerMapDto>(mapper.ConfigurationProvider)
            .ToListAsync())
            .ToDictionary(k => k.PlayerId, v => v);
    }

    private async Task ResetGlobals()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var commanderRatings = await context.CommanderMmrs //SeedCommanders has to be called before!
            .AsNoTracking()
            .ToListAsync();

        cmdrMmrDic = commanderRatings.ToDictionary(k => new CmdrMmmrKey() { Race = k.Race, Opprace = k.OppRace }, v => v);

        maxMmrCmdr = startMmr;
        maxMmrStd = startMmr;
        ReplayPlayerMmrChanges.Clear();
        LatestReplayGameTime = DateTime.MinValue;
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

    //private async Task SaveReplayPlayersData(Dictionary<int, float> replayPlayerMmrChanges)
    //{
    //    using var scope = serviceProvider.CreateScope();
    //    using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

    //    StringBuilder sb = new();
    //    int i = 0;
    //    foreach (var ent in replayPlayerMmrChanges)
    //    {
    //        sb.Append($"UPDATE {nameof(ReplayContext.ReplayPlayers)}" +
    //            $" SET {nameof(ReplayPlayer.MmrChange)} = {ent.Value.ToString(CultureInfo.InvariantCulture)}" +
    //            $" WHERE {nameof(ReplayPlayer.ReplayPlayerId)} = {ent.Key}; ");
    //        i++;
    //        if (i % 1000 == 0)
    //        {
    //            await context.Database.ExecuteSqlRawAsync(sb.ToString());
    //            sb.Clear();
    //        }
    //    }

    //    if (sb.Length > 0)
    //    {
    //        await context.Database.ExecuteSqlRawAsync(sb.ToString());
    //    }
    //}

    //private async Task SavePlayersData(Dictionary<int, List<DsRCheckpoint>> playerRatings)
    //{
    //    using var scope = serviceProvider.CreateScope();
    //    using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

    //    StringBuilder sb = new();
    //    int i = 0;
    //    foreach (var ent in playerRatings)
    //    {
    //        var lastRating = ent.Value.Last();
    //        var mmr = lastRating.Mmr == double.NaN ? "0" : lastRating.Mmr.ToString(CultureInfo.InvariantCulture);

    //        sb.Append($"UPDATE {nameof(ReplayContext.Players)}" +
    //            $" SET {nameof(Player.Mmr)} = {mmr}, {nameof(Player.MmrOverTime)} = '{GetOverTimeRating(ent.Value)}'" +
    //            $" WHERE {nameof(Player.PlayerId)} = {ent.Key}; ");

    //        i++;
    //        if (i % 500 == 0)
    //        {
    //            await context.Database.ExecuteSqlRawAsync(sb.ToString());
    //            sb.Clear();
    //        }
    //    }


    //    if (sb.Length > 0)
    //    {
    //        await context.Database.ExecuteSqlRawAsync(sb.ToString());
    //    }
    //}

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

    private void AddPlayersRankings(Dictionary<int, List<DsRCheckpoint>> playerRatings, TeamData teamData, DateTime gameTime)
    {
        foreach (var player in teamData.Players)
        {
            var plRatings = playerRatings[GetMmrId(player.ReplayPlayer.Player)];
            var currentPlayerRating = plRatings.Last();
            int gamesCountBefore = currentPlayerRating.Index;

            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;
            double confidenceBefore = currentPlayerRating.Confidence;

            double mmrAfter = mmrBefore + player.PlayerMmrDelta;
            double consistencyAfter = consistencyBefore + player.PlayerConsistencyDelta;
            double confidenceAfter = confidenceBefore + player.PlayerConfidenceDelta;

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);
            mmrAfter = Math.Max(1, mmrAfter);

            ReplayPlayerMmrChanges[player.ReplayPlayer.ReplayPlayerId] = (float)(mmrAfter - mmrBefore);
            plRatings.Add(new DsRCheckpoint()
            {
                Mmr = mmrAfter,
                Consistency = consistencyAfter,
                Confidence = confidenceAfter,
                Index = gamesCountBefore + 1,
                Time = gameTime
            });
        }
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

    private static void SetExpectationsToWin(Dictionary<int, List<DsRCheckpoint>> playerRatings, ReplayData replayData)
    {
        replayData.WinnerPlayersExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.PlayersAvgMmr, replayData.LoserTeamData.PlayersAvgMmr);
        replayData.WinnerCmdrExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.CmdrComboMmr, replayData.LoserTeamData.CmdrComboMmr);
    }

    private static void SetConfidence(Dictionary<int, List<DsRCheckpoint>> playerRatings, ReplayData replayData)
    {
        SetConfidence(playerRatings, replayData.WinnerTeamData);
        SetConfidence(playerRatings, replayData.LoserTeamData);

        replayData.Confidence = (replayData.WinnerTeamData.Confidence + replayData.LoserTeamData.Confidence) / 2;
    }
    private static void SetConfidence(Dictionary<int, List<DsRCheckpoint>> playerRatings, TeamData teamData)
    {
        foreach (var playerData in teamData.Players) {
            playerData.Confidence = playerRatings[GetMmrId(playerData.ReplayPlayer.Player)].Last().Confidence;
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        if (teamMmrMean <= 0)
        {
            throw new Exception("Team MMR can't be 0 or less!");
        }

        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double mcv)
    {
        return (double)(eloK * mcv * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
    }

    static readonly double distributionMult = 1.0 / (1/*2*/);
    private static double GetConfidenceFactor(double confidence)
    {
        double variance = ((distributionMult * 0.4) + (1 - confidence));

        return distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private static double GetCorrectedConfidenceFactor(double playerConfidence, double replayConfidence)
    {
        double totalConfidenceFactor = (1 - GetConfidenceFactor(playerConfidence)) * GetConfidenceFactor(replayConfidence);
        return 1 + confidenceImpact * (totalConfidenceFactor - 1);
    }

    //private static double GetNormalDistribution(double mean, double variance, double value)
    //{
    //    double potence = -Math.Pow(((value - mean) / (Math.Abs(variance) * Math.Sqrt(2))), 2);
    //    double top = Math.Pow(Math.E, potence);
    //    double bottom = Math.Sqrt(2 * Math.PI) * Math.Abs(variance);

    //    return top / bottom;
    //}

    private static double GetPlayersComboMmr(Dictionary<int, List<DsRCheckpoint>> playerRatings, PlayerData[] playerDatas)
    {
        double teamMmr = 0;

        foreach (var playerData in playerDatas)
        {
            if (!playerRatings.ContainsKey(GetMmrId(playerData.ReplayPlayer.Player)))
            {
                playerRatings[GetMmrId(playerData.ReplayPlayer.Player)] = new List<DsRCheckpoint>() { new() {
                    Mmr = startMmr,
                    Consistency = 0,
                    Confidence = 0,
                    Index = 0,
                    Time = DateTime.MinValue
                } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatings[GetMmrId(playerData.ReplayPlayer.Player)].Last().Mmr;
            }
        }
        return teamMmr / playerDatas.Length;
    }

    public static int GetMmrId(PlayerDsRDto player)
    {
        //todo
        return player.PlayerId;
    }

    private static void FixMmrEquality(TeamData teamData, TeamData oppTeamData)
    {
        PlayerData[] posDeltaPlayers =
            teamData.Players.Where(x => x.PlayerMmrDelta >= 0).Concat(
            oppTeamData.Players.Where(x => x.PlayerMmrDelta >= 0)).ToArray();

        PlayerData[] negPlayerDeltas =
            teamData.Players.Where(x => x.PlayerMmrDelta < 0).Concat(
            oppTeamData.Players.Where(x => x.PlayerMmrDelta < 0)).ToArray();



        double absSumPosDeltas = Math.Abs(teamData.Players.Sum(x => Math.Max(0, x.PlayerMmrDelta)) + oppTeamData.Players.Sum(x => Math.Max(0, x.PlayerMmrDelta)));
        double absSumNegDeltas = Math.Abs(teamData.Players.Sum(x => Math.Min(0, x.PlayerMmrDelta)) + oppTeamData.Players.Sum(x => Math.Min(0, x.PlayerMmrDelta)));
        double absSumAllDeltas = absSumPosDeltas + absSumNegDeltas;

        //if (teamData.Players.Length != oppTeamData.Players.Length)
        //{
        //    throw new Exception("Not same player amount.");
        //}

        if (absSumPosDeltas == 0 || absSumNegDeltas == 0)
        {
            foreach (var player in teamData.Players)
            {
                player.PlayerMmrDelta = 0;
            }
            foreach (var player in oppTeamData.Players)
            {
                player.PlayerMmrDelta = 0;
            }
            return;
        }

        foreach (var posDeltaPlayer in posDeltaPlayers)
        {
            posDeltaPlayer.PlayerMmrDelta *= (absSumAllDeltas / (absSumPosDeltas * 2));
        }
        foreach (var negDeltaPlayer in negPlayerDeltas)
        {
            negDeltaPlayer.PlayerMmrDelta *= (absSumAllDeltas / (absSumNegDeltas * 2));
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
    public int MmrGames { get; set; }
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
}