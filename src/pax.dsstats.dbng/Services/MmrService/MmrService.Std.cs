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
        foreach (var replay in replayDsRDtos)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        return playerRatingsStd;
    }

    private Dictionary<int, List<DsRCheckpoint>> ContinueCalculateStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, List<ReplayDsRDto> newReplays)
    {
        if (!newReplays.Any())
        {
            return new();
        }

        foreach (var replay in newReplays)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        LatestReplayGameTime = newReplays.Last().GameTime;
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
            logger.LogDebug($"skipping wrong cmdr commanders");
            return;
        }

        ReplayData replayData = new(replay);
        if (replayData.WinnerTeamData.Players.Length != 3 || replayData.LoserTeamData.Players.Length != 3)
        {
            logger.LogDebug($"skipping wrong teamcounts");
            return;
        }

        SetReplayDataStd(playerRatingsStd, replayData);

        CalculateRatingsDeltasStd(playerRatingsStd, replayData, replayData.WinnerTeamData);
        CalculateRatingsDeltasStd(playerRatingsStd, replayData, replayData.LoserTeamData);

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
        FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);


        AddPlayersRankings(playerRatingsStd, replayData.WinnerTeamData, replayData.ReplayGameTime);
        AddPlayersRankings(playerRatingsStd, replayData.LoserTeamData, replayData.ReplayGameTime);

        if (IsProgressActive)
        {
            SetProgress(replayData, true);
        }
    }

    private void SetReplayDataStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayData replayData)
    {
        SetMmrsStd(playerRatingsStd, replayData.WinnerTeamData);
        SetMmrsStd(playerRatingsStd, replayData.LoserTeamData);

        SetExpectationsToWin(playerRatingsStd, replayData);

        SetConfidence(playerRatingsStd, replayData);
    }

    private void SetMmrsStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, TeamData teamData)
    {
        teamData.CmdrComboMmr = startMmr;
        teamData.PlayersAvgMmr = GetPlayersComboMmr(playerRatingsStd, teamData.Players);
    }

    private void CalculateRatingsDeltasStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayData replayData, TeamData teamData)
    {
        for (int i = 0; i < teamData.Players.Length; i++)
        {
            var plRatings = playerRatingsStd[GetMmrId(teamData.Players[i].ReplayPlayer.Player)];
            var lastPlRating = plRatings.Last();

            double playerConsistency = lastPlRating.Consistency;
            double playerConfidence = lastPlRating.Confidence;
            double playerMmr = lastPlRating.Mmr;

            if (playerMmr > maxMmrStd)
            {
                maxMmrStd = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersAvgMmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_confidence = GetCorrectedConfidenceFactor(playerConfidence, replayData.Confidence);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0)
                * (useConfidence ? factor_confidence : 1.0);

            var player = teamData.Players[i];
            player.PlayerMmrDelta = CalculateMmrDelta(replayData.WinnerPlayersExpectationToWin, playerImpact, 1);
            player.PlayerConsistencyDelta = consistencyDeltaMult * 2 * (replayData.WinnerPlayersExpectationToWin - 0.50);
            player.PlayerConfidenceDelta = Math.Abs(/*confidenceDeltaMult * */(player.PlayerMmrDelta / eloK));

            if (player.PlayerMmrDelta > eloK)
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
            .Where(r => r.Playercount == 6
                && r.Duration >= 210
                && r.WinnerTeam > 0
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
