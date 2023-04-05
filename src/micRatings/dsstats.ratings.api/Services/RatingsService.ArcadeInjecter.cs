using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    public async Task<Dictionary<RatingType, Dictionary<int, Dictionary<int, double>>>> GetArcadeInjectDic()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<RatingType, Dictionary<int, Dictionary<int, double>>> injectDic = new();

        var ratingTypes = new List<RatingType>() { RatingType.Cmdr, RatingType.Std };

        foreach (var ratingType in ratingTypes)
        {
            injectDic[ratingType] = new();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replays = await context.ArcadeReplays
                .Where(x => x.ArcadeReplayRating != null && x.ArcadeReplayRating.RatingType == ratingType)
                .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.ArcadeReplayId)
                .Select(s => new InjectReplay()
                {
                    CreatedAt = s.CreatedAt,
                    ArcadeReplayPlayers = s.ArcadeReplayPlayers.Select(t => new InjectReplayPlayer()
                    {
                        ArcadePlayer = new InjectPlayer()
                        {
                            ProfileId = t.ArcadePlayer.ProfileId
                        },
                        ArcadeReplayPlayerRating = new InjectReplayPlayerRating()
                        {
                            Rating = t.ArcadeReplayPlayerRating.Rating 
                        }
                    }).ToList()
                })
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            foreach (var replay in replays)
            {
                int dateInt = int.Parse(replay.CreatedAt.ToString(@"yyyyMMdd"));
                foreach (var rp in replay.ArcadeReplayPlayers)
                {
                    if (!injectDic[ratingType].TryGetValue(rp.ArcadePlayer.ProfileId, out var plEnt))
                    {
                        plEnt = injectDic[ratingType][rp.ArcadePlayer.ProfileId] = new();
                    }
                    plEnt[dateInt] = rp.ArcadeReplayPlayerRating.Rating;
                }
            }
        }
        return injectDic;
    }
}

public record InjectReplay
{
    public DateTime CreatedAt { get; set; }
    public List<InjectReplayPlayer> ArcadeReplayPlayers { get; set; } = new();
}

public record InjectReplayPlayer
{
    public InjectPlayer ArcadePlayer { get; set; } = new();
    public InjectReplayPlayerRating ArcadeReplayPlayerRating { get; set; } = new();
}

public record InjectPlayer
{
    public int ProfileId { get; set; }
}

public record InjectReplayPlayerRating
{
    public float Rating { get; set; }
}
