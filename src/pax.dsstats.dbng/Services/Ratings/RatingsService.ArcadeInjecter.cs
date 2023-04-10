using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.Text.Json;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    private readonly string injectDicFile = "/data/ds/injectdic.json";

    public async Task<Dictionary<RatingType, Dictionary<int, Dictionary<int, double>>>> GetArcadeInjectDic()
    {
        if (File.Exists(injectDicFile))
        {
            var injectDicFromJson = JsonSerializer
                .Deserialize<Dictionary<RatingType, Dictionary<int, Dictionary<int, double>>>>(File.ReadAllText(injectDicFile));
            return injectDicFromJson ?? new();
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<RatingType, Dictionary<int, Dictionary<int, double>>> injectDic = new();

        var ratingTypes = new List<RatingType>() { RatingType.Cmdr, RatingType.Std };

        DateTime startTime = new DateTime(2021, 1, 31);
        DateTime endTime = DateTime.Today.AddDays(2);
        DateTime stepTime = startTime;

        while (stepTime < endTime)
        {
            startTime = stepTime;
            stepTime = stepTime.AddYears(1);
            foreach (var ratingType in ratingTypes)
            {
                injectDic[ratingType] = new();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var replays = await context.ArcadeReplays
                    .Where(x => x.CreatedAt >= startTime && x.CreatedAt < endTime
                        && x.ArcadeReplayRating != null && x.ArcadeReplayRating.RatingType == ratingType)
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
        }

        var json = JsonSerializer.Serialize(injectDic);
        File.WriteAllText(injectDicFile, json);

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
