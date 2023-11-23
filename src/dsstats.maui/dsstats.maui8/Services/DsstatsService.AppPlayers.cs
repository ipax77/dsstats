using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.Collections.Frozen;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly Dictionary<RequestNames, Dictionary<RatingType, AppPlayerRatingInfo>> appPlayerInfos = [];

    public async Task<FrozenDictionary<RequestNames, Dictionary<RatingType, AppPlayerRatingInfo>>> GetAppPlayers()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var requestNames = configService.GetRequestNames();
        var toonIds = requestNames.Select(s => s.ToonId).ToList();

        var dbRatings = await context.PlayerRatings
            .Where(x => toonIds.Contains(x.Player.ToonId))
            .Select(s => new
            {
                Player = new PlayerId(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId),
                s.RatingType,
                s.Rating,
                s.Pos,
                s.Games
            })
            .ToListAsync();

        foreach (var requestName in configService.GetRequestNames())
        {
            var playerRatings = dbRatings
                .Where(x => x.Player.ToonId == requestName.ToonId
                    && x.Player.RegionId == requestName.RegionId)
                .ToList();

            if (!appPlayerInfos.TryGetValue(requestName, out var infos)
                || infos is null)
            {
                infos = appPlayerInfos[requestName] = [];
            }

            foreach (var playerRating in playerRatings)
            {
                if (!infos.TryGetValue(playerRating.RatingType, out var rating)
                    || rating is null)
                {
                    rating = infos[playerRating.RatingType] = new() { RatingType = playerRating.RatingType };
                }

                rating.LocalRating = playerRating.Rating;
                rating.LocalPos = playerRating.Pos;
                rating.Games = playerRating.Games;
            }
        }
        return appPlayerInfos.ToFrozenDictionary();
    }
}

public record AppPlayerRatingInfo
{
    public RatingType RatingType { get; set; }
    public double LocalRating { get; set; }
    public double RemoteRating { get; set; }
    public int LocalPos { get; set; }
    public int RemotePos { get; set; }
    public int Games { get; set; }
}
