
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

        var winnersCommandersComboMMR = GetCmdrComboMmrNg(winnerTeam);
        var losersCommandersComboMMR = GetCommandersComboMMR(loserTeamCommanders, winnerTeamCommanders);
        var winnerCommandersComboExpectationToWin = EloExpectationToWin(winnersCommandersComboMMR, losersCommandersComboMMR);


    }

    private double GetCmdrComboMmrNg(IEnumerable<ReplayPlayerDsRDto> players)
    {
        foreach (var player in players)
        {
            var oppCmdrs = players.Select(s => s.OppRace).ToArray();
            var teamCmdrs = players.Select(s => s.Race).Distinct().ToArray();

            foreach (var oppCmdr in oppCmdrs)
            {
                var antiSynergyCommander = commanderRatings
                    .Where(x => (x.Commander_1 == player.Race && x.Commander_2 == oppCmdr)
                        || (x.Commander_1 == oppCmdr && x.Commander_2 == player.Race))
                    .FirstOrDefault();

                if (antiSynergyCommander == null)
                {
                    throw new ArgumentNullException(nameof(antiSynergyCommander));
                }

                double antiSynergyMmr;
                if ((antiSynergyCommander.Commander_1 == player.Race) && (antiSynergyCommander.Commander_2 == oppCmdr))
                {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_1;
                }
                else if ((antiSynergyCommander.Commander_2 == player.Race) && (antiSynergyCommander.Commander_1 == oppCmdr))
                {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_2;
                }
            }
        }
        return 0;
    }

    private double GetCommandersComboMMR(Commander[] synergyCmdrs, Commander[] antiSynergyCmdrs)
    {
        double[] commandersComboMMR = new double[3];

        for (int i = 0; i < 3; i++)
        {

            double antiSynergySum = 0;
            double synergySum = 0;

            for (int k = 0; k < 3; k++)
            {

                var synCmdr = synergyCmdrs[i];
                var antiSynCmdr = antiSynergyCmdrs[k];

                var antiSynergyCommander = commanderRatings
                    .Where(x => (x.Commander_1 == synCmdr && x.Commander_2 == antiSynCmdr)
                        || (x.Commander_1 == antiSynCmdr && x.Commander_2 == synCmdr))
                    .FirstOrDefault();

                if (antiSynergyCommander == null)
                {
                    throw new ArgumentNullException(nameof(antiSynergyCommander));
                }

                double antiSynergyMmr;
                if ((antiSynergyCommander.Commander_1 == synergyCmdrs[i]) && (antiSynergyCommander.Commander_2 == antiSynergyCmdrs[k]))
                {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_1;
                }
                else if ((antiSynergyCommander.Commander_2 == synergyCmdrs[i]) && (antiSynergyCommander.Commander_1 == antiSynergyCmdrs[k]))
                {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_2;
                }
                else throw new Exception();

                if (i == k)
                {
                    antiSynergySum += (OwnMatchupPercentage * antiSynergyMmr);
                }
                else
                {
                    antiSynergySum += (MatesMatchupsPercentage * antiSynergyMmr);


                    CommanderMmr synergyCommander = this.commanderRatings
                    .Where(c =>
                        ((c.Commander_1 == synergyCmdrs[i]) && (c.Commander_2 == synergyCmdrs[k])) ||
                        ((c.Commander_2 == synergyCmdrs[i]) && (c.Commander_1 == synergyCmdrs[k])))
                    .FirstOrDefault()!;

                    synergySum += (0.5 * synergyCommander.SynergyMmr);
                }
            }

            commandersComboMMR[i] = 0
                + (AntiSynergyPercentage * antiSynergySum)
                + (SynergyPercentage * synergySum);
        }

        return commandersComboMMR.Sum() / 3;
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
