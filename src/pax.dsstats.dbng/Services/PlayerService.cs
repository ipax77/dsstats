using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services;

public static class PlayerService
{
    public static async void GetExpectationCount(ReplayContext context)
    {
        int toonId = 226401; // PAX
        // int toonId = 1488340; // Feralan
        // int toonId = 8509078; // Firestorm

        var select = from r in context.Replays
                      from rp in r.ReplayPlayers
                      where rp.Player.ToonId == toonId
                       && r.ReplayRatingInfo != null
                       && r.ReplayRatingInfo.RatingType == shared.RatingType.CmdrTE && r.ReplayRatingInfo.LeaverType == LeaverType.None
                      select r;

        var replays = await select
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Include(i => i.ReplayRatingInfo)
                .ThenInclude(i => i.RepPlayerRatings)
            .ToListAsync();

        List<double> expectations = new();

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            if (replay.ReplayRatingInfo == null)
            {
                continue;
            }

            var team1Mmr = replay.ReplayRatingInfo.RepPlayerRatings
                .Where(x => x.GamePos <= 3)
                .Sum(s => s.Rating - s.RatingChange);

            var team2Mmr = replay.ReplayRatingInfo.RepPlayerRatings
                .Where(x => x.GamePos > 3)
                .Sum(s => s.Rating - s.RatingChange);

            var replayPlayer = replay.ReplayPlayers.First(x => x.Player.ToonId == toonId);
            var playerTeam = replayPlayer.GamePos <= 3 ? 1 : 2;

            double expectationToWin;

            if (playerTeam == 1)
            {
                expectationToWin = EloExpectationToWin(team1Mmr, team2Mmr);
            } else
            {
                expectationToWin = EloExpectationToWin(team2Mmr, team1Mmr);
            }
            expectations.Add(expectationToWin);
        }

        Console.WriteLine($"AvgExpectationToWin: {Math.Round(expectations.Average(), 2)}");
        Console.WriteLine($"below 60 ExpectationToWins: {Math.Round(expectations.Where(x => x <= 0.6).Count() * 100 / (double)expectations.Count, 2)}");

    }
    public static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip = 1600)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

}
