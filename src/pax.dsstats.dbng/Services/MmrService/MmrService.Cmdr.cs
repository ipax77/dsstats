
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

        var orderedByGames = playerRatingsCmdr.OrderByDescending(x => x.Value.Count);
        var totalConfidence = orderedByGames.Sum(x => x.Value.Last().Confidence) / orderedByGames.Count();

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

        if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3) && replay.ReplayPlayers.Min(x => x.Duration > 0))
        {
            logger.LogDebug($"skipping invalid commanders");
            return;
        }

        ReplayData replayData = new(replay);
        if (replayData.WinnerTeamData.Players.Length != 3 || replayData.LoserTeamData.Players.Length != 3)
        {
            logger.LogDebug($"skipping wrong teamcounts");
            return;
        }

        SetReplayDataCmdr(playerRatingsCmdr, replayData);

        CalculateRatingsDeltas(playerRatingsCmdr, replayData, replayData.WinnerTeamData);
        CalculateRatingsDeltas(playerRatingsCmdr, replayData, replayData.LoserTeamData);

        // Adjust Loser delta
        foreach (var loserPlayer in replayData.LoserTeamData.Players)
        {
            loserPlayer.PlayerMmrDelta *= -1;
            loserPlayer.CommanderMmrDelta *= -1;
        }
        // Adjust Leaver delta
        foreach (var winnerPlayer in replayData.WinnerTeamData.Players)
        {
            if (winnerPlayer.IsLeaver)
            {
                winnerPlayer.PlayerMmrDelta *= -1;
                winnerPlayer.CommanderMmrDelta = 0;

                winnerPlayer.PlayerConsistencyDelta *= -1;
            }
        }
        if (replayData.WinnerTeamData.Players.Count(x => x.IsLeaver) > 0)
        {
        }
        if (replayData.WinnerTeamData.Players.Count(x => x.IsLeaver) > 1)
        {
        }

        FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);


        AddPlayersRankings(playerRatingsCmdr, replayData.WinnerTeamData, replayData.ReplayGameTime);
        AddPlayersRankings(playerRatingsCmdr, replayData.LoserTeamData, replayData.ReplayGameTime);

        SetCommandersComboMmr(replayData.WinnerTeamData);
        SetCommandersComboMmr(replayData.LoserTeamData);

        if (IsProgressActive)
        {
            SetProgress(replayData, false);
        }
    }

    private void SetReplayDataCmdr(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayData replayData)
    {
        SetCmdrMmrs(playerRatingsCmdr, replayData.WinnerTeamData);
        SetCmdrMmrs(playerRatingsCmdr, replayData.LoserTeamData);

        SetExpectationsToWin(playerRatingsCmdr, replayData);

        SetConfidence(playerRatingsCmdr, replayData);
    }

    private void SetCmdrMmrs(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData)
    {
        if (useCommanderMmr) {
            teamData.CmdrComboMmr = GetCommandersComboMmr(teamData.Players);
        }
        teamData.PlayersAvgMmr = GetPlayersComboMmr(playerRatingsCmdr, teamData.Players);
    }

    private void CalculateRatingsDeltas(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayData replayData, TeamData teamData)
    {
        foreach (var playerData in teamData.Players)
        {
            var plRatings = playerRatingsCmdr[GetMmrId(playerData.ReplayPlayer.Player)];
            var lastPlRating = plRatings.Last();

            double playerConsistency = lastPlRating.Consistency;
            double playerConfidence = lastPlRating.Confidence;
            double playerMmr = lastPlRating.Mmr;

            //if (playerMmr > maxMmrCmdr)
            //{
            //    maxMmrCmdr = playerMmr;
            //}

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersAvgMmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_confidence = GetCorrectedConfidenceFactor(playerConfidence, replayData.Confidence);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0)
                * (useConfidence ? factor_confidence : 1.0);

            playerData.PlayerMmrDelta = CalculateMmrDelta(replayData.WinnerPlayersExpectationToWin, playerImpact, (useCommanderMmr ? (1 - replayData.WinnerCmdrExpectationToWin) : 1));
            playerData.PlayerConsistencyDelta = consistencyDeltaMult * 2 * (replayData.WinnerPlayersExpectationToWin - 0.50);
            playerData.PlayerConfidenceDelta = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmrCmdr)) / startMmr;
            playerData.CommanderMmrDelta = CalculateMmrDelta(replayData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);

            if (playerData.PlayerMmrDelta > eloK * teamData.Players.Length)
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
            if ((int)playerCmdr <= 3) {
                continue;
            }

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].ReplayPlayer.Race;
                if ((int)synergyPlayerCmdr <= 3) {
                    continue;
                }

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].ReplayPlayer.OppRace;
                if ((int)antiSynergyPlayerCmdr <= 3) {
                    continue;
                }

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta;
            }
        }
    }

    private double GetCommandersComboMmr(PlayerData[] teamPlayers)
    {
        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamPlayers.Length; playerIndex++) {
            var playerCmdr = teamPlayers[playerIndex].ReplayPlayer.Race;
            if ((int)playerCmdr <= 3) {
                commandersComboMMRSum += startMmr;
                continue;
            }

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamPlayers.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamPlayers[synergyPlayerIndex].ReplayPlayer.Race;
                if ((int)synergyPlayerCmdr <= 3) {
                    continue;
                }

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamPlayers[antiSynergyPlayerIndex].ReplayPlayer.OppRace;
                if ((int)antiSynergyPlayerCmdr <= 3) {
                    continue;
                }

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
