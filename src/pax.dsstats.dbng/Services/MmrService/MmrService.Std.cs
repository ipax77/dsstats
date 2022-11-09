
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
        foreach (var replay in replayDsRDtos) {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        return playerRatingsStd;
    }

    private void ProcessStdReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayDsRDto replay)
    {
        if (replay.ReplayPlayers.Any(a => (int)a.Race > 3)) {
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
        for (int i = 0; i < replayProcessData.LoserTeamData.Players.Length; i++) {
            replayProcessData.LoserTeamData.PlayersMmrDelta[i] *= -1;
            replayProcessData.LoserTeamData.PlayersConsistencyDelta[i] *= -1;
            replayProcessData.LoserTeamData.CmdrMmrDelta[i] *= -1;
        }

        AddPlayersRankings(playerRatingsStd, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        AddPlayersRankings(playerRatingsStd, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);
    }


    private static void SetStdMmrs(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, TeamData teamData, DateTime gameTime)
    {
        teamData.PlayersMmr = GetTeamMmr(playerRatingsStd, teamData.Players, gameTime);
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