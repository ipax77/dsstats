
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class MmrService
{
    private readonly Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr = new();
    private List<CommanderMmr> commanderRatings = new();

    private const double AntiSynergyPercentage = 0.50;
    private const double SynergyPercentage = 1 - AntiSynergyPercentage;
    private const double OwnMatchupPercentage = 1.0 / 3;
    private const double MatesMatchupsPercentage = (1 - OwnMatchupPercentage) / 2;

    private async Task CalculateCmdr(DateTime startTime)
    {
        var replayDsRDtos = await GetCmdrReplayDsRDtos(startTime);

        foreach (var replay in replayDsRDtos)
        {
            ProcessCmdrReplay(replay);
        }
    }

    private void ProcessCmdrReplay(ReplayDsRDto replay)
    {
        if (replay.ReplayPlayers.Any(a => (int)a.Race <= 3))
        {
            logger.LogWarning($"skipping wrong cmdr commanders");
            return;
        }

        var winnerTeam = replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam);
        var loserTeam = replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam);

        if (winnerTeam.Count() != 3 || loserTeam.Count() != 3)
        {
            logger.LogWarning($"skipping wrong teamcounts");
            return;
        }

        var winnerTeamCommanders = winnerTeam.Select(x => x.Race).ToArray();
        var loserTeamCommanders = loserTeam.Select(x => x.Race).ToArray();

        var winnerTeamMmr = GetCmdrTeamMmr(winnerTeam, replay.GameTime);
        var loserTeamMmr = GetCmdrTeamMmr(loserTeam, replay.GameTime);

        var winnerTeamExpectationToWin = EloExpectationToWin(winnerTeamMmr, loserTeamMmr);

        var winnersCommandersComboMMR = GetCommandersComboMmr(winnerTeam);
        var losersCommandersComboMMR = GetCommandersComboMmr(loserTeam);
        var winnerCommandersComboExpectationToWin = EloExpectationToWin(winnersCommandersComboMMR, losersCommandersComboMMR);


    }

    private double GetCommandersComboMmr(IEnumerable<ReplayPlayerDsRDto> teamPlayers)
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

                synergySum += ((1 / 2) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamPlayers.Count(); antiSynergyPlayerIndex++) {
                var antiSynergyPlayerCmdr = teamPlayers.ElementAt(antiSynergyPlayerIndex).OppRace;

                var antiSynergy = commanderRatings
                    .First(x => x.Race == playerCmdr && x.OppRace == antiSynergyPlayerCmdr);

                if (playerIndex == antiSynergyPlayerIndex) {
                    antiSynergySum += (OwnMatchupPercentage * antiSynergy.AntiSynergyMmr);
                } else {
                    antiSynergySum += (MatesMatchupsPercentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (AntiSynergyPercentage * antiSynergySum)
                + (SynergyPercentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }

    private double GetCmdrTeamMmr(IEnumerable<ReplayPlayerDsRDto> replayPlayers, DateTime gameTime)
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

    private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime startTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.DefaultFilter
                && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic)
                && r.GameTime >= startTime)
            .OrderBy(o => o.ReplayId)
                .ThenBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
