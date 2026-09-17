using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);

        int playerId = importService.GetPlayerId(request.ToonId);

        var stats = await GetBasicPlayerStats(playerId, context, token);

        if (stats == null)
        {
            return new() { ToonId = request.ToonId };
        }

        var details = await GetRatingDetails(request.RatingType, playerId, context, token);

        stats.RatingDetails.Add(details);

        return stats with { ToonId = request.ToonId };
    }

    private static async Task<PlayerStatsResponse?> GetBasicPlayerStats(int playerId, DsstatsContext context, CancellationToken token)
    {
        return await context.Players
            .Where(x => x.PlayerId == playerId)
            .Select(x => new PlayerStatsResponse()
            {
                Name = x.Name,
                RegionId = x.ToonId.Region,
                Ratings = x.Ratings.Select(s => new PlayerRatingListItem()
                {
                    RatingType = s.RatingType,
                    PlayerId = s.PlayerId,
                    RegionId = s.Player!.ToonId.Region,
                    Name = s.Player!.Name,
                    Pos = s.Position,
                    Games = s.Games,
                    Wins = s.Wins,
                    Mvps = s.Mvps,
                    Change = s.Change,
                    Main = s.Main,
                    MainCount = s.MainCount,
                    Rating = s.Rating,
                    Cons = s.Consistency,
                    Conf = s.Confidence,
                }).ToList(),
            })
            .FirstOrDefaultAsync(token);
    }

}
