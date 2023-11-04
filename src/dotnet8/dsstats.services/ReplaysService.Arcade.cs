
using AutoMapper.QueryableExtensions;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace dsstats.services;

public partial class ReplaysService
{
    public async Task<ArcadeReplayDto?> GetArcadeReplay(string hash, CancellationToken token = default)
    {
        var ents = hash.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length != 3)
        {
            return null;
        }

        if (int.TryParse(ents[0], out var regionId)
            && int.TryParse(ents[1], out var bucketId)
            && int.TryParse(ents[2], out var recordId))
        {
            return await context.ArcadeReplays
                .Where(x => x.RegionId == regionId
                    && x.BnetBucketId == bucketId
                    && x.BnetRecordId == recordId)
                .ProjectTo<ArcadeReplayDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(token);
        }
        return null;
    }

    private async Task<int> GetArcadeReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        var query = GetArcadeReplaysQueriable(request);

        return await query.CountAsync(token);
    }

    private async Task<ReplaysResponse> GetArcadeReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var query = GetArcadeReplaysQueriable(request);
            query = SortArcadeReplays(request, query);

            if (request.PlayerId is not null)
            {
                return await GetArcadeReplaysForPlayerId(query, request, token);
            }

            var replays = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(s => new ReplayListDto()
                {
                    GameTime = s.CreatedAt,
                    Duration = s.Duration,
                    WinnerTeam = s.WinnerTeam,
                    GameMode = (GameMode)s.GameMode,
                    TournamentEdition = s.TournamentEdition,
                    ReplayHash = $"{s.RegionId}|{s.BnetBucketId}|{s.BnetRecordId}",
                    DefaultFilter = false,
                    CommandersTeam1 = string.Empty,
                    CommandersTeam2 = string.Empty,
                    MaxLeaver = 0
                })
                .ToListAsync(token);

            return new() { Replays = replays };
        }
        catch (OperationCanceledException) { }

        return new();
    }

    private async Task<ReplaysResponse> GetArcadeReplaysForPlayerId(IQueryable<ArcadeReplay> query,
                                                              ReplaysRequest request,
                                                              CancellationToken token)
    {
        if (request.PlayerId is null)
        {
            return new();
        }

        var replays = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(s => new ReplayListRatingDto()
            {
                GameTime = s.CreatedAt,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = (GameMode)s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = $"{s.RegionId}|{s.BnetBucketId}|{s.BnetRecordId}",
                DefaultFilter = false,
                CommandersTeam1 = "",
                CommandersTeam2 = "",
                MaxLeaver = 0,
                ReplayPlayers = s.ArcadeReplayPlayers.Select(t => new ReplayPlayerListDto()
                {
                    Name = t.Name,
                    GamePos = t.SlotNumber,
                    Race = Commander.None,
                    ReplayPlayerRating = t.ArcadeReplayPlayerRating == null ? null : new ReplayPlayerRatingListDto()
                    {
                        RatingChange = t.ArcadeReplayPlayerRating.RatingChange
                    },
                    Player = new PlayerId()
                    {
                        ToonId = t.ArcadePlayer!.ProfileId,
                        RealmId = t.ArcadePlayer.RealmId,
                        RegionId = t.ArcadePlayer.RegionId
                    }
                }).ToList()
            })
            .ToListAsync(token);

        ReplaysResponse response = new() { PlayerId = request.PlayerId };

        foreach (var replay in replays)
        {
            ReplayPlayerInfo? info = null;

            var player = replay.ReplayPlayers.FirstOrDefault(f => f.Player.ToonId == request.PlayerId.ToonId
                && f.Player.RealmId == request.PlayerId.RealmId
                && f.Player.RegionId == request.PlayerId.RegionId);

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

            response.Replays.Add(new()
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

        return response;
    }

    private IQueryable<ArcadeReplay> SortArcadeReplays(ReplaysRequest request, IQueryable<ArcadeReplay> replays)
    {
        if (request.Orders.Count == 0)
        {
            return replays.OrderByDescending(o => o.CreatedAt);
        }

        foreach (var order in request.Orders)
        {
            var property = order.Property switch
            {
                "GameTime" => nameof(ArcadeReplay.CreatedAt),
                "Duration" => nameof(ArcadeReplay.Duration),
                "GameMode" => nameof(ArcadeReplay.GameMode),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(property))
            {
                continue;
            }

            if (order.Ascending)
            {
                replays = replays.AppendOrderBy(property);
            }
            else
            {
                replays = replays.AppendOrderByDescending(property);
            }
        }
        return replays;
    }

    private IQueryable<ArcadeReplay> GetArcadeReplaysQueriable(ReplaysRequest request)
    {
        var replays = context.ArcadeReplays
            .Where(x => x.CreatedAt > new DateTime(2021, 2, 1));

        if (request.PlayerId is not null)
        {
            if (request.PlayerIdVs is not null)
            {
                replays = from p in context.ArcadePlayers
                          from rp in p.ArcadeReplayPlayers
                          from rp1 in rp.ArcadeReplay!.ArcadeReplayPlayers
                          where rp.ArcadePlayer!.ProfileId == request.PlayerId.ToonId
                            && rp.ArcadePlayer!.RealmId == request.PlayerId.RealmId
                            && rp.ArcadePlayer!.RegionId == request.PlayerId.RegionId
                            && rp1.ArcadePlayer!.ProfileId == request.PlayerIdVs.ToonId
                            && rp1.ArcadePlayer!.RealmId == request.PlayerIdVs.RealmId
                            && rp1.ArcadePlayer!.RegionId == request.PlayerIdVs.RegionId
                            && rp1.Team != rp.Team
                            && rp.ArcadeReplay!.CreatedAt > new DateTime(2021, 2, 1)
                          select rp.ArcadeReplay;
            }
            else if (request.PlayerIdWith is not null)
            {
                replays = from p in context.ArcadePlayers
                          from rp in p.ArcadeReplayPlayers
                          from rp1 in rp.ArcadeReplay!.ArcadeReplayPlayers
                          where rp.ArcadePlayer!.ProfileId == request.PlayerId.ToonId
                            && rp.ArcadePlayer!.RealmId == request.PlayerId.RealmId
                            && rp.ArcadePlayer!.RegionId == request.PlayerId.RegionId
                            && rp1.ArcadePlayer!.ProfileId == request.PlayerIdWith.ToonId
                            && rp1.ArcadePlayer!.RealmId == request.PlayerIdWith.RealmId
                            && rp1.ArcadePlayer!.RegionId == request.PlayerIdWith.RegionId
                            && rp1.Team != rp.Team
                            && rp.ArcadeReplay!.CreatedAt > new DateTime(2021, 2, 1)
                          select rp.ArcadeReplay;
            }
            else
            {
                replays = from p in context.ArcadePlayers
                          from rp in p.ArcadeReplayPlayers
                          where rp.ArcadePlayer!.ProfileId == request.PlayerId.ToonId
                            && rp.ArcadePlayer!.RealmId == request.PlayerId.RealmId
                            && rp.ArcadePlayer!.RegionId == request.PlayerId.RegionId
                            && rp.ArcadeReplay!.CreatedAt > new DateTime(2021, 2, 1)
                          select rp.ArcadeReplay;
            }
        }

        replays = FilterArcadePlayers(replays, request.Players);

        return replays;
    }

    private IQueryable<ArcadeReplay> FilterArcadePlayers(IQueryable<ArcadeReplay> replays, string players)
    {
        if (string.IsNullOrEmpty(players))
        {
            return replays;
        }

        var names = players.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

        if (names.Count == 0)
        {
            return replays;
        }
        else if (names.Count == 1)
        {
            var name = names[0];
            replays = from rp in context.ArcadeReplayPlayers
                      where rp.ArcadeReplay!.CreatedAt > new DateTime(2021, 2, 1)
                        && rp.ArcadePlayer!.Name == name
                      select rp.ArcadeReplay;
        }
        else
        {
            var name = names[0];
            replays = from rp in context.ArcadeReplayPlayers
                      where rp.ArcadeReplay!.CreatedAt > new DateTime(2021, 2, 1)
                        && rp.ArcadePlayer!.Name == name
                      select rp.ArcadeReplay;

            for (int i = 1; i < names.Count; i++)
            {
                var iname = names[i];
                replays = replays.Where(x => x.ArcadeReplayPlayers.Any(a => a.ArcadePlayer!.Name == iname));
            }
        }
        return replays.Distinct();
    }
}
