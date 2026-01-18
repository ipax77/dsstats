using System.Collections.Frozen;

namespace dsstats.shared.Extensions;

public static class RequestExtensions
{
    public static Dictionary<string, object?> BuildQueryParams(this ReplaysRequest request, FrozenDictionary<string, string> paramMap)
    {
        Dictionary<string, object?> queryDic = new();

        queryDic[paramMap[nameof(ReplaysRequest.RatingType)]] =
            request.RatingType != RatingType.All ? (int)request.RatingType : null;

        queryDic[paramMap[nameof(ReplaysRequest.Name)]] =
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name;

        queryDic[paramMap[nameof(ReplaysRequest.Commander)]] =
            string.IsNullOrWhiteSpace(request.Commander) ? null : request.Commander;

        queryDic[paramMap[nameof(ReplaysRequest.LinkCommanders)]] =
            request.LinkCommanders ? true : null;

        queryDic[paramMap[nameof(ReplaysRequest.ReplayHash)]] =
            string.IsNullOrWhiteSpace(request.ReplayHash) ? null : request.ReplayHash;


        return queryDic;
    }

    public static Dictionary<string, object?> BuildQueryParams(
    this ArcadeReplaysRequest request,
    FrozenDictionary<string, string> paramMap)
    {
        var queryDic = new Dictionary<string, object?>();

        queryDic[paramMap[nameof(ArcadeReplaysRequest.Name)]] =
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name;

        queryDic[paramMap[nameof(ArcadeReplaysRequest.ReplayHash)]] =
            string.IsNullOrWhiteSpace(request.ReplayHash) ? null : request.ReplayHash;

        return queryDic;
    }

    public static Dictionary<string, object?> BuildQueryParams(
    this PlayerRatingsRequest request,
    FrozenDictionary<string, string> paramMap)
    {
        var queryDic = new Dictionary<string, object?>();

        queryDic[paramMap[nameof(PlayerRatingsRequest.RatingType)]] =
            request.RatingType != RatingType.All ? (int)request.RatingType : null;

        queryDic[paramMap[nameof(PlayerRatingsRequest.Name)]] =
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name;

        queryDic[paramMap[nameof(PlayerRatingsRequest.RegionId)]] =
            request.RegionId != 0 ? request.RegionId : null;

        queryDic[paramMap[nameof(PlayerRatingsRequest.ToonIdString)]] =
            string.IsNullOrEmpty(request.ToonIdString) ? null : request.ToonIdString;

        return queryDic;
    }

    public static Dictionary<string, object?> BuildQueryParams(this BuildsRequest request, FrozenDictionary<string, string> paramMap)
    {
        var queryDic = new Dictionary<string, object?>();

        queryDic[paramMap[nameof(BuildsRequest.RatingType)]] =
            request.RatingType != RatingType.All ? (int)request.RatingType : null;

        queryDic[paramMap[nameof(BuildsRequest.TimePeriod)]] =
            request.TimePeriod != TimePeriod.Last90Days ? (int)request.TimePeriod : null;

        queryDic[paramMap[nameof(BuildsRequest.Interest)]] =
            request.Interest != Commander.Abathur ? request.Interest.ToString() : null;

        queryDic[paramMap[nameof(BuildsRequest.Versus)]] =
            request.Versus != Commander.None ? request.Versus.ToString() : null;

        queryDic[paramMap[nameof(BuildsRequest.FromRating)]] =
            request.FromRating != 2000 ? request.FromRating : null;

        queryDic[paramMap[nameof(BuildsRequest.ToRating)]] =
            request.ToRating != Data.MaxBuildRating ? request.ToRating : null;

        queryDic[paramMap[nameof(BuildsRequest.Breakpoint)]] =
            request.Breakpoint != Breakpoint.Min10 ? (int)request.Breakpoint : null;

        //queryDic[paramMap[nameof(BuildsRequest.WithLeavers)]] =
        //    request.WithLeavers != false ? request.WithLeavers : null;

        //queryDic[paramMap[nameof(BuildsRequest.WithSpawnInfo)]] =
        //    request.WithSpawnInfo != false ? request.WithSpawnInfo : null;

        return queryDic;
    }
}
