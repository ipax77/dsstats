
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace sc2dsstats.maui.Services;

public class RatingRepository : IRatingRepository
{
    private static Dictionary<int, RatingMemory> RatingMemory = new();
    private static Dictionary<string, RavenMmrChange> MmrChanges = new();

    public async Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            var ratingMemory = RatingMemory[toonId];
            RavenPlayerDetailsDto dto = new()
            {
                Name = ratingMemory.RavenPlayer.Name,
                ToonId = ratingMemory.RavenPlayer.ToonId,
                RegionId = ratingMemory.RavenPlayer.RegionId,
                IsUploader = ratingMemory.RavenPlayer.IsUploader,
            };

            if (ratingMemory.CmdrRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.CmdrRavenRating.Type,
                    Games = ratingMemory.CmdrRavenRating.Games,
                    Wins = ratingMemory.CmdrRavenRating.Wins,
                    Mvp = ratingMemory.CmdrRavenRating.Mvp,
                    TeamGames = ratingMemory.CmdrRavenRating.TeamGames,
                    Main = ratingMemory.CmdrRavenRating.Main,
                    MainPercentage = ratingMemory.CmdrRavenRating.MainPercentage,
                    Mmr = ratingMemory.CmdrRavenRating.Mmr,
                    MmrOverTime = ratingMemory.CmdrRavenRating.MmrOverTime,
                });
            }

            if (ratingMemory.StdRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.StdRavenRating.Type,
                    Games = ratingMemory.StdRavenRating.Games,
                    Wins = ratingMemory.StdRavenRating.Wins,
                    Mvp = ratingMemory.StdRavenRating.Mvp,
                    TeamGames = ratingMemory.StdRavenRating.TeamGames,
                    Main = ratingMemory.StdRavenRating.Main,
                    MainPercentage = ratingMemory.StdRavenRating.MainPercentage,
                    Mmr = ratingMemory.StdRavenRating.Mmr,
                    MmrOverTime = ratingMemory.StdRavenRating.MmrOverTime,
                });
            }
            return dto;
        }
        return await Task.FromResult(new RavenPlayerDetailsDto());
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        IQueryable<RatingMemory> ratingMemories;

        if (request.Type == RatingType.Cmdr)
        {
            ratingMemories = RatingMemory.Values
                .Where(x => x.CmdrRavenRating != null)
                .AsQueryable();
        }
        else if (request.Type == RatingType.Std)
        {
            ratingMemories = RatingMemory.Values
                .Where(x => x.StdRavenRating != null)
                .AsQueryable();
        }
        else
        {
            throw new NotImplementedException();
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratingMemories = ratingMemories.Where(x => x.RavenPlayer.Name.ToUpper().Contains(request.Search.ToUpper()));
        }

        var orderPre = request.Type == RatingType.Cmdr ? "CmdrRavenRating" : "StdRavenRating";
        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                ratingMemories = ratingMemories.AppendOrderBy($"{orderPre}.{order.Property}");
            }
            else
            {
                ratingMemories = ratingMemories.AppendOrderByDescending($"{orderPre}.{order.Property}");
            }
        }
#pragma warning disable CS8602
        return new RatingsResult
        {
            Count = ratingMemories.Count(),
            Players = ratingMemories.Skip(request.Skip).Take(request.Take)
                .Select(s => new RavenPlayerDto()
                {
                    Name = s.RavenPlayer.Name,
                    ToonId = s.RavenPlayer.ToonId,
                    RegionId = s.RavenPlayer.RegionId,
                    Rating = request.Type == RatingType.Cmdr ?
                        new()
                        {
                            Games = s.CmdrRavenRating.Games,
                            Wins = s.CmdrRavenRating.Wins,
                            Mvp = s.CmdrRavenRating.Mvp,
                            TeamGames = s.CmdrRavenRating.TeamGames,
                            Main = s.CmdrRavenRating.Main,
                            MainPercentage = s.CmdrRavenRating.MainPercentage,
                            Mmr = s.CmdrRavenRating.Mmr
                        }
                        : new()
                        {
                            Games = s.StdRavenRating.Games,
                            Wins = s.StdRavenRating.Wins,
                            Mvp = s.StdRavenRating.Mvp,
                            TeamGames = s.StdRavenRating.TeamGames,
                            Main = s.StdRavenRating.Main,
                            MainPercentage = s.StdRavenRating.MainPercentage,
                            Mmr = s.StdRavenRating.Mmr
                        }
                })
                .ToList()
        };
#pragma warning restore CS8602
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        var dtos = RatingMemory.Values
                    .Where(x => x.CmdrRavenRating != null)
                    .GroupBy(g => Math.Round(g.CmdrRavenRating?.Mmr ?? 0, 0))
                    .Select(s => new MmrDevDto
                    {
                        Count = s.Count(),
                        Mmr = s.Average(a => Math.Round(a.CmdrRavenRating?.Mmr ?? 0, 0))
                    })
                    .OrderBy(o => o.Mmr)
                    .ToList();
        return await Task.FromResult(dtos);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var dtos = RatingMemory.Values
            .Where(x => x.StdRavenRating != null)
            .GroupBy(g => Math.Round(g.StdRavenRating?.Mmr ?? 0, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.StdRavenRating?.Mmr ?? 0, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToList();
        return await Task.FromResult(dtos);
    }

    public async Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
    {
        if (MmrChanges.ContainsKey(replayHash))
        {
            return MmrChanges[replayHash].Changes;
        }
        return await Task.FromResult(new List<PlChange>());
    }

    public async Task<string?> GetToonIdName(int toonId)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            return RatingMemory[toonId].RavenPlayer.Name;
        }
        return await Task.FromResult("Anonymous");
    }

    public List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames)
    {
        if (ratingType == RatingType.Cmdr)
        {
            return RatingMemory.Values
                .Where(x => x.CmdrRavenRating != null && x.CmdrRavenRating.Games >= minGames)
                .OrderByDescending(o => o.CmdrRavenRating?.Wins * 100.0 / o.CmdrRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        else if (ratingType == RatingType.Std)
        {
            return RatingMemory.Values
                .Where(x => x.StdRavenRating != null && x.StdRavenRating.Games >= minGames)
                .OrderByDescending(o => o.StdRavenRating?.Wins * 100.0 / o.StdRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        return new();
    }

    public async Task<UpdateResult> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges)
    {
        foreach (var change in replayPlayerMmrChanges)
        {
            MmrChanges[change.Hash] = new RavenMmrChange() { Changes = change.Changes };
        }
        return await Task.FromResult(new UpdateResult() { Total = replayPlayerMmrChanges.Count });
    }

    public async Task<UpdateResult> UpdateRavenPlayers(Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings, RatingType ratingType)
    {
        foreach (var ent in ravenPlayerRatings)
        {
            if (RatingMemory.ContainsKey(ent.Key.ToonId))
            {
                var ratingMemory = RatingMemory[ent.Key.ToonId];
                if (ratingType == RatingType.Cmdr)
                {
                    ratingMemory.CmdrRavenRating = ent.Value;
                }
                else if (ratingType == RatingType.Std)
                {
                    ratingMemory.StdRavenRating = ent.Value;
                }
            }
            else
            {
                RatingMemory ratingMemory = new()
                {
                    RavenPlayer = ent.Key
                };
                if (ratingType == RatingType.Cmdr)
                {
                    ratingMemory.CmdrRavenRating = ent.Value;
                }
                else if (ratingType == RatingType.Std)
                {
                    ratingMemory.StdRavenRating = ent.Value;
                }
                RatingMemory[ent.Key.ToonId] = ratingMemory;
            }
        }
        return await Task.FromResult(new UpdateResult() { Total = ravenPlayerRatings.Count });
    }

    public List<int> GetNameToonIds(string name)
    {
        return RatingMemory.Values
            .Where(x => x.RavenPlayer.Name == name)
            .Select(s => s.RavenPlayer.ToonId)
            .ToList();
    }
}

internal record RatingMemory
{
    public RavenPlayer RavenPlayer { get; set; } = null!;
    public RavenRating? CmdrRavenRating { get; set; }
    public RavenRating? StdRavenRating { get; set; }
}