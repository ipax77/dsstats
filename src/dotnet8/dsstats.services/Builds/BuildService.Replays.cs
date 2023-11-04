using dsstats.db;
using dsstats.shared;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace dsstats.services;

public partial class BuildService
{
    public async Task<int> GetReplaysCount(BuildRequest request, CancellationToken token = default)
    {
        var replays = GetQueriableReplays(request);
        return await replays
            .Select(s => s.ReplayId)
            .Distinct()
            .CountAsync(token);

        //SELECT COUNT(*)
        //    FROM(
        //    SELECT DISTINCT `r0`.`ReplayId`
        //    FROM `ReplayPlayers` AS `r`
        //    INNER JOIN `Replays` AS `r0` ON `r`.`ReplayId` = `r0`.`ReplayId`
        //    LEFT JOIN `ReplayRatings` AS `r1` ON `r0`.`ReplayId` = `r1`.`ReplayId`
        //    LEFT JOIN `RepPlayerRatings` AS `r2` ON `r`.`ReplayPlayerId` = `r2`.`ReplayPlayerId`
        //    WHERE(((((`r0`.`GameTime` >= @__start_0) AND(`r1`.`LeaverType` = 0)) AND `r1`.`RatingType` IN(1, 3)) AND((`r`.`Duration` = 0) OR(`r0`.`Duration` > @__duration_2))) AND(`r`.`Race` = @__request_Interest_3)) AND(`r2`.`Rating` >= @__p_4)
    }

    public async Task<List<ReplayListDto>> GetReplays(BuildRequest request, int skip, int take, CancellationToken token)
    {
        var replays = GetQueriableReplays(request);

        var replaylist = await replays
            .OrderByDescending(o => o.GameTime)
            .Skip(skip)
            .Take(take)
            .Select(s => new ReplayListRatingDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = (GameMode)s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver,
                ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerListDto()
                {
                    Name = t.Name,
                    GamePos = t.GamePos,
                    Race = t.Race,
                    OppRace = t.OppRace,
                    ReplayPlayerRating = t.ComboReplayPlayerRating == null ? null : new ReplayPlayerRatingListDto()
                    {
                        RatingChange = t.ComboReplayPlayerRating.Change
                    },
                    Player = new PlayerId()
                    {
                        ToonId = t.Player.ToonId,
                        RealmId = t.Player.RealmId,
                        RegionId = t.Player.RegionId
                    }
                }).ToList()
            })
            .ToListAsync(token);

        return GetReplayListDtoWithPlayerInfo(replaylist, request);
    }

    private List<ReplayListDto> GetReplayListDtoWithPlayerInfo(List<ReplayListRatingDto> replays, BuildRequest request)
    {
        List<PlayerId> playerIds = request.PlayerNames
            .Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId)).ToList();

        List<ReplayListDto> replaylist = new();
        Dictionary<string, bool> hashSet = new();

        foreach (var replay in replays)
        {
            if  (hashSet.ContainsKey(replay.ReplayHash))
            {
                continue;
            }
            else
            {
                hashSet[replay.ReplayHash] = true;
            }

            ReplayPlayerInfo? info = null;

            var player = replay.ReplayPlayers
                .Where(x => x.Race == request.Interest)
                .Where(x => request.Versus == Commander.None ? true : x.OppRace == request.Versus)
                .Where(x => playerIds.Count == 0 ? true
                    : playerIds.Contains(new PlayerId(x.Player.ToonId, x.Player.RealmId, x.Player.RegionId)))
                .FirstOrDefault();

            if (player is not null)
            {
                info = new()
                {
                    Name = player.Name,
                    Pos = player.GamePos,
                    RatingChange = player.ReplayPlayerRating?.RatingChange ?? 0,
                    Commander = player.Race
                };
            }

            replaylist.Add(new()
            {
                GameTime = replay.GameTime,
                Duration = replay.Duration,
                WinnerTeam = replay.WinnerTeam,
                GameMode = replay.GameMode,
                ReplayHash = replay.ReplayHash,
                DefaultFilter = replay.DefaultFilter,
                CommandersTeam1 = replay.CommandersTeam1,
                CommandersTeam2 = replay.CommandersTeam2,
                MaxLeaver = replay.MaxLeaver,
                PlayerInfo = info
            });
        }
        return replaylist;
    }

    private IQueryable<Replay> GetQueriableReplays(BuildRequest request)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var replayPlayers = context.ReplayPlayers
            .Where(x => x.Replay.GameTime >= start)
            .Where(x => noEnd || x.Replay.GameTime < end)
            .Where(x => request.WithLeavers || x.Replay.ReplayRating!.LeaverType == LeaverType.None)
            .Where(x => ratingTypes.Contains(x.Replay.ReplayRating!.RatingType))
            .Where(x => x.Race == request.Interest);

        if (request.Versus != Commander.None)
        {
            replayPlayers = replayPlayers.Where(x => x.OppRace == request.Versus);
        }

        if (request.PlayerNames.Count == 0)
        {
            return GetRatingReplays(replayPlayers, request);
        }
        else
        {
            return GetPlayerReplays(replayPlayers, request);
        }
    }

    private IQueryable<Replay> GetRatingReplays(IQueryable<ReplayPlayer> replayPlayers, BuildRequest request)
    {
        if (request.FromRating > Data.MinBuildRating)
        {
            replayPlayers = replayPlayers.Where(x => x.ComboReplayPlayerRating!.Rating >= request.FromRating);
        }

        if (request.ToRating < Data.MaxBuildRating)
        {
            replayPlayers = replayPlayers.Where(x => x.ComboReplayPlayerRating!.Rating <= request.ToRating);
        }

        return replayPlayers.Select(s => s.Replay);
    }

    private IQueryable<Replay> GetPlayerReplays(IQueryable<ReplayPlayer> replayPlayers, BuildRequest request)
    {
        var predicate = PredicateBuilder.New<ReplayPlayer>();

        foreach (var player in request.PlayerNames)
        {
            predicate = predicate.Or(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId);
        }

        return replayPlayers.Where(predicate).Select(s => s.Replay);
    }
}
