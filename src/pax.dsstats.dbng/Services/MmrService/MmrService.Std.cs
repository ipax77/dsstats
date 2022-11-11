using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;


namespace pax.dsstats.dbng.Services;
public partial class MmrService
{
    private double maxMmrStd = startMmr; //ToDo - load for continuation!!!

    private async Task<Dictionary<int, List<DsRCheckpoint>>> ReCalculateStd(DateTime startTime, DateTime endTime)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();

        var replayDsRDtos = await GetStdReplayDsRDtos(startTime, endTime);
        return ContinueCalculateStd(playerRatingsStd, replayDsRDtos);
    }
    private Dictionary<int, List<DsRCheckpoint>> ContinueCalculateStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, List<ReplayDsRDto> newReplays)
    {
        foreach (var replay in newReplays)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        LatestReplayGameTime = newReplays.LastOrDefault()?.GameTime ?? DateTime.MinValue;
        return playerRatingsStd;
    }


    private void ProcessStdReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayDsRDto replay)
    {
        if (replay.WinnerTeam == 0)
        {
            return;
        }

        if (replay.ReplayPlayers.Any(a => (int)a.Race > 3))
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

        SetMmrsStd(playerRatingsStd, replayProcessData.WinnerTeamData, replayProcessData.ReplayGameTime);
        SetMmrsStd(playerRatingsStd, replayProcessData.LoserTeamData, replayProcessData.ReplayGameTime);

        SetExpectationsToWin(replayProcessData);

        CalculateRatingsDeltasStd(playerRatingsStd, replayProcessData, replayProcessData.WinnerTeamData);
        CalculateRatingsDeltasStd(playerRatingsStd, replayProcessData, replayProcessData.LoserTeamData);

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

        //SetCommandersComboMmr(replayProcessData.WinnerTeamData);
        //SetCommandersComboMmr(replayProcessData.LoserTeamData);
    }

    private void SetMmrsStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, TeamData teamData, DateTime gameTime)
    {
        teamData.CmdrComboMmr = 1.0;
        teamData.PlayersMmr = GetTeamMmr(playerRatingsCmdr, teamData.Players, gameTime);
    }

    private void CalculateRatingsDeltasStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsCmdr, ReplayProcessData replayProcessData, TeamData teamData)
    {
        for (int i = 0; i < teamData.Players.Length; i++)
        {
            var plRatings = playerRatingsCmdr[GetMmrId(teamData.Players[i].Player)];
            var lastPlRating = plRatings.Last();
            double playerConsistency = lastPlRating.Consistency;

            double playerMmr = lastPlRating.Mmr;
            if (playerMmr > maxMmrStd)
            {
                maxMmrStd = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersMmr * teamData.Players.Length, playerMmr);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0);

            if (playerImpact > 1 || playerImpact < 0)
            {
            }

            teamData.PlayersMmrDelta[i] = CalculateMmrDelta(replayProcessData.WinnerPlayersExpectationToWin, playerImpact, (useCommanderMmr ? (1 - replayProcessData.WinnerCmdrExpectationToWin) : 1));
            teamData.PlayersConsistencyDelta[i] = consistencyDeltaMult * 2 * (replayProcessData.WinnerPlayersExpectationToWin - 0.50);

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmrStd)) / startMmr;
            teamData.CmdrMmrDelta[i] = CalculateMmrDelta(replayProcessData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);


            if (teamData.PlayersMmrDelta[i] > eloK)
            {
                throw new Exception("MmrDelta is bigger than eloK");
            }
        }
    }

    private async Task<List<ReplayDsRDto>> GetStdReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.DefaultFilter
                && (r.GameMode == GameMode.Standard))
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
}
