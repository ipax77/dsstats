using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        if (request.ComboRating)
        {
            return await GetComboRatingsCount(request, token);
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        return await ratings.CountAsync(token);
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        if (request.ComboRating)
        {
            return await GetComboRatings(request, token);
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        ratings = SortPlayerRatings(request, ratings);

        var lratings = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        return new()
        {
            Players = lratings
        };
    }

    private IQueryable<PlayerRating> SortPlayerRatings(RatingsRequest request, IQueryable<PlayerRating> ratings)
    {
        foreach (var order in request.Orders)
        {
            if (order.Property == "Rating")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Rating - o.ArcadeDefeatsSinceLastUpload * 25);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Rating - o.ArcadeDefeatsSinceLastUpload * 25);
                }
            }
            else if (order.Property == "Wins")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.Wins * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.Wins * 100.0 / o.Games);
                }
            }
            else if (order.Property == "Mvp")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.Mvp * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.Mvp * 100.0 / o.Games);
                }
            }
            else if (order.Property == "MainCount")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.MainCount * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.MainCount * 100.0 / o.Games);
                }
            }
            else
            {
                if (order.Ascending)
                {
                    ratings = ratings.AppendOrderBy(order.Property);
                }
                else
                {
                    ratings = ratings.AppendOrderByDescending(order.Property);
                }
            }
        }
        return ratings;
    }

    private IQueryable<PlayerRating> GetRequestRatingsQueirable(ReplayContext context, RatingsRequest request)
    {
        var ratings = context.PlayerRatings
            .Include(i => i.Player)
            .Where(x => x.Games > 20 && x.RatingType == request.Type);

        if (request.Uploaders && !Data.IsMaui)
        {
            ratings = ratings.Where(x => x.Player.UploaderId != null);
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => x.Player.Name.ToUpper().Contains(request.Search.Trim().ToUpper()));
        }

        return ratings;
    }

    public async Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = context.PlayerRatings
            .Where(x => request.ToonIds.Contains(x.Player.ToonId));

        if (request.RatingType != shared.RatingType.None)
        {
            ratings = ratings.Where(x => x.RatingType == request.RatingType);
        }

        return new ToonIdRatingResponse()
        {
            Ratings = await ratings
                .OrderByDescending(o => o.Rating)
                .Take(10)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token)
        };
    }

    public async Task<ToonIdRatingResponse> GetPlayerIdRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var toonIds = request.PlayerIds.Select(s => s.ToonId).ToList();

        var ratingQuery = context.PlayerRatings
            .Where(x => toonIds.Contains(x.Player.ToonId));

        if (request.RatingType != shared.RatingType.None)
        {
            ratingQuery = ratingQuery.Where(x => x.RatingType == request.RatingType);
        }

        var ratings = await ratingQuery
                .OrderByDescending(o => o.Rating)
                .Take(10)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);

        ratings = ratings.Where(x => request.PlayerIds.Any(a => a.ToonId == x.Player.ToonId
                                                            && a.RealmId == x.Player.RealmId
                                                            && a.RegionId == x.Player.RegionId))
                        .ToList();                                                                 


        return new ToonIdRatingResponse()
        {
            Ratings = ratings
        };
    }

    public async Task<List<PlayerRatingReplayCalcDto>> GetToonIdCalcRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == request.RatingType
                && request.ToonIds.Contains(x.Player.ToonId))
            .ProjectTo<PlayerRatingReplayCalcDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    public async Task<List<PlayerRatingReplayCalcDto>> GetPlayerIdCalcRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var toonIds = request.PlayerIds.Select(s => s.ToonId).ToList();

        var calcDtos = await context.PlayerRatings
            .Where(x => x.RatingType == request.RatingType
                && toonIds.Contains(x.Player.ToonId))
            .ProjectTo<PlayerRatingReplayCalcDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        calcDtos = calcDtos.Where(x => request.PlayerIds.Any(a => a.ToonId == x.Player.ToonId
                                                                  && a.RealmId == x.Player.RealmId
                                                                  && a.RegionId == x.Player.RegionId))
                            .ToList();
        return calcDtos;
    }

    public ReplayRatingDto? GetOnlineRating(ReplayDetailsDto replayDto, List<PlayerRatingReplayCalcDto> calcDtos)
    {
        if (replayDto.ReplayRatingInfo == null || !calcDtos.Any())
        {
            return null;
        }

        var replayDsRDto = mapper.Map<ReplayDsRDto>(mapper.Map<Replay>(replayDto));

        var dsrReplayPlayers = new List<ReplayPlayerDsRDto>(replayDsRDto.ReplayPlayers);
        replayDsRDto.ReplayPlayers.Clear();

        foreach (var dsrReplayPlayer in dsrReplayPlayers)
        {
            replayDsRDto.ReplayPlayers
                .Add(dsrReplayPlayer with { Player = dsrReplayPlayer.Player with { PlayerId = dsrReplayPlayer.Player.ToonId } });
        }

        Dictionary<int, CalcRating> calcRatings = new();

        foreach (var replayPlayer in replayDto.ReplayPlayers)
        {
            var calcDto = calcDtos.FirstOrDefault(f => f.Player.ToonId == replayPlayer.Player.ToonId);

            CalcRating calcRating = new()
            {
                PlayerId = replayPlayer.Player.ToonId,
                Games = calcDto?.Games ?? 0,
                Mmr = calcDto?.Rating ?? 1000.0,
                Consistency = calcDto?.Consistency ?? 0,
                Confidence = calcDto?.Confidence ?? 0,
                IsUploader = replayPlayer.IsUploader
            };
            calcRatings[replayPlayer.Player.ToonId] = calcRating;
        }
        var replayRating = MmrService.ProcessReplay(replayDsRDto, calcRatings, new(), new(false));

        if (replayRating == null)
        {
            return null;
        }

        foreach (var replayPlayer in replayDto.ReplayPlayers)
        {
            var replayPlayerRating = replayRating.RepPlayerRatings
                .FirstOrDefault(f => f.GamePos == replayPlayer.GamePos);

            if (replayPlayerRating == null)
            {
                continue;
            }

            replayPlayer.MmrChange = replayPlayerRating.RatingChange;
        }

        return replayRating;
    }
}
