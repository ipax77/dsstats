

using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<PlayerDetailSummary> GetPlayerPlayerIdComboSummary(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        PlayerDetailSummary summary = new()
        {
            GameModesPlayed = await GetPlayerIdArcadeGameModeCounts(playerId, token),
            Ratings = await GetPlayerIdComboRatings(playerId, token),
            Commanders = await GetPlayerIdCommandersPlayed(playerId, ratingType, token),
            ChartDtos = await GetPlayerRatingChartData(playerId, RatingCalcType.Combo, ratingType, token),
            MvpInfo = await GetMvpInfo(playerId, ratingType)
        };

        (summary.CmdrPercentileRank, summary.StdPercentileRank) =
            await GetPercentileRank(
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Cmdr)?.Pos ?? 0,
                summary.Ratings.FirstOrDefault(f => f.RatingType == RatingType.Std)?.Pos ?? 0,
                RatingCalcType.Combo);

        return summary;
    }

    private async Task<List<PlayerRatingDetailDto>> GetPlayerIdComboRatings(PlayerId playerId, CancellationToken token)
    {
        return await context.ComboPlayerRatings
                .Where(x => x.Player!.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId)
                .Select(s => new PlayerRatingDetailDto()
                {
                    RatingType = s.RatingType,
                    Rating = Math.Round(s.Rating, 2),
                    Pos = s.Pos,
                    Games = s.Games,
                    Wins = s.Wins,
                    Consistency = Math.Round(s.Consistency, 2),
                    Confidence = Math.Round(s.Confidence, 2),
                    Player = new PlayerRatingPlayerDto()
                    {
                        Name = s.Player.Name,
                        ToonId = s.Player.ToonId,
                        RegionId = s.Player.RegionId,
                        RealmId = s.Player.RealmId,
                        IsUploader = s.Player.UploaderId != null
                    }
                })
                .ToListAsync(token);
    }

    public async Task<PlayerRatingDetails> GetPlayerIdComboPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        // return await DEBUGGetPlayerRatingDetails(toonId, ratingType, token);

        return new()
        {
            Teammates = await GetPlayerIdComboPlayerTeammates(context, playerId, ratingType, token),
            Opponents = await GetPlayerIdComboPlayerOpponents(context, playerId, ratingType, token),
            AvgTeamRating = await GetPlayerIdComboTeamRating(context, playerId, ratingType, true, token),
            CmdrsAvgGain = await GetPlayerIdComboPlayerCmdrAvgGain(playerId, ratingType, TimePeriod.Past90Days, token)
        };
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdComboPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(timePeriod);
        bool noEnd = endTime < DateTime.Today.AddDays(-2);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && r.GameTime > startTime
                        && (noEnd || rp.Replay.GameTime < endTime)
                        && rr.RatingType == ratingType
                    group new { rp, rpr } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                    };

        var items = await group.ToListAsync(token);

        if (ratingType == RatingType.Cmdr || ratingType == RatingType.CmdrTE)
        {
            items = items.Where(x => (int)x.Commander > 3).ToList();
        }
        else if (ratingType == RatingType.Std || ratingType == RatingType.StdTE)
        {
            items = items.Where(x => (int)x.Commander <= 3).ToList();
        }
        return items;
    }

    private async Task<double> GetPlayerIdComboTeamRating(ReplayContext context, PlayerId playerId, RatingType ratingType, bool inTeam, CancellationToken token)
    {
        var teamRatings = from p in context.Players
                          from rp in p.ReplayPlayers
                          join r in context.Replays on rp.ReplayId equals r.ReplayId
                          from t in r.ReplayPlayers
                          join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                          join rpr in context.ComboReplayPlayerRatings on t.ReplayPlayerId equals rpr.ReplayPlayerId
                          where p.ToonId == playerId.ToonId
                              && p.RealmId == playerId.RealmId
                              && p.RegionId == playerId.RegionId
                              && rr.RatingType == ratingType
                              && (!inTeam || t != rp)
                              && (inTeam ? t.Team == rp.Team : t.Team != rp.Team)
                              && t.Player.ToonId > 0
                          select rpr;

        var avgRating = await teamRatings
            .Select(s => s.Rating)
            .DefaultIfEmpty()
            .AverageAsync(token);
        return Math.Round(avgRating, 2);
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdComboPlayerTeammates(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var teammateGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from t in rp.Replay.ReplayPlayers
                            join rr in context.ComboReplayRatings on rp.ReplayId equals rr.ReplayId
                            join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                            where p.ToonId == playerId.ToonId
                                && p.RealmId == playerId.RealmId
                                && p.RegionId == playerId.RegionId
                                && rr.RatingType == ratingType
                            where t != rp && t.Team == rp.Team
                            group new { t, rpr } by new { t.Player.ToonId, t.Player.RealmId, t.Player.RegionId, t.Player.Name } into g
                            orderby g.Count() descending
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.t.PlayerResult == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2)
                            };


        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = s.AvgGain
        }).ToList();
    }

    private async Task<List<PlayerTeamResult>> GetPlayerIdComboPlayerOpponents(ReplayContext context, PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        var teammateGroup = from p in context.Players
                            from rp in p.ReplayPlayers
                            from o in rp.Replay.ReplayPlayers
                            join rr in context.ComboReplayRatings on rp.ReplayId equals rr.ReplayId
                            join rpr in context.ComboReplayPlayerRatings on o.ReplayPlayerId equals rpr.ReplayPlayerId
                            where p.ToonId == playerId.ToonId
                                    && p.RealmId == playerId.RealmId
                                    && p.RegionId == playerId.RegionId
                                && rr.RatingType == ratingType
                            where o.Team != rp.Team
                            group new { o, rpr } by new { o.Player.ToonId, o.Player.RealmId, o.Player.RegionId, o.Player.Name } into g
                            where g.Count() > 10
                            select new PlayerTeamResultHelper()
                            {
                                PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                                Name = g.Key.Name,
                                Count = g.Count(),
                                Wins = g.Count(c => c.o.PlayerResult == PlayerResult.Win),
                                AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2)
                            };

        var results = await teammateGroup
            .ToListAsync(token);


        return results.Select(s => new PlayerTeamResult()
        {
            Name = s.Name,
            PlayerId = s.PlayerId,
            Count = s.Count,
            Wins = s.Wins,
            AvgGain = s.AvgGain
        }).ToList();
    }
}
