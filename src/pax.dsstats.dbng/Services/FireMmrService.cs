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

public class FireMmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<FireMmrService> logger;

    public FireMmrService(IServiceProvider serviceProvider, IMapper mapper, ILogger<FireMmrService> logger)
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

    private static bool useCommanderMmr = true;
    private static bool useConsistency = true;
    private static bool useFactorToTeamMates = true;

    private readonly Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr = new();
    private readonly Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();
    private CommanderMmr[] commanderRatings = null!;
    private readonly Dictionary<int, float> replayPlayerMmrChanges = new();

    public event EventHandler<EventArgs>? Recalculated;
    protected virtual void OnRecalculated(EventArgs e)
    {
        EventHandler<EventArgs>? handler = Recalculated;
        handler?.Invoke(this, e);
    }

    public async Task CalcMmmr()
    {
        Stopwatch sw = new();
        sw.Start();

        await ClearRatings(false);

        await CalcMmrCmdr();
        await CalcMmrStd();

        sw.Stop();
        logger.LogWarning($"fire-ratings calculated in {sw.ElapsedMilliseconds} ms");

        OnRecalculated(new());
    }

    public async Task CalcMmrCmdr()
    {
        maxMmr = startMmr;
        await SeedCommanderMmrs();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayDsrDtos = await context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Duration >= 300)
            .Where(r =>
                (r.Playercount == 6)
                && (r.GameMode == GameMode.Commanders)
                /*Fake*/&& (r.WinnerTeam != 0/*Fake*/))
            .OrderBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        int count = 0;
        for (count = 0; count < replayDsrDtos.Count; count++) {
            var replay = replayDsrDtos[count];

            if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3)) {
                logger.LogWarning($"skipping wrong cmdr commanders");
                continue;
            }

            bool isMmrCalculated = replay.ReplayPlayers.Any(p => p.MmrChange.HasValue);
            if (!isMmrCalculated) {
                break;
            }
        }

        for (; count < replayDsrDtos.Count; count++) {
            var replay = replayDsrDtos[count];

            if (count == 507) {
            }

            if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3)) {
                logger.LogWarning($"skipping wrong cmdr commanders");
                continue;
            }

            var winnerTeam = replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam);
            var loserTeam = replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam);
            IEnumerable<ReplayPlayerDsRDto> leaverTeam = Enumerable.Empty<ReplayPlayerDsRDto>();

            if (winnerTeam.Count() != 3 || loserTeam.Count() != 3) {
                logger.LogWarning($"skipping wrong teamcounts");
                continue;
            }

            //replay.Maxleaver < 90 -> hasLeaver
            //replay.Duration - winnerTeam.ElementAt(0).Duration >= 90 -> playerIsLeaver

            //int correctedDuration = replay.Duration;
            //if (replay.WinnerTeam == 0) {
            //    correctedDuration = replay.ReplayPlayers.Where(x => !x.IsUploader).Max(x => x.Duration);

            //    var uploaders = replay.ReplayPlayers.Where(x => x.IsUploader);

            //    if (!uploaders.Any()) {
            //        continue;
            //    } else {
            //        winnerTeam = replay.ReplayPlayers.Where(x => !x.IsUploader && x.Duration >= uploaders.First().Duration - 100);
            //        //loserTeam = 
            //    }
            //}
            //leaverTeam = replay.ReplayPlayers.Where(x => x.Duration <= correctedDuration - 89);


            var winnerTeamMmr = GetTeamMmr(winnerTeam, GameMode.Commanders, replay.GameTime, replay.Duration);
            var loserTeamMmr = GetTeamMmr(loserTeam, GameMode.Commanders, replay.GameTime, replay.Duration);
            var teamElo = ELO(winnerTeamMmr, loserTeamMmr);

            var winnersCommandersComboMMR = GetCommandersComboMMR(winnerTeam);
            var losersCommandersComboMMR = GetCommandersComboMMR(loserTeam);
            var commandersElo = ELO(winnersCommandersComboMMR, losersCommandersComboMMR);


            (double[] winnersMmrDelta, double[] winnersConsistencyDelta, double[] winnersCommandersMmrDelta) = CalculateRatingsDeltas(winnerTeam.Select(s => s.Player), GameMode.Commanders, true, teamElo, winnerTeamMmr, commandersElo);
            (double[] losersMmrDelta, double[] losersConsistencyDelta, double[] losersCommandersMmrDelta) = CalculateRatingsDeltas(loserTeam.Select(s => s.Player), GameMode.Commanders, false, teamElo, loserTeamMmr, commandersElo);

            FixMMR_Equality(winnersMmrDelta, losersMmrDelta);
            //FixMMR_Equality(winnersCommandersMmrDelta, losersCommandersMmrDelta);

            AddPlayersRankings(winnerTeam, GameMode.Commanders, winnersMmrDelta, winnersConsistencyDelta, replay.GameTime);
            AddPlayersRankings(loserTeam, GameMode.Commanders, losersMmrDelta, losersConsistencyDelta, replay.GameTime);

            SetCommandersComboMMR(winnersCommandersMmrDelta, winnerTeam);
            SetCommandersComboMMR(losersCommandersMmrDelta, loserTeam);
        }

        await SetRatings(GameMode.Commanders);
    }
    public async Task CalcMmrStd()
    {
        maxMmr = startMmr;
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayDsrDtos = await context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Duration >= 300)
            .Where(r =>
                (r.Playercount == 6)
                && (r.GameMode == GameMode.Standard)
                /*Fake*/&& (r.WinnerTeam != 0/*Fake*/))
            .OrderBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        int count = 0;
        for (count = 0; count < replayDsrDtos.Count; count++) {
            bool isMmrCalculated = replayDsrDtos[count].ReplayPlayers.Any(p => p.MmrChange.HasValue);
            if (!isMmrCalculated) {
                break;
            }
        }

        for (; count < replayDsrDtos.Count; count++) {
            var replay = replayDsrDtos[count];
            var winnerTeam = replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam);
            var loserTeam = replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam);
            IEnumerable<ReplayPlayerDsRDto> leaverTeam = Enumerable.Empty<ReplayPlayerDsRDto>();

            if (winnerTeam.Count() != 3 || loserTeam.Count() != 3)
            {
                logger.LogWarning($"skipping wrong teamcounts");
                continue;
            }

            //replay.Maxleaver < 90 -> hasLeaver
            //replay.Duration - winnerTeam.ElementAt(0).Duration >= 90 -> playerIsLeaver

            //int correctedDuration = replay.Duration;
            //if (replay.WinnerTeam == 0)
            //{
            //    correctedDuration = replay.ReplayPlayers.Where(x => !x.IsUploader).Max(x => x.Duration);

            //    var uploaders = replay.ReplayPlayers.Where(x => x.IsUploader);

            //    if (!uploaders.Any()) {
            //        continue;
            //    } else {
            //        winnerTeam = replay.ReplayPlayers.Where(x => !x.IsUploader && x.Duration >= uploaders.First().Duration - 100);
            //        //loserTeam = 
            //    }
            //}

            //leaverTeam = replay.ReplayPlayers.Where(x => x.Duration <= correctedDuration - 89);


            var winnerTeamMmr = GetTeamMmr(winnerTeam, GameMode.Standard, replay.GameTime, replay.Duration);
            var loserTeamMmr = GetTeamMmr(loserTeam, GameMode.Standard, replay.GameTime, replay.Duration);
            var teamElo = ELO(winnerTeamMmr, loserTeamMmr);

            (double[] winnersMmrDelta, double[] winnersConsistencyDelta, double[] dummy) = CalculateRatingsDeltas(winnerTeam.Select(s => s.Player), GameMode.Standard, true, teamElo, winnerTeamMmr, 0.5);
            (double[] losersMmrDelta, double[] losersConsistencyDelta, dummy) = CalculateRatingsDeltas(loserTeam.Select(s => s.Player), GameMode.Standard, false, teamElo, loserTeamMmr, 0.5);

            FixMMR_Equality(winnersMmrDelta, losersMmrDelta);

            AddPlayersRankings(winnerTeam, GameMode.Standard, winnersMmrDelta, winnersConsistencyDelta, replay.GameTime);
            AddPlayersRankings(loserTeam, GameMode.Standard, losersMmrDelta, losersConsistencyDelta, replay.GameTime);
        }

        await SetRatings(GameMode.Standard);
    }

    private async Task SetRatings(GameMode gameMode)
    {
        maxMmr = startMmr;

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        //# playerRatings
        Dictionary<int, List<DsRCheckpoint>> playerRatings = null!;
        if (gameMode == GameMode.Commanders)
        {
            playerRatings = playerRatingsCmdr;
        }
        else if (gameMode == GameMode.Standard)
        {
            playerRatings = playerRatingsStd;
        }

        var players = await context.Players.ToListAsync();
        foreach (var player in players) {
            if (!playerRatings.ContainsKey(player.PlayerId)) {
                continue;
            }

            var playerRating = playerRatings[player.PlayerId];

            if (gameMode == GameMode.Commanders) {
                player.Mmr = playerRating.Last().Mmr;
                player.MmrOverTime = GetOverTimeRating(playerRating);
            } else if (gameMode == GameMode.Standard) {
                player.MmrStd = playerRating.Last().Mmr;
                player.MmrStdOverTime = GetOverTimeRating(playerRating);
            }
        }

        //# replayPlayerMmrChanges
        var replayPlayers = await context.ReplayPlayers.ToListAsync();
        foreach (var replayPlayer in replayPlayers) {
            if (!replayPlayerMmrChanges.ContainsKey(replayPlayer.ReplayPlayerId)) {
                continue;
            }

            var replayPlayerMmrChange = replayPlayerMmrChanges[replayPlayer.ReplayPlayerId];
            replayPlayer.MmrChange = replayPlayerMmrChange;
        }

        //# commanderRatings
        if (gameMode == GameMode.Commanders) {
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            foreach (var rating in commanderRatings) {
                var commanderCombo = await context.CommanderMmrs.FirstAsync(f => f.CommanderMmrId == rating.CommanderMmrId);

                commanderCombo.SynergyMmr = rating.SynergyMmr;
                commanderCombo.AntiSynergyMmr = rating.AntiSynergyMmr;
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task ClearRatings(bool recalculate)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)

        if (recalculate) {
            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.ReplayPlayers)} SET {nameof(ReplayPlayer.MmrChange)} = NULL");

            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.Mmr)} = {startMmr}");
            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStd)} = {startMmr}");
            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrOverTime)} = NULL");
            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.Players)} SET {nameof(Player.MmrStdOverTime)} = NULL");

            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.SynergyMmr)} = {startMmr}");
            await context.Database.ExecuteSqlRawAsync($"UPDATE {nameof(context.CommanderMmrs)} SET {nameof(CommanderMmr.AntiSynergyMmr)} = {startMmr}");
        }

        playerRatingsCmdr.Clear();
        playerRatingsStd.Clear();
        replayPlayerMmrChanges.Clear();
        commanderRatings = await context.CommanderMmrs.AsNoTracking().ToArrayAsync();
        maxMmr = startMmr;
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

    private static void FixMMR_Equality(double[] team1_mmrDelta, double[] team2_mmrDelta)
    {
        double abs_sumTeam1_mmrDelta = Math.Abs(team1_mmrDelta.Sum());
        double abs_sumTeam2_mmrDelta = Math.Abs(team2_mmrDelta.Sum());

        for (int i = 0; i < 3; i++)
        {
            team1_mmrDelta[i] = team1_mmrDelta[i] *
                ((abs_sumTeam1_mmrDelta + abs_sumTeam2_mmrDelta) / (abs_sumTeam1_mmrDelta * 2));
            team2_mmrDelta[i] = team2_mmrDelta[i] *
                ((abs_sumTeam2_mmrDelta + abs_sumTeam1_mmrDelta) / (abs_sumTeam2_mmrDelta * 2));

            if (abs_sumTeam1_mmrDelta == 0 || abs_sumTeam2_mmrDelta == 0)
            {
                team1_mmrDelta[i] = 0;
                team2_mmrDelta[i] = 0;
            }
        }
    }

    private static double GetCorrected_revConsistency(double raw_revConsistency)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
        //return ((1 - consistencyImpact) + (consistencyImpact * raw_revConsistency)); //Equal to above
    }

    static double maxMmr = startMmr;
    private (double[], double[], double[]) CalculateRatingsDeltas(IEnumerable<PlayerDsRDto> teamPlayers, GameMode gameMode, bool winner, double teamElo, double teamMmr, double commandersElo)
    {
        var playersMmrDelta = new double[teamPlayers.Count()];
        var playersConsistencyDelta = new double[teamPlayers.Count()];
        var commandersMmrDelta = new double[teamPlayers.Count()];

        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            List<DsRCheckpoint> plRatings = null!;
            if (gameMode == GameMode.Commanders)
            {
                plRatings = playerRatingsCmdr[teamPlayers.ElementAt(i).PlayerId];
            }
            else if (gameMode == GameMode.Standard)
            {
                plRatings = playerRatingsStd[teamPlayers.ElementAt(i).PlayerId];
            }

            double playerConsistency = plRatings.Last().Consistency;
            double playerMmr = plRatings.Last().Mmr;
            if (playerMmr > maxMmr)
            {
                maxMmr = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamMmr, playerMmr);
            double factor_consistency = GetCorrected_revConsistency(1 - playerConsistency);

            double playerImpact = 1.0
                * (useFactorToTeamMates ? factor_playerToTeamMates : (1.0 / 3))
                * (useConsistency ? factor_consistency : 1.0);

            playersMmrDelta[i] = CalculateMmrDelta(teamElo, playerImpact, (useCommanderMmr ? (1 - commandersElo) : 1));
            playersConsistencyDelta[i] = consistencyDeltaMult * 2 * (teamElo - 0.50);

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmr)) / startMmr;
            commandersMmrDelta[i] = CalculateMmrDelta(commandersElo, 1, commandersMmrImpact);

            if (!winner)
            {
                playersMmrDelta[i] *= -1;
                playersConsistencyDelta[i] *= -1;
                commandersMmrDelta[i] *= -1;
            }
        }
        return (playersMmrDelta, playersConsistencyDelta, commandersMmrDelta);
    }

    private void AddPlayersRankings(IEnumerable<ReplayPlayerDsRDto> teamPlayers, GameMode gameMode, double[] playersMmrDelta, double[] playersConsistencyDelta, DateTime gameTime)
    {
        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            List<DsRCheckpoint> plRatings = null!;
            if (gameMode == GameMode.Commanders)
            {
                plRatings = playerRatingsCmdr[teamPlayers.ElementAt(i).Player.PlayerId];
            }
            else if (gameMode == GameMode.Standard)
            {
                plRatings = playerRatingsStd[teamPlayers.ElementAt(i).Player.PlayerId];
            }

            double mmrBefore = plRatings.Last().Mmr;
            double consistencyBefore = plRatings.Last().Consistency;

            double mmrAfter = mmrBefore + playersMmrDelta[i];
            double consistencyAfter = consistencyBefore + playersConsistencyDelta[i];

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);

            replayPlayerMmrChanges.Add(teamPlayers.ElementAt(i).ReplayPlayerId, (float)(mmrAfter - mmrBefore));
            plRatings.Add(new DsRCheckpoint() { Mmr = mmrAfter, Consistency = consistencyAfter, Time = gameTime });
        }
    }

    public static double ELO(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (playerTwoRating - playerOneRating)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double mcv)
    {
        return (double)(eloK * mcv * (1 - elo) * playerImpact);
    }

    private static double PlayerToTeamMates(double teamMmr, double playerMmr)
    {
        if (teamMmr < 1)
        {
            return (1.0 / 3);
        }

        return (playerMmr / teamMmr) / 3.0;
    }

    private double GetTeamMmr(IEnumerable<ReplayPlayerDsRDto> replayPlayers, GameMode gameMode, DateTime gameTime, int replayDuration)
    {
        double teamMmr = 0;

        foreach (var replayPlayer in replayPlayers)
        {
            Dictionary<int, List<DsRCheckpoint>> playerRatings = null!;
            if (gameMode == GameMode.Commanders)
            {
                playerRatings = playerRatingsCmdr;
            }
            else if (gameMode == GameMode.Standard)
            {
                playerRatings = playerRatingsStd;
            }

            if (!playerRatings.ContainsKey(replayPlayer.Player.PlayerId))
            {
                playerRatings[replayPlayer.Player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatings[replayPlayer.Player.PlayerId].Last().Mmr;
            }
        }

        return teamMmr / 3.0;
    }




    const double AntiSynergy_Percentage = 0.50;
    const double Synergy_Percentage = 1 - AntiSynergy_Percentage;

    const double OwnMatchup_Percentage = 1.0 / 3;
    const double MatesMatchups_Percentage = (1 - OwnMatchup_Percentage) / 2;


    private double GetCommandersComboMMR(IEnumerable<ReplayPlayerDsRDto> teamPlayers)
    {
        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamPlayers.Count(); playerIndex++) {
            var playerCmdr = teamPlayers.ElementAt(playerIndex).Race;

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamPlayers.Count(); synergyPlayerIndex++) {
                if (playerIndex == synergyPlayerIndex) {
                    continue;
                }

                var synergyPlayerCmdr = teamPlayers.ElementAt(synergyPlayerIndex).Race;
                var synergy = commanderRatings
                    .First(x => x.Race == playerCmdr && x.OppRace == synergyPlayerCmdr);

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Count(); antiSynergyPlayerIndex++) {
                var antiSynergyPlayerCmdr = teamPlayers.ElementAt(antiSynergyPlayerIndex).OppRace;

                var antiSynergy = commanderRatings
                    .First(x => x.Race == playerCmdr && x.OppRace == antiSynergyPlayerCmdr);

                if (playerIndex == antiSynergyPlayerIndex) {
                    antiSynergySum += (OwnMatchup_Percentage * antiSynergy.AntiSynergyMmr);
                } else {
                    antiSynergySum += (MatesMatchups_Percentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (AntiSynergy_Percentage * antiSynergySum)
                + (Synergy_Percentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }
    //private double GetCommandersComboMMR(ReplayPlayerDsRDto[] teamPlayers)
    //{
    //    double[] commandersComboMMR = new double[3];

    //    for (int i = 0; i < 3; i++)
    //    {

    //        double antiSynergySum = 0;
    //        double synergySum = 0;

    //        for (int k = 0; k < 3; k++)
    //        {
    //            CommanderMmr antiSynergyCommander = this.commanderRatings
    //                .Where(c =>
    //                    ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == enemyCommanders[k])) ||
    //                    ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == enemyCommanders[k])))
    //                .FirstOrDefault()!;

    //            double antiSynergyMmr;
    //            if ((antiSynergyCommander.Commander_1 == teamCommanders[i]) && (antiSynergyCommander.Commander_2 == enemyCommanders[k]))
    //            {
    //                antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_1;
    //            }
    //            else if ((antiSynergyCommander.Commander_2 == teamCommanders[i]) && (antiSynergyCommander.Commander_1 == enemyCommanders[k]))
    //            {
    //                antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_2;
    //            }
    //            else throw new Exception();

    //            if (i == k)
    //            {
    //                antiSynergySum += (OwnMatchup_Percentage * antiSynergyMmr);
    //            }
    //            else
    //            {
    //                antiSynergySum += (MatesMatchups_Percentage * antiSynergyMmr);


    //                CommanderMmr synergyCommander = this.commanderRatings
    //                .Where(c =>
    //                    ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == teamCommanders[k])) ||
    //                    ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == teamCommanders[k])))
    //                .FirstOrDefault()!;

    //                synergySum += (0.5 * synergyCommander.SynergyMmr);
    //            }
    //        }

    //        commandersComboMMR[i] = 0
    //            + (AntiSynergy_Percentage * antiSynergySum)
    //            + (Synergy_Percentage * synergySum);
    //    }

    //    return commandersComboMMR.Sum() / 3;
    //}

    
    private void SetCommandersComboMMR(double[] commandersMmrDelta, IEnumerable<ReplayPlayerDsRDto> teamPlayers)
    {
        for (int playerIndex = 0; playerIndex < teamPlayers.Count(); playerIndex++) {
            var playerCmdr = teamPlayers.ElementAt(playerIndex).Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamPlayers.Count(); synergyPlayerIndex++) {
                if (playerIndex == synergyPlayerIndex) {
                    continue;
                }

                var synergyPlayerCmdr = teamPlayers.ElementAt(synergyPlayerIndex).Race;
                var synergy = commanderRatings
                    .First(x => x.Race == playerCmdr && x.OppRace == synergyPlayerCmdr);

                synergy.SynergyMmr += commandersMmrDelta[playerIndex] / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Count(); antiSynergyPlayerIndex++) {
                var antiSynergyPlayerCmdr = teamPlayers.ElementAt(antiSynergyPlayerIndex).OppRace;

                var antiSynergy = commanderRatings
                    .First(x => x.Race == playerCmdr && x.OppRace == antiSynergyPlayerCmdr);

                antiSynergy.AntiSynergyMmr += commandersMmrDelta[playerIndex];
            }
        }
    }
    //private void SetCommandersComboMMR(double[] commandersMmrDelta, Commander[] teamCommanders, Commander[] enemyCommanders)
    //{
    //    for (int i = 0; i < 3; i++)
    //    {
    //        for (int k = 0; k < 3; k++)
    //        {
    //            CommanderMmr antiSynergyCommander = this.commanderRatings
    //                .Where(c =>
    //                    ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == enemyCommanders[k])) ||
    //                    ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == enemyCommanders[k])))
    //                .FirstOrDefault()!;

    //            if (antiSynergyCommander.Commander_1 == antiSynergyCommander.Commander_2)
    //            {
    //                antiSynergyCommander.AntiSynergyMmr_1 += commandersMmrDelta[i];
    //                antiSynergyCommander.AntiSynergyMmr_2 += commandersMmrDelta[i];
    //                antiSynergyCommander.AntiSynergyElo_1 = 0.5;
    //                antiSynergyCommander.AntiSynergyElo_2 = 0.5;
    //            }
    //            else
    //            {
    //                if ((antiSynergyCommander.Commander_1 == teamCommanders[i]) && (antiSynergyCommander.Commander_2 == enemyCommanders[k]))
    //                {
    //                    antiSynergyCommander.AntiSynergyMmr_1 += commandersMmrDelta[i];
    //                    antiSynergyCommander.AntiSynergyElo_1 = ELO(antiSynergyCommander.AntiSynergyMmr_1, antiSynergyCommander.AntiSynergyMmr_2);
    //                }
    //                else if ((antiSynergyCommander.Commander_2 == teamCommanders[i]) && (antiSynergyCommander.Commander_1 == enemyCommanders[k]))
    //                {
    //                    antiSynergyCommander.AntiSynergyMmr_2 += commandersMmrDelta[i];
    //                    antiSynergyCommander.AntiSynergyElo_2 = ELO(antiSynergyCommander.AntiSynergyMmr_2, antiSynergyCommander.AntiSynergyMmr_1);
    //                }
    //                else throw new Exception();
    //            }

    //            if (i != k)
    //            {
    //                CommanderMmr synergyCommander = this.commanderRatings
    //                    .Where(c =>
    //                        ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == teamCommanders[k])) ||
    //                        ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == teamCommanders[k])))
    //                    .FirstOrDefault()!;

    //                synergyCommander.SynergyMmr += commandersMmrDelta[i] / 2;
    //            }
    //        }
    //    }
    //}

    private async Task SeedCommanderMmrs()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (!await context.CommanderMmrs.AnyAsync())
        {
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            for (int i = 0; i < allCommanders.Count; i++)
            {
                for (int k = 0; k < allCommanders.Count; k++)
                {
                    context.CommanderMmrs.Add(new() {
                        SynergyMmr = FireMmrService.startMmr,

                        Race = allCommanders[i],
                        OppRace = allCommanders[k],

                        AntiSynergyMmr = FireMmrService.startMmr
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }
}