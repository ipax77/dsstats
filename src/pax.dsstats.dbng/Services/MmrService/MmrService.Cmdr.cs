
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

    private async Task<Dictionary<int, List<DsRCheckpoint>>> CalculateCmdr(DateTime startTime)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr = new();

        var replayDsRDtos = await GetCmdrReplayDsRDtos(startTime);
        foreach (var replay in replayDsRDtos)
        {
            ProcessCmdrReplay(playerRatingsCmdr, replay);
        }
        return playerRatingsCmdr;
    }

    private void ProcessCmdrReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayDsRDto replay)
    {
        if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3))
        {
            logger.LogWarning($"skipping wrong cmdr commanders");
            return;
        }

        ReplayProcessData replayProcessData = new(replay);
        if (replayProcessData.WinnerTeamData.Players.Length != 3 || replayProcessData.LoserTeamData.Players.Length != 3) {
            logger.LogWarning($"skipping wrong teamcounts");
            return;
        }

        if (replay.WinnerTeam == 0) {
            return;
        }

        SetMmrs(playerRatingsCmdr, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        SetMmrs(playerRatingsCmdr, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetExpectationsToWin(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);

        CalculateRatingsDeltas(playerRatingsCmdr, replayProcessData.WinnerTeamData);
        CalculateRatingsDeltas(playerRatingsCmdr, replayProcessData.LoserTeamData);

        FixMmrEquality(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);

        // Adjust Loser delta
        for (int i = 0; i < replayProcessData.LoserTeamData.Players.Length; i++)
        {
            replayProcessData.LoserTeamData.PlayersMmrDelta[i] *= -1;
            replayProcessData.LoserTeamData.PlayersConsistencyDelta[i] *= -1;
            replayProcessData.LoserTeamData.CmdrMmrDelta[i] *= -1;
        }

        AddPlayersRankings(playerRatingsCmdr, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        AddPlayersRankings(playerRatingsCmdr, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetCommandersComboMmr(replayProcessData.WinnerTeamData);
        SetCommandersComboMmr(replayProcessData.LoserTeamData);
    }

    private void AddPlayersRankings(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData, DateTime gameTime)
    {
        for (int i = 0; i < teamData.Players.Length; i++)
        {
            var player = teamData.Players[i];
            var plRatings = playerRatingsCmdr[player.Player.PlayerId];
            var currentPlayerRating = plRatings.Last();

            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;

            double mmrAfter = mmrBefore + teamData.PlayersMmrDelta[i];
            double consistencyAfter = consistencyBefore + teamData.PlayersConsistencyDelta[i];

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);

            replayPlayerMmrChanges.Add(teamData.Players[i].ReplayPlayerId, (float)teamData.PlayersMmrDelta[i]);
            plRatings.Add(new DsRCheckpoint() { Mmr = mmrAfter, Consistency = consistencyAfter, Time = gameTime });
        }
    }

    private void SetMmrs(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData, DateTime gameTime)
    {
        teamData.CmdrComboMmr = GetCommandersComboMmr(teamData.Players);
        teamData.PlayersMmr = GetTeamMmr(playerRatingsCmdr, teamData.Players, gameTime);
    }

    private void SetExpectationsToWin(TeamData winnerTeamData, TeamData loserTeamData)
    {
        winnerTeamData.WinnerPlayersExpectationToWin = loserTeamData.WinnerPlayersExpectationToWin = EloExpectationToWin(winnerTeamData.PlayersMmr, loserTeamData.PlayersMmr);
        winnerTeamData.WinnerCmdrExpectationToWin = loserTeamData.WinnerCmdrExpectationToWin = EloExpectationToWin(winnerTeamData.CmdrComboMmr, loserTeamData.CmdrComboMmr);
    }

    private void CalculateRatingsDeltas(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData)
    {
        for (int i = 0; i < teamData.Players.Length; i++)
        {
            var plRatings = playerRatingsCmdr[teamData.Players[i].Player.PlayerId];
            var lastPlRating = plRatings.Last();
            double playerConsistency = lastPlRating.Consistency;

            double playerMmr = lastPlRating.Mmr;
            if (playerMmr > maxMmrCmdr)
            {
                maxMmrCmdr = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersMmr * teamData.Players.Length, playerMmr);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0);

            if (playerImpact > 1 || playerImpact < 0) {
            }

            teamData.PlayersMmrDelta[i] = CalculateMmrDelta(teamData.WinnerPlayersExpectationToWin, playerImpact, (useCommanderMmr ? (1 - teamData.WinnerCmdrExpectationToWin) : 1));
            teamData.PlayersConsistencyDelta[i] = consistencyDeltaMult * 2 * (teamData.WinnerPlayersExpectationToWin - 0.50);

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmrCmdr)) / startMmr;
            teamData.CmdrMmrDelta[i] = CalculateMmrDelta(teamData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);


            if (teamData.PlayersMmrDelta[i] > eloK) {
                throw new Exception("MmrDelta is bigger than eloK");
            }
        }
    }


    private void SetCommandersComboMmr(TeamData teamData)
    {
        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].Race;
                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.CmdrMmrDelta[playerIndex] / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.CmdrMmrDelta[playerIndex];
            }
        }
    }

    private double GetCommandersComboMmr(ReplayPlayerDsRDto[] teamPlayers)
    {
        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamPlayers.Length; playerIndex++)
        {
            var playerCmdr = teamPlayers[playerIndex].Race;

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamPlayers.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamPlayers[synergyPlayerIndex].Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, Opprace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamPlayers[antiSynergyPlayerIndex].OppRace;

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

    private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime startTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => /*r.DefaultFilter
                && */(r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic)
                && r.GameTime >= startTime)
            .OrderBy(o => o.GameTime)
                .ThenBy(r => r.ReplayId)
            .AsNoTracking()
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

                    SynergyMmr = FireMmrService.startMmr,
                    AntiSynergyMmr = FireMmrService.startMmr
                };
                context.CommanderMmrs.Add(cmdrMmr);
            }
        }

        await context.SaveChangesAsync();
    }
}
