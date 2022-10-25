using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;

public class FireMmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;

    public FireMmrService(IServiceProvider serviceProvider, IMapper mapper)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
    }

    // private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;

    private static readonly double eloK = 64; // default 32
    private static readonly double clip = eloK * 12.5;
    private static readonly double startMmr = 1000.0;

    private readonly Dictionary<int, List<DsRCheckpoint>> ratings = new();

    public async Task CalcMmmr()
    {
        await ClearRatings();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.Players)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Duration >= 90)
            .Where(r => r.Playercount == 6 && r.GameMode == GameMode.Commanders)
            .OrderBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);

        int count = 0;
        await replays.ForEachAsync(replay =>
        {

            var winnerTeam = replay.Players.Where(x => x.Team == replay.WinnerTeam);
            var loserTeam = replay.Players.Where(x => x.Team != replay.WinnerTeam);
            var leaverTeam = new List<ReplayPlayerDsRDto>().AsEnumerable();

            int correctedDuration = replay.Duration;
            if (replay.WinnerTeam == 0)
            {
                correctedDuration = replay.Players.Where(x => !x.IsUploader).Max(x => x.Duration);

                var uploaders = replay.Players.Where(x => x.IsUploader);

                winnerTeam = replay.Players.Where(x => !x.IsUploader && x.Duration >= uploaders.First().Duration - 100);
                //loserTeam = 
            }

            leaverTeam = replay.Players.Where(x => x.Duration <= correctedDuration - 89);


            var winnerTeamMmr = GetTeamMmr(winnerTeam, replay.GameTime, replay.Duration);
            var loserTeamMmr = GetTeamMmr(loserTeam, replay.GameTime, replay.Duration);


            var teamElo = ExpectationToWin(winnerTeamMmr, loserTeamMmr);

            (double[] winnersMmrDelta, double[] winnersConsistencyDelta) = CalculatePlayersDeltas(winnerTeam.Select(s => s.Player), true, teamElo, winnerTeamMmr);
            (double[] losersMmrDelta, double[] losersConsistencyDelta) = CalculatePlayersDeltas(loserTeam.Select(s => s.Player), false, teamElo, loserTeamMmr);

            FixMMR_Equality(winnersMmrDelta, losersMmrDelta);


            AddPlayersRankings(winnerTeam.Select(s => s.Player), winnersMmrDelta, winnersConsistencyDelta, replay.GameTime);
            AddPlayersRankings(loserTeam.Select(s => s.Player), losersMmrDelta, losersConsistencyDelta, replay.GameTime);

            count++;
        });

        await SetRatings();
    }

    private async Task SetRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int i = 0;
        foreach (var rating in ratings)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == rating.Key);
            player.Mmr = rating.Value.Last().Mmr;
            player.MmrOverTime = GetOverTimeRating(rating.Value);
            i++;
        }
        await context.SaveChangesAsync();
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

    private async Task ClearRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET Mmr = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET MmrStd = {startMmr}");
        await context.Database.ExecuteSqlRawAsync("UPDATE Players SET MmrOverTime = NULL");
        ratings.Clear();
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
        }
    }

    private static double GetCorrected_revConsistency(double raw_revConsistency)
    {
        return 1.0;

        //return ((1 - CONSISTENCY_IMPACT) + (Program.CONSISTENCY_IMPACT * raw_revConsistency));
    }

    private (double[], double[]) CalculatePlayersDeltas(IEnumerable<PlayerDsRDto> teamPlayers, bool winner, double teamElo, double teamMmr)
    {
        var playersMmrDelta = new double[teamPlayers.Count()];
        var playersConsistencyDelta = new double[teamPlayers.Count()];

        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = ratings[teamPlayers.ElementAt(i).PlayerId];
            double playerMmr = plRatings.Last().Mmr;
            double playerConsistency = plRatings.Last().Consistency;

            double factor_playerToTeamMates = PlayerToTeamMates(teamMmr, playerMmr);
            double factor_consistency = GetCorrected_revConsistency(1 - playerConsistency);

            double playerImpact = 1
                * factor_playerToTeamMates
                * factor_consistency;

            playersMmrDelta[i] = CalculateMmrDelta(teamElo, playerImpact);
            playersConsistencyDelta[i] = consistencyDeltaMult * 2 * (teamElo - 0.50);

            if (!winner)
            {
                playersMmrDelta[i] *= -1;
                playersConsistencyDelta[i] *= -1;
            }
        }
        return (playersMmrDelta, playersConsistencyDelta);
    }

    private void AddPlayersRankings(IEnumerable<PlayerDsRDto> teamPlayers, double[] playersMmrDelta, double[] playersConsistencyDelta, DateTime gameTime)
    {
        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = ratings[teamPlayers.ElementAt(i).PlayerId];

            double mmrBefore = plRatings.Last().Mmr;
            double consistencyBefore = plRatings.Last().Consistency;

            double mmrAfter = mmrBefore + playersMmrDelta[i];
            double consistencyAfter = consistencyBefore + playersConsistencyDelta[i];

            plRatings.Add(new DsRCheckpoint() { Mmr = mmrAfter, Consistency = consistencyAfter, Time = gameTime });
        }
    }

    private static double ExpectationToWin(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (playerTwoRating - playerOneRating)));
    }

    private static double CalculateMmrDelta(double teamElo, double playerImpact)
    {
        return (double)(eloK * 1.0/*mcv*/ * (1 - teamElo) * playerImpact);
    }

    private static double PlayerToTeamMates(double winnerTeamMmr, double playerMmr)
    {
        if (winnerTeamMmr == 0)
        {
            return (1.0 / 3);
        }

        return playerMmr / winnerTeamMmr;
    }

    private double GetTeamMmr(IEnumerable<ReplayPlayerDsRDto> replayPlayers, DateTime gameTime, int replayDuration)
    {
        double teamMmr = 0;

        foreach (var replayPlayer in replayPlayers)
        {
            if (!ratings.ContainsKey(replayPlayer.Player.PlayerId))
            {
                ratings[replayPlayer.Player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += ratings[replayPlayer.Player.PlayerId].Last().Mmr;
            }
        }

        return teamMmr / 3.0;
    }
}