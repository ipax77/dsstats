using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace dsstats.ratings;

public partial class RatingService(IServiceScopeFactory scopeFactory, ILogger<RatingService> logger) : IRatingService
{
    public async Task ProduceRatings(RatingCalcType ratingCalcType, bool recalc = false)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var t = ratingCalcType switch
        {
            RatingCalcType.Dsstats => ProduceDsstatsRatings(recalc).ConfigureAwait(false),
            RatingCalcType.Arcade => ProduceArcadeRatings(recalc).ConfigureAwait(false),
            RatingCalcType.Combo => ProduceComboRatings(recalc).ConfigureAwait(false),
            _ => throw new NotImplementedException(),
        };
        await t;
        sw.Stop();

        logger.LogWarning("{rating} ratings produced in {elapsed} ms ({min} min)",
            ratingCalcType.ToString(),
            sw.ElapsedMilliseconds,
            Math.Round(sw.Elapsed.TotalMinutes, 2));
    }

    private async Task CleanupPreRatings(ReplayContext context)
    {
        var preRatings = await context.ReplayRatings
            .Where(x => x.IsPreRating)
            .ToListAsync();

        if (preRatings.Count == 0)
        {
            return;
        }

        var replayIds = preRatings.Select(s => s.ReplayId).ToList();

        var replayPlayerIds = await context.Replays
            .Where(x => replayIds.Contains(x.ReplayId))
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.ReplayPlayerId)
            .ToListAsync();

        var replayPlayerRatings = await context.RepPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .ToListAsync();

        context.ReplayRatings.RemoveRange(preRatings);
        context.RepPlayerRatings.RemoveRange(replayPlayerRatings);

        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetCalcRatings(DateTime fromDate, ReplayContext context)
    {
        Dictionary<int, Dictionary<PlayerId, CalcRating>> calcRatings = new();

        var calcDtos = await GetDsstatsCalcDtos(new()
        {
            FromDate = fromDate,
            Skip = 0,
            Take = 101,
            Continue = true,
            GameModes = [3, 4, 7]
        });

        return await GetDsstatsMmrIdRatings(calcDtos);
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetDsstatsMmrIdRatings(List<CalcDto> calcDtos)
    {
        var ratingTypes = calcDtos.Select(s => s.GetRatingType())
            .Distinct()
            .ToList();

        var playerIds = calcDtos.SelectMany(s => s.Players).Select(s => s.PlayerId)
            .Distinct()
            .ToList();

        var toonIds = playerIds.Select(s => s.ToonId)
            .Distinct()
            .ToList();

        Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            };

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from pr in context.PlayerRatings
                    join p in context.Players on pr.PlayerId equals p.PlayerId
                    where ratingTypes.Contains((int)pr.RatingType)
                        && toonIds.Contains(p.ToonId)
                    select new
                    {
                        pr.RatingType,
                        PlayerId = new PlayerId(p.ToonId, p.RealmId, p.RegionId),
                        pr.Games,
                        pr.Wins,
                        pr.Mvp,
                        Mmr = pr.Rating,
                        pr.Consistency,
                        pr.Confidence,
                        pr.IsUploader,
                        pr.Main,
                        pr.MainCount
                    };

        var ratings = await query.ToListAsync();

        foreach (var playerId in playerIds)
        {
            var plRatings = ratings.Where(s => s.PlayerId == playerId).ToList();

            foreach (var plRating in plRatings)
            {
                mmrIdRatings[(int)plRating.RatingType][playerId] = new()
                {
                    PlayerId = playerId,
                    Games = plRating.Games,
                    Wins = plRating.Wins,
                    Mvps = plRating.Mvp,
                    Mmr = plRating.Mmr,
                    Consistency = plRating.Consistency,
                    Confidence = plRating.Confidence,
                    IsUploader = plRating.IsUploader,
                    CmdrCounts = GetFakeCmdrDic(plRating.Main, plRating.MainCount, plRating.Games)
                };
            }
        }

        return mmrIdRatings;
    }

    public static Dictionary<Commander, int> GetFakeCmdrDic(Commander main, int mainCount, int games)
    {
        Dictionary<Commander, int> cmdrDic = new();

        var mainPercentage = mainCount * 100.0 / games;

        if (mainPercentage > 99)
        {
            cmdrDic.Add(main, games);
            return cmdrDic;
        }

        if ((int)main <= 3)
        {
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.Std).Where(x => x != main))
            {
                cmdrDic[cmdr] = games / 3;
            }
        }
        else
        {
            int total = Data.GetCommanders(Data.CmdrGet.NoStd).Count;
            int avg = (games - mainCount) / (total - 1);
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoStd).Where(x => x != main))
            {
                cmdrDic[cmdr] = avg;
            }
        }

        cmdrDic[main] = (int)(((games - mainCount) * mainPercentage) / (100.0 - mainPercentage));
        return cmdrDic;
    }
}

public record RawCalcDto
{
    public int DsstatsReplayId { get; set; }
    public int Sc2ArcadeReplayId { get; set; }
    public DateTime GameTime { get; init; }
    public int GameMode { get; set; }
    public int Duration { get; init; }
    public int Maxkillsum { get; init; }
    public bool TournamentEdition { get; init; }
    public List<RawPlayerCalcDto> Players { get; init; } = new();

    public CalcDto GetCalcDto()
    {
        return new()
        {
            ReplayId = Math.Max(DsstatsReplayId, Sc2ArcadeReplayId),
            GameTime = GameTime,
            GameMode = GameMode,
            Duration = Duration,
            TournamentEdition = TournamentEdition,
            Players = Players.Select(s => new PlayerCalcDto()
            {
                ReplayPlayerId = s.ReplayPlayerId,
                GamePos = s.GamePos,
                PlayerResult = s.PlayerResult,
                IsLeaver = s.Duration < Duration - 90,
                IsMvp = s.Kills == Maxkillsum,
                Team = s.Team,
                Race = s.Race,
                PlayerId = s.PlayerId,
                IsUploader = s.IsUploader
            }).ToList(),
        };
    }
}

public record RawPlayerCalcDto
{
    public int ReplayPlayerId { get; init; }
    public int GamePos { get; init; }
    public int PlayerResult { get; init; }
    public int Duration { get; init; }
    public int Kills { get; init; }
    public int Team { get; init; }
    public Commander Race { get; init; }
    public PlayerId PlayerId { get; init; } = null!;
    public bool IsUploader { get; set; }
}