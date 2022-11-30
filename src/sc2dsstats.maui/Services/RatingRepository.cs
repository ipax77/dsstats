
using dsstats.mmr;
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
                .Where(x => x.CmdrRavenRating != null && x.CmdrRavenRating.Games >= 20)
                .AsQueryable();
        }
        else if (request.Type == RatingType.Std)
        {
            ratingMemories = RatingMemory.Values
                .Where(x => x.StdRavenRating != null && x.StdRavenRating.Games >= 20)
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
            if (order.Property.EndsWith("Mvp"))
            {
                if (order.Ascending)
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Mvp * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Mvp * 100.0 / o.StdRavenRating.Games);
                    }
                }
                else
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Mvp * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Mvp * 100.0 / o.StdRavenRating.Games);
                    }
                }
            }
            else if (order.Property.EndsWith("Wins"))
            {
                if (order.Ascending)
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Wins * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Wins * 100.0 / o.StdRavenRating.Games);
                    }
                }
                else
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Wins * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Wins * 100.0 / o.StdRavenRating.Games);
                    }
                }
            }
            else
            {

                var property = order.Property.StartsWith("Rating.") ? order.Property[7..] : order.Property;
                if (order.Ascending)
                {
                    ratingMemories = ratingMemories.AppendOrderBy($"{orderPre}.{property}");
                }
                else
                {
                    ratingMemories = ratingMemories.AppendOrderByDescending($"{orderPre}.{property}");
                }
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, CancellationToken token = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];
            if (MmrChanges.TryGetValue(replay.ReplayHash, out var mmrChanges))
            {
                replay.MmrChange = mmrChanges.Changes
                    .FirstOrDefault(f => f.Pos == replay.PlayerPos)?.Change ?? 0;
            }
        }
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

    public async Task<UpdateResult> UpdateRavenPlayers(HashSet<PlayerDsRDto> players, Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        //Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings = GetRavenPlayers(players, mmrIdRatings);

        //foreach (var ent in ravenPlayerRatings) {
        //    ent.Value.Type = ratingType;
        //    if (RatingMemory.ContainsKey(ent.Key.ToonId)) {
        //        var ratingMemory = RatingMemory[ent.Key.ToonId];
        //        if (ratingType == RatingType.Cmdr) {

        //            ratingMemory.CmdrRavenRating = ent.Value;
        //        } else if (ratingType == RatingType.Std) {
        //            ratingMemory.StdRavenRating = ent.Value;
        //        }
        //    } else {
        //        RatingMemory ratingMemory = new() {
        //            RavenPlayer = ent.Key
        //        };
        //        if (ratingType == RatingType.Cmdr) {
        //            ratingMemory.CmdrRavenRating = ent.Value;
        //        } else if (ratingType == RatingType.Std) {
        //            ratingMemory.StdRavenRating = ent.Value;
        //        }
        //        RatingMemory[ent.Key.ToonId] = ratingMemory;
        //    }
        //}
        //return await Task.FromResult(new UpdateResult() { Total = ravenPlayerRatings.Count });
        return null;
    }


    //public static Dictionary<RavenPlayer, RavenRating> GetRavenPlayers(List<PlayerDsRDto> players, Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    //{
    //    Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings = new();

    //    foreach (var player in players) {
    //        var mmrId = GetMmrId(player);

    //        if (mmrIdRatings.ContainsKey(mmrId)) {
    //            var rating = mmrIdRatings[mmrId];
    //            (var main, var mainper) = rating.GetMain();
    //            RavenPlayer ravenPlayer = new() {
    //                PlayerId = player.PlayerId,
    //                Name = player.Name,
    //                ToonId = player.ToonId,
    //                RegionId = player.RegionId,
    //                IsUploader = rating.IsUploader

    //            };

    //            ravenPlayerRatings[ravenPlayer] = new() {
    //                Games = rating.Games,
    //                Wins = rating.Wins,
    //                Mvp = rating.Mvp,
    //                Main = main,
    //                MainPercentage = mainper,
    //                Mmr = rating.Mmr,
    //                MmrOverTime = GetDbMmrOverTime(rating.MmrOverTime),
    //                Consistency = rating.Consistency,
    //                Confidence = rating.Confidence,
    //            };
    //        }
    //    }
    //    return ravenPlayerRatings;
    //}

    public List<int> GetNameToonIds(string name)
    {
        return RatingMemory.Values
            .Where(x => x.RavenPlayer.Name == name)
            .Select(s => s.RavenPlayer.ToonId)
            .ToList();
    }


    // To Test
    public async Task<Dictionary<int, CalcRating>> GetCalcRatings(RatingType ratingType, List<ReplayPlayerDsRDto> replayPlayerDsRDtos)
    {
        Dictionary<int, CalcRating> calcRatings = new();

        foreach (var replayPlayerDsRDto in replayPlayerDsRDtos) {

            if (!RatingMemory.TryGetValue(replayPlayerDsRDto.Player.ToonId, out var ratingMemory)) {
                ratingMemory = RatingMemory[replayPlayerDsRDto.Player.ToonId] = new RatingMemory() {
                    RavenPlayer = new RavenPlayer() {
                        RegionId = replayPlayerDsRDto.Player.RegionId,
                        PlayerId = replayPlayerDsRDto.Player.PlayerId,
                        IsUploader = replayPlayerDsRDto.IsUploader,
                        Name = replayPlayerDsRDto.Player.Name,
                        ToonId = replayPlayerDsRDto.Player.ToonId
                    }
                };
            }

            if (ratingType == RatingType.Cmdr && ratingMemory.CmdrRavenRating != null) {
                calcRatings.Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.CmdrRavenRating));
            } else if (ratingType == RatingType.Std && ratingMemory.StdRavenRating != null) {
                calcRatings.Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.StdRavenRating));
            }
        }

        return calcRatings;
    }

    private static CalcRating GetCalcRating(RavenPlayer ravenPlayer, RavenRating ravenRating)
    {
        return new CalcRating() {
            IsUploader = ravenPlayer.IsUploader,
            Confidence = ravenRating?.Confidence ?? 0,
            Consistency = ravenRating?.Consistency ?? 0,
            Games = ravenRating?.Games ?? 0,
            TeamGames = ravenRating?.TeamGames ?? 0,
            Wins = ravenRating?.Wins ?? 0,
            Mvp = ravenRating?.Mvp ?? 0,

            Mmr = ravenRating?.Mmr ?? MmrService.startMmr,
            MmrOverTime = GetTimeRatings(ravenRating?.MmrOverTime),

            CmdrCounts = new(), // ToDo ???
        };
    }

    private static List<TimeRating> GetTimeRatings(string? mmrOverTime)
    {
        if (string.IsNullOrEmpty(mmrOverTime)) {
            return new();
        }

        List<TimeRating> timeRatings = new();

        foreach (var ent in mmrOverTime.Split('|', StringSplitOptions.RemoveEmptyEntries)) {
            var timeMmr = ent.Split(',');
            if (timeMmr.Length == 2) {
                if (double.TryParse(timeMmr[0], out double mmr)) {
                    timeRatings.Add(new TimeRating() {
                        Mmr = mmr,
                        Date = timeMmr[1]
                    });
                }
            }
        }
        return timeRatings;
    }
}

internal record RatingMemory
{
    public RavenPlayer RavenPlayer { get; set; } = null!;
    public RavenRating? CmdrRavenRating { get; set; }
    public RavenRating? StdRavenRating { get; set; }
}