

using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdComboSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdArcadeGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdComboRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetArcadePlayerRatingChartData(playerId, ratingType, token)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);

        return summary;
    }

    public async Task<List<ReplayPlayerChartDto>> GetComboPlayerRatingChartData(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var bab = from cpr in context.ComboReplayPlayerRatings
                  where cpr.ReplayPlayer.Replay.ComboReplayRating!.RatingType == ratingType
                    && cpr.ReplayPlayer.Player.ToonId == playerId.ToonId
                    && cpr.ReplayPlayer.Player.RealmId == playerId.RealmId
                    && cpr.ReplayPlayer.Player.RegionId == playerId.RegionId
                  group cpr.ReplayPlayer by new { Year = cpr.ReplayPlayer.Replay.GameTime.Year, Week = context.Week(cpr.ReplayPlayer.Replay.GameTime) } into g
                  select new ReplayPlayerChartDto()
                  {
                      Replay = new ReplayChartDto()
                      {
                          // GameTime = new DateTime(g.Key.Year, g.Key.Month, 1),
                          Year = g.Key.Year,
                          Week = g.Key.Week,
                      },
                      ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                      {
                          Rating = Math.Round(g.Average(a => a.ComboReplayPlayerRating!.Rating)),
                          Games = g.Max(m => m.ComboReplayPlayerRating!.Games)
                      }
                  };



        var replaysQuery = from p in context.ArcadePlayers
                           from rp in p.ArcadeReplayPlayers
                           orderby rp.ArcadeReplay!.CreatedAt
                           where p.ProfileId == playerId.ToonId
                            && p.RegionId == playerId.RegionId
                            && p.RealmId == playerId.RealmId
                            && rp.ArcadeReplay!.ArcadeReplayRating != null
                            && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                           group rp by new { Year = rp.ArcadeReplay!.CreatedAt.Year, Week = context.Week(rp.ArcadeReplay.CreatedAt) } into g
                           select new ReplayPlayerChartDto()
                           {
                               Replay = new ReplayChartDto()
                               {
                                   // GameTime = new DateTime(g.Key.Year, g.Key.Month, 1),
                                   Year = g.Key.Year,
                                   Week = g.Key.Week,
                               },
                               ReplayPlayerRatingInfo = new RepPlayerRatingChartDto()
                               {
                                   Rating = MathF.Round(g.Average(a => a.ArcadeReplayPlayerRating!.Rating)),
                                   Games = g.Max(m => m.ArcadeReplayPlayerRating!.Games)
                               }
                           };
        return await replaysQuery.ToListAsync(token);
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdComboRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.ComboPlayerRatings
                .Where(x => x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new PlayerRatingDetailDto()
                {
                    RatingType = s.RatingType, 
                    Rating = Math.Round(s.Rating, 2),
                    Pos = s.Pos,
                    Games = s.Games,
                    Wins = s.Wins,
                    Consistency = Math.Round(s.Consistency, 2),
                    Confidence = Math.Round(s.Confidence, 2),
                    Player = new PlayerRatingPlayerDto()
                    {
                        Name = s.Player.Name,
                        ToonId = s.Player.ToonId,
                        RegionId = s.Player.RegionId,
                        RealmId = s.Player.RealmId,
                    }
                })
                .ToListAsync(token);
    }


}
