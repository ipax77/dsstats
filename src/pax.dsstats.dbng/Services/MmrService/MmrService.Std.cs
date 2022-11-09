
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    private async Task<Dictionary<int, List<DsRCheckpoint>>> CalculateStd(DateTime startTime)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();
        maxMmr = startMmr;

        var replayDsRDtos = await GetStdReplayDsRDtos(startTime);
        foreach (var replay in replayDsRDtos)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        return playerRatingsStd;
    }

    private void ProcessStdReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayDsRDto replay)
    {
        if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3))
        {
            logger.LogWarning($"skipping wrong cmdr commanders");
            return;
        }

        ReplayProcessData replayProcessData = new(replay);

        SetStdMmrs(playerRatingsStd, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        SetStdMmrs(playerRatingsStd, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetExpectationsToWin(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);
        SetExpectationsToWin(replayProcessData.LoserTeamData, replayProcessData.WinnerTeamData);

        CalculateRatingsDeltas(playerRatingsStd, replayProcessData.WinnerTeamData);
        CalculateRatingsDeltas(playerRatingsStd, replayProcessData.LoserTeamData);

        FixMmrEquality(replayProcessData.WinnerTeamData, replayProcessData.LoserTeamData);

        // Adjust Loser delta
        for (int i = 0; i < replayProcessData.LoserTeamData.Players.Length; i++)
        {
            replayProcessData.LoserTeamData.PlayersMmrDelta[i] *= -1;
            replayProcessData.LoserTeamData.PlayersConsistencyDelta[i] *= -1;
            replayProcessData.LoserTeamData.CmdrMmrDelta[i] *= -1;
        }

        AddPlayersRankings(playerRatingsStd, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        AddPlayersRankings(playerRatingsStd, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetStdComboMmr(replayProcessData.WinnerTeamData);
        SetStdComboMmr(replayProcessData.LoserTeamData);
    }


    private void SetStdMmrs(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, TeamData teamData, DateTime gameTime)
    {
        teamData.CmdrComboMmr = GetStdComboMmr(teamData.Players);
        teamData.PlayersMmr = GetStdTeamMmr(playerRatingsStd, teamData.Players, gameTime);
    }

    private void SetStdComboMmr(TeamData teamData)
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

    private double GetStdComboMmr(ReplayPlayerDsRDto[] teamPlayers)
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

                synergySum += ((1 / 2) * synergy.SynergyMmr);
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

    private double GetStdTeamMmr(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayPlayerDsRDto[] replayPlayers, DateTime gameTime)
    {
        double teamMmr = 0;

        foreach (var replayPlayer in replayPlayers)
        {
            if (!playerRatingsStd.ContainsKey(replayPlayer.Player.PlayerId))
            {
                playerRatingsStd[replayPlayer.Player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatingsStd[replayPlayer.Player.PlayerId].Last().Mmr;
            }
        }
        return teamMmr / 3.0;
    }

    private async Task<List<ReplayDsRDto>> GetStdReplayDsRDtos(DateTime startTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.DefaultFilter
                && (r.GameMode == GameMode.Standard)
                && r.GameTime >= startTime)
        .OrderBy(o => o.GameTime)
                .ThenBy(r => r.ReplayId)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
