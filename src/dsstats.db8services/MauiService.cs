using dsstats.db8;
using dsstats.shared.Maui;
using System.Data.Entity;

namespace dsstats.db8services;

public class MauiService(ReplayContext context)
{
    public async Task<MauiRatingResponse> GetMauiRatings(MauiRatingRequest request, CancellationToken token = default)
    {
        if (request.PlayerIds.Count == 0 || request.PlayerIds.Count > 10)
        {
            return new();
        }

        return request.RatingCalcType switch
        {
            shared.RatingCalcType.Combo => await GetComboMauiRatingResponse(request, token),
            shared.RatingCalcType.Dsstats => await GetDsstatsMauiRatingResponse(request, token),
            shared.RatingCalcType.Arcade => await GetArcadeMauiRatingResponse(request, token),
            _ => new()
        };
    }

    private async Task<MauiRatingResponse> GetDsstatsMauiRatingResponse(MauiRatingRequest request, CancellationToken token)
    {
        List<MauiRatingInfo> infos = new();

        foreach (var playerId in request.PlayerIds)
        {
            MauiRatingInfo? info = await context.PlayerRatings
                .Where(x => x.RatingType == request.RatingType
                    && x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new MauiRatingInfo()
                {
                    RequestNames = new(s.Player.Name, s.Player.ToonId, s.Player.RegionId, s.Player.RealmId),
                    Rating = Convert.ToInt32(s.Rating),
                    Pos = s.Pos
                })
                .FirstOrDefaultAsync(token);

            if (info is not null)
            {
                infos.Add(info);
            }
        }
        return new()
        {
            RatingCalcType = shared.RatingCalcType.Dsstats,
            RatingType = request.RatingType,
            RatingInfos = infos
        };
    }

    private async Task<MauiRatingResponse> GetComboMauiRatingResponse(MauiRatingRequest request, CancellationToken token)
    {
        List<MauiRatingInfo> infos = new();

        foreach (var playerId in request.PlayerIds)
        {
            MauiRatingInfo? info = await context.ComboPlayerRatings
                .Where(x => x.RatingType == request.RatingType
                    && x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new MauiRatingInfo()
                {
                    RequestNames = new(s.Player.Name, s.Player.ToonId, s.Player.RegionId, s.Player.RealmId),
                    Rating = Convert.ToInt32(s.Rating),
                    Pos = s.Pos
                })
                .FirstOrDefaultAsync(token);

            if (info is not null)
            {
                infos.Add(info);
            }
        }
        return new()
        {
            RatingCalcType = shared.RatingCalcType.Dsstats,
            RatingType = request.RatingType,
            RatingInfos = infos
        };
    }

    private async Task<MauiRatingResponse> GetArcadeMauiRatingResponse(MauiRatingRequest request, CancellationToken token)
    {
        List<MauiRatingInfo> infos = new();

        foreach (var playerId in request.PlayerIds)
        {
            MauiRatingInfo? info = await context.ArcadePlayerRatings
                .Where(x => x.RatingType == request.RatingType
                    && x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new MauiRatingInfo()
                {
                    RequestNames = new(s.Player!.Name, s.Player.ToonId, s.Player.RegionId, s.Player.RealmId),
                    Rating = Convert.ToInt32(s.Rating),
                    Pos = s.Pos
                })
                .FirstOrDefaultAsync(token);

            if (info is not null)
            {
                infos.Add(info);
            }
        }
        return new()
        {
            RatingCalcType = shared.RatingCalcType.Dsstats,
            RatingType = request.RatingType,
            RatingInfos = infos
        };
    }
}
