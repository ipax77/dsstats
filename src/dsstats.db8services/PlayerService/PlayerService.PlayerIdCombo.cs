

using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdComboSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdArcadeGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdComboRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetArcadePlayerRatingChartData(playerId, ratingType, token),
            MvpInfo = await GetMvpInfo(playerId, ratingType)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0);

        return summary;
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
                        IsUploader = s.Player.UploaderId != null
                    }
                })
                .ToListAsync(token);
    }


}
