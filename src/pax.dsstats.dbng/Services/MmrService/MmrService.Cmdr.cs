
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    private double maxMmrCmdr = startMmr; //ToDo - load for continuation!!!

    private const double AntiSynergyPercentage = 0.50;
    private const double SynergyPercentage = 1 - AntiSynergyPercentage;
    private const double OwnMatchupPercentage = 1.0 / 3;
    private const double MatesMatchupsPercentage = (1 - OwnMatchupPercentage) / 2;

    private async Task<Dictionary<int, List<DsRCheckpoint>>> ReCalculateCmdr(DateTime startTime, DateTime endTime)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr = new();

        var replayDsRDtos = await GetCmdrReplayDsRDtos(startTime, endTime);
        foreach (var replay in replayDsRDtos)
        {
            ProcessCmdrReplay(playerRatingsCmdr, replay);
        }

        var weightedSum = playerRatingsCmdr.Sum(x => ((x.Value.Count - 1) * x.Value.Last().Uncertainty));
        var weightedUncertanity = weightedSum / playerRatingsCmdr.Sum(x => (x.Value.Count - 1));
        var accuracy = 1 - weightedUncertanity;

        return playerRatingsCmdr;
    }
    private Dictionary<int, List<DsRCheckpoint>> ContinueCalculateCmdr(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, List<ReplayDsRDto> newReplays)
    {
        if (!newReplays.Any())
        {
            return new();
        }

        foreach (var replay in newReplays)
        {
            ProcessCmdrReplay(playerRatingsCmdr, replay);
        }
        LatestReplayGameTime = newReplays.Last().GameTime;
        return playerRatingsCmdr;
    }


    private void ProcessCmdrReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayDsRDto replay)
    {
        if (replay.WinnerTeam == 0)
        {
            return;
        }

        if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3))
        {
            logger.LogInformation($"skipping wrong cmdr commanders");
            return;
        }

        ReplayProcessData replayProcessData = new(replay);
        if (replayProcessData.WinnerTeamData.Players.Length != 3 || replayProcessData.LoserTeamData.Players.Length != 3)
        {
            logger.LogInformation($"skipping wrong teamcounts");
            return;
        }

        SetCmdrMmrs(playerRatingsCmdr, replayProcessData.WinnerTeamData);
        SetCmdrMmrs(playerRatingsCmdr, replayProcessData.LoserTeamData);

        SetExpectationsToWin(playerRatingsCmdr, replayProcessData);

        CalculateRatingsDeltas(playerRatingsCmdr, replayProcessData, replayProcessData.WinnerTeamData);
        CalculateRatingsDeltas(playerRatingsCmdr, replayProcessData, replayProcessData.LoserTeamData);

        // Adjust Loser delta
        foreach (var loserPlayer in replayProcessData.LoserTeamData.Players)
        {
            loserPlayer.PlayerMmrDelta *= -1;
            loserPlayer.PlayerConsistencyDelta *= -1;
            loserPlayer.CommanderMmrDelta *= -1;
        }
        // Adjust Leaver delta
        foreach (var winnerPlayer in replayProcessData.WinnerTeamData.Players)
        {
            if (winnerPlayer.IsLeaver)
            {
                winnerPlayer.PlayerMmrDelta *= -1;
                winnerPlayer.PlayerConsistencyDelta *= -1;
                winnerPlayer.CommanderMmrDelta = 0;
            }
        }
        if (replayProcessData.WinnerTeamData.Players.Count(x => x.IsLeaver) > 0)
        {
        }
        if (replayProcessData.WinnerTeamData.Players.Count(x => x.IsLeaver) > 1)
        {
        }

        FixMmrEquality(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);


        AddPlayersRankings(playerRatingsCmdr, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        AddPlayersRankings(playerRatingsCmdr, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetCommandersComboMmr(replayProcessData.WinnerTeamData);
        SetCommandersComboMmr(replayProcessData.LoserTeamData);

        if (IsProgressActive)
        {
            SetProgress(replayProcessData, false);
        }
    }

    private void SetCmdrMmrs(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData)
    {
        teamData.CmdrComboMmr = GetCommandersComboMmr(teamData.Players);
        teamData.PlayersMeanMmr = GetPlayersComboMmr(playerRatingsCmdr, teamData.Players);
    }

    private void CalculateRatingsDeltas(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayProcessData replayProcessData, TeamData teamData)
    {
        foreach (var player in teamData.Players)
        {
            var plRatings = playerRatingsCmdr[GetMmrId(player.ReplayPlayer.Player)];
            var lastPlRating = plRatings.Last();
            double playerConsistency = lastPlRating.Consistency;
            
            double playerMmr = lastPlRating.Mmr;
            if (playerMmr > maxMmrCmdr)
            {
                maxMmrCmdr = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersMeanMmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_uncertainty = (1 - replayProcessData.Uncertainty);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0)
                * (useUncertanity ? factor_uncertainty : 1.0);

            if (playerImpact > teamData.Players.Length || playerImpact < 0)
            {
            }

            player.PlayerMmrDelta = CalculateMmrDelta(replayProcessData.WinnerPlayersExpectationToWin, playerImpact, (useCommanderMmr ? (1 - replayProcessData.WinnerCmdrExpectationToWin) : 1));
            player.PlayerConsistencyDelta = consistencyDeltaMult * 2 * (replayProcessData.WinnerPlayersExpectationToWin - 0.50);
            if (double.IsNaN(player.PlayerMmrDelta) || double.IsInfinity(player.PlayerMmrDelta))
            {
            }

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmrCmdr)) / startMmr;
            player.CommanderMmrDelta = CalculateMmrDelta(replayProcessData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);
            if (double.IsNaN(player.CommanderMmrDelta) || double.IsInfinity(player.CommanderMmrDelta))
            {
            }

            if (player.PlayerMmrDelta > eloK * teamData.Players.Length)
            {
                throw new Exception("MmrDelta is bigger than eloK");
            }
        }
    }


    private void SetCommandersComboMmr(TeamData teamData)
    {
        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].ReplayPlayer.Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].ReplayPlayer.Race;
                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].ReplayPlayer.OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta;
            }
        }
    }

    private double GetCommandersComboMmr(PlayerData[] teamPlayers)
    {
        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamPlayers.Length; playerIndex++)
        {
            var playerCmdr = teamPlayers[playerIndex].ReplayPlayer.Race;

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamPlayers.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamPlayers[synergyPlayerIndex].ReplayPlayer.Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamPlayers[antiSynergyPlayerIndex].ReplayPlayer.OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = antiSynergyPlayerCmdr }];

                if (playerIndex == antiSynergyPlayerIndex)
                {
                    antiSynergySum += (OwnMatchupPercentage * antiSynergy.AntiSynergyMmr);
                }
                else
                {
                    antiSynergySum += (MatesMatchupsPercentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (AntiSynergyPercentage * antiSynergySum)
                + (SynergyPercentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }

    private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 210
                && r.WinnerTeam > 0
                && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime >= startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task SeedCommanderMmrs()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (context.CommanderMmrs.Any())
        {
            return;
        }

        var commanderMmrs = await context.CommanderMmrs.ToListAsync();
        var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

        foreach (var race in allCommanders)
        {
            foreach (var oppRace in allCommanders)
            {
                CommanderMmr cmdrMmr = new()
                {
                    Race = race,
                    OppRace = oppRace,

                    SynergyMmr = startMmr,
                    AntiSynergyMmr = startMmr
                };
                context.CommanderMmrs.Add(cmdrMmr);
            }
        }

        await context.SaveChangesAsync();
    }
}
