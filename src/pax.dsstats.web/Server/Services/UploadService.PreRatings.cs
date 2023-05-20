using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    public async Task SetPreRatings(Replay replay)
    {
        if ((DateTime.UtcNow - replay.GameTime) > TimeSpan.FromHours(1))
        {
            return;
        }

        var replayDsRDto = mapper.Map<ReplayDsRDto>(replay);
        var ratingType = MmrService.GetRatingType(replayDsRDto);

        if (ratingType == RatingType.None)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var calcRatings = await GetCalcRatings(replay, ratingType, context);
        var replayRating = MmrService.ProcessReplay(replayDsRDto, calcRatings, new(), new(false));

        if (replayRating == null)
        {
            return;
        }

        await SavePreRating(replay, replayRating, context);
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

        context.ReplayRatings.Add(replayRating);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, CalcRating>> GetCalcRatings(Replay replay, RatingType ratingType, ReplayContext context)
    {
        Dictionary<int, CalcRating> calcRatings = new();


        var playerIds = replay.ReplayPlayers.Select(s => s.Player.PlayerId).ToList();

        var calcDtos = await context.PlayerRatings
            .Where(x => x.RatingType == ratingType
                && playerIds.Contains(x.Player.PlayerId))
            .ProjectTo<PlayerRatingReplayCalcDto>(mapper.ConfigurationProvider)
            .ToListAsync();
                
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            var calcDto = calcDtos.FirstOrDefault(f => f.Player.PlayerId == replayPlayer.Player.PlayerId);

            CalcRating calcRating = new()
            {
                PlayerId = replayPlayer.Player.PlayerId,
                Games = calcDto?.Games ?? 0,
                Mmr = calcDto?.Rating ?? 1000.0,
                Consistency = calcDto?.Consistency ?? 0,
                Confidence = calcDto?.Confidence ?? 0,
                IsUploader = replayPlayer.IsUploader
            };

            calcRatings[replayPlayer.Player.PlayerId] = calcRating;
        }

        return calcRatings;
    }
}
