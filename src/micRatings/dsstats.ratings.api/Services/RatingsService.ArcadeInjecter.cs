using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Text.Json;
using System.IO;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    private readonly string injectDicFile = "/data/ds/injectdic.json";

    public async Task<Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>>> GetArcadeInjectDic()
    {
        if (File.Exists(injectDicFile))
        {
            return GetFromJson();
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>> injectDic = new();

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
                                ProfileId = t.ArcadePlayer.ProfileId,
                                RegionId = t.ArcadePlayer.RegionId,
                                RealmId = t.ArcadePlayer.RealmId,
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
                        var playerId = new ArcadePlayerId(rp.ArcadePlayer.ProfileId, rp.ArcadePlayer.RealmId, rp.ArcadePlayer.RegionId);
                        if (!injectDic[ratingType].TryGetValue(playerId, out var plEnt))
                        {
                            plEnt = injectDic[ratingType][playerId] = new();
                        }
                        plEnt[dateInt] = rp.ArcadeReplayPlayerRating.Rating;
                    }
                }
            }
        }

        SaveToJson(injectDic);

        return injectDic;
    }

    private void SaveToJson(Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>> injectDic)
    {
        InjectJson injectJson = new InjectJson();

        foreach (RatingType ratingType in injectDic.Keys)
        {
            foreach (ArcadePlayerId playerId in injectDic[ratingType].Keys)
            {
                injectJson.Players.Add(new()
                {
                    RatingType = ratingType,
                    ProfileId = playerId.ProfileId,
                    RealmId = playerId.RealmId,
                    RegionId = playerId.RegionId,
                    Ratings = injectDic[ratingType][playerId].Select(s => new InjectJsonRating()
                    {
                        DateInt = s.Key,
                        Rating = s.Value
                    }).ToList()
                });
            }
        }

        var json = JsonSerializer.Serialize(injectJson);
        File.WriteAllText(injectDicFile, json);
    }

    private Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>> GetFromJson()
    {
        InjectJson injectJson = JsonSerializer.Deserialize<InjectJson>(File.ReadAllText(injectDicFile)) ?? new();

        Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>> injectDic = new();

        foreach (var player in injectJson.Players)
        {
            if (!injectDic.ContainsKey(player.RatingType))
            {
                injectDic[player.RatingType] = new();
            }
            var playerId = new ArcadePlayerId(player.ProfileId, player.RealmId, player.RegionId);
            injectDic[player.RatingType][playerId] = player.Ratings.ToDictionary(k => k.DateInt, v => v.Rating);
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
    public int RegionId { get; set; }
    public int RealmId { get; set; }
}

public record InjectReplayPlayerRating
{
    public float Rating { get; set; }
}


public record InjectJson
{
    public List<InjectJsonPlayer> Players { get; set; } = new();
}

public record InjectJsonPlayer
{
    public RatingType RatingType { get; set; }
    public int ProfileId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public List<InjectJsonRating> Ratings { get; set; } = new();
}

public record InjectJsonRating
{
    public int DateInt { get; set; }
    public double Rating { get; set; }
}