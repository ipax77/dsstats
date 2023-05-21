using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{
    public async Task SetPreRatings(Replay replay)
    {
        if ((DateTime.UtcNow - replay.GameTime) > TimeSpan.FromHours(1))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        var waitResult = await ratingsService.ratingSs.WaitAsync(60000);
        if (waitResult == false)
        {
            return;
        }

        try
        {
            var replayDsRDto = await context.Replays
                .Where(x => x.ReplayId == replay.ReplayId)
                .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (replayDsRDto == null)
            {
                return;
            }

            var ratingType = MmrService.GetRatingType(replayDsRDto);

            if (ratingType == RatingType.None)
            {
                return;
            }

            var calcRatings = await GetCalcRatings(replay, ratingType, context);
            var replayRating = MmrService.ProcessReplay(replayDsRDto, calcRatings, new(), new(false));

            if (replayRating == null)
            {
                return;
            }

            await SavePreRating(replay, replayRating, context);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed generating preRating: {ex.Message}");
        }
        finally
        {
            ratingsService.ratingSs.Release();
        }
    }

    private async Task SavePreRating(Replay replay, ReplayRatingDto replayRatingDto, ReplayContext context)
    {
        var replayRating = mapper.Map<ReplayRating>(replayRatingDto);

        replayRating.ReplayId = replay.ReplayId;

        foreach (var replayPlayerRating in replayRating.RepPlayerRatings)
        {
            var replayPlayer = replay.ReplayPlayers
                .FirstOrDefault(f => f.GamePos == replayPlayerRating.GamePos);

            if (replayPlayer == null)
            {
                return;
            }

            replayPlayerRating.ReplayPlayerId = replayPlayer.ReplayPlayerId;
        }

        replayRating.IsPreRating = true;
        context.ReplayRatings.Add(replayRating);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, CalcRating>> GetCalcRatings(Replay replay, RatingType ratingType, ReplayContext context)
    {
        Dictionary<int, CalcRating> calcRatings = new();


        var playerIds = replay.ReplayPlayers.Select(s => s.PlayerId).ToList();

        var calcDtos = await context.PlayerRatings
            .Where(x => x.RatingType == ratingType
                && playerIds.Contains(x.Player.PlayerId))
            .ProjectTo<PlayerRatingReplayCalcDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            var calcDto = calcDtos.FirstOrDefault(f => f.Player.PlayerId == replayPlayer.PlayerId);

            CalcRating calcRating = new()
            {
                PlayerId = replayPlayer.PlayerId,
                Games = calcDto?.Games ?? 0,
                Mmr = calcDto?.Rating ?? 1000.0,
                Consistency = calcDto?.Consistency ?? 0,
                Confidence = calcDto?.Confidence ?? 0,
                IsUploader = replayPlayer.IsUploader
            };

            calcRatings[replayPlayer.PlayerId] = calcRating;
        }

        return calcRatings;
    }
}
