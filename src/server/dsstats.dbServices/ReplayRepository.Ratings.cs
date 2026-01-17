using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices;

public partial class ReplayRepository
{
    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        return await context.ReplayRatings
            .Where(x => x.Replay!.ReplayHash == replayHash && x.RatingType == RatingType.All)
            .Select(s => new ReplayRatingDto()
            {
                RatingType = s.RatingType,
                LeaverType = s.LeaverType,
                ExpectedWinProbability = s.ExpectedWinProbability,
                IsPreRating = s.IsPreRating,
                AvgRating = s.AvgRating,
                ReplayPlayerRatings = s.ReplayPlayerRatings.Select(t => new ReplayPlayerRatingDto()
                {
                    RatingType = t.RatingType,
                    RatingBefore = t.RatingBefore,
                    RatingDelta = t.RatingDelta,
                    Games = t.Games,
                    ToonId = new()
                    {
                        Realm = t.Player!.ToonId.Realm,
                        Region = t.Player.ToonId.Region,
                        Id = t.Player.ToonId.Id
                    }
                }).ToList(),
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveReplayRatingAll(
        string replayHash,
        ReplayRatingDto ratingDto)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var replay = await context.Replays
            .Include(r => r.Ratings)
                .ThenInclude(rr => rr.ReplayPlayerRatings)
            .Include(r => r.Players)
                .ThenInclude(rp => rp.Ratings)
            .Include(r => r.Players)
                .ThenInclude(rp => rp.Player)
            .FirstOrDefaultAsync(r => r.ReplayHash == replayHash);

        if (replay == null)
            return;

        // ReplayRating (ALL)
        var replayRating = GetOrCreateReplayRating(replay, ratingDto);

        // Index DTO ratings by ToonId for fast lookup
        var dtoRatings = ratingDto.ReplayPlayerRatings.ToDictionary(
            r => (r.ToonId.Id, r.ToonId.Region, r.ToonId.Realm));

        foreach (var replayPlayer in replay.Players)
        {
            var key = (
                replayPlayer.Player!.ToonId.Id,
                replayPlayer.Player.ToonId.Region,
                replayPlayer.Player.ToonId.Realm);

            if (!dtoRatings.TryGetValue(key, out var playerDto))
                continue;

            GetOrCreateReplayPlayerRating(
                replayPlayer,
                playerDto,
                replayRating);
        }

        await context.SaveChangesAsync();
    }


    private static ReplayRating GetOrCreateReplayRating(
    Replay replay,
    ReplayRatingDto dto)
    {
        var rating = replay.Ratings
            .FirstOrDefault(r => r.RatingType == RatingType.All);

        if (rating != null)
        {
            rating.LeaverType = dto.LeaverType;
            rating.ExpectedWinProbability = dto.ExpectedWinProbability;
            rating.IsPreRating = dto.IsPreRating;
            rating.AvgRating = dto.AvgRating;
            return rating;
        }

        rating = new ReplayRating
        {
            RatingType = RatingType.All,
            LeaverType = dto.LeaverType,
            ExpectedWinProbability = dto.ExpectedWinProbability,
            IsPreRating = dto.IsPreRating,
            AvgRating = dto.AvgRating
        };

        replay.Ratings.Add(rating);
        return rating;
    }

    private static ReplayPlayerRating GetOrCreateReplayPlayerRating(
        ReplayPlayer replayPlayer,
        ReplayPlayerRatingDto dto,
        ReplayRating replayRating)
    {
        var rating = replayPlayer.Ratings
            .FirstOrDefault(r => r.RatingType == RatingType.All);

        if (rating == null)
        {
            rating = new ReplayPlayerRating
            {
                RatingType = RatingType.All,
                ReplayPlayerId = replayPlayer.ReplayPlayerId,
                PlayerId = replayPlayer.PlayerId
            };

            replayPlayer.Ratings.Add(rating);
        }

        // Update values
        rating.RatingBefore = dto.RatingBefore;
        rating.RatingDelta = dto.RatingDelta;
        rating.Games = dto.Games;

        // Required FK — must always be set
        rating.ReplayRating = replayRating;

        return rating;
    }
}
