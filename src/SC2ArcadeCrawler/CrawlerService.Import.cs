﻿using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private Dictionary<ArcadeReplayId, bool> arcadeReplayIds = [];

    private async Task ImportArcadeReplays(CrawlInfo crawlInfo, CancellationToken token)
    {
        await SeedReplayIds();

        List<ArcadeReplay> replays = new();

        foreach (var result in crawlInfo.Results)
        {
            ArcadeReplayId acradeReplayId = new(result.RegionId, result.BnetBucketId, result.BnetRecordId);
            if (arcadeReplayIds.ContainsKey(acradeReplayId))
            {
                crawlInfo.Dups++;
                continue;
            }

            if (result.SlotsHumansTaken != 6
                || result.Match == null
                || result.Match.ProfileMatches.Count == 0)
            {
                continue;
            }

            GameMode gameMode = result.MapVariantMode switch
            {
                "3V3" => GameMode.Standard,
                "3V3 Commanders" => GameMode.Commanders,
                "Heroic Commanders" => GameMode.CommandersHeroic,
                "Standard" => GameMode.Standard,
                "Commanders" => GameMode.Commanders,
                _ => GameMode.None
            };

            if (gameMode == GameMode.None)
            {
                continue;
            }

            var winners = result.Match.ProfileMatches.Where(f => f.Decision == "win").ToList();
            var losers = result.Match.ProfileMatches.Except(winners).ToList();

            if (!winners.Any())
            {
                continue;
            }

            var winnerPlayers = new List<Slot>();

            int team1Winners = 0;
            int team2Winners = 0;

            foreach (var winner in winners)
            {
                var slot = result.Slots.FirstOrDefault(f => f.Profile?.ProfileId == winner.Profile.ProfileId
                    && f.Profile.RealmId == winner.Profile.RealmId
                    && f.Profile.RegionId == winner.Profile.RegionId);
                if (slot != null)
                {
                    winnerPlayers.Add(slot);
                    if (slot.Team == 1)
                    {
                        team1Winners++;
                    }
                    if (slot.Team == 2)
                    {
                        team2Winners++;
                    }
                }
            }

            int winnerTeam = 0;

            if (team1Winners > team2Winners)
            {
                winnerTeam = 1;
            }
            else if (team2Winners > team1Winners)
            {
                winnerTeam = 2;
            }
            else
            {
                crawlInfo.Errors++;
                logger.LogInformation($"could not determine winnerteam: BnetBucketId {result.BnetBucketId}, BnetRecordId {result.BnetRecordId}");
                // continue;
            }

            if (result.Match.ProfileMatches.Any(a => a.Profile.ProfileId == 0))
            {
                logger.LogInformation($"replay with MatchProfiles ProfileId == 0: RegionId {crawlInfo.RegionId}, BnetBucketId {result.BnetBucketId}, BnetRecordId {result.BnetRecordId}");
                continue;
            }

            if (result.Slots.Any(a => a.Profile?.ProfileId == 0))
            {
                logger.LogInformation($"replay with SlotsProfiles ProfileId == 0: RegionId {crawlInfo.RegionId}, BnetBucketId {result.BnetBucketId}, BnetRecordId {result.BnetRecordId}");
                continue;
            }

            ArcadeReplay replay = new()
            {
                RegionId = result.RegionId,
                BnetRecordId = result.BnetRecordId,
                BnetBucketId = result.BnetBucketId,
                GameMode = gameMode,
                CreatedAt = result.CreatedAt,
                Duration = result.Match.CompletedAt == null || result.ClosedAt == null ? 0
                : Convert.ToInt32((result.Match.CompletedAt.Value - result.ClosedAt.Value).TotalSeconds),
                PlayerCount = 6,
                WinnerTeam = winnerTeam,
                TournamentEdition = crawlInfo.TeMap,
                ArcadeReplayDsPlayers = result.Slots.Select(s => new ArcadeReplayDsPlayer()
                {
                    Name = s.Name,
                    SlotNumber = s.SlotNumber ?? 0,
                    Team = s.Team ?? 0,
                    Player = new()
                    {
                        Name = s.Name,
                        RegionId = s.Profile?.RegionId ?? 0,
                        RealmId = s.Profile?.RealmId ?? 0,
                        ToonId = s.Profile?.ProfileId ?? 0
                    }
                }).ToList()
            };

            foreach (var rp in replay.ArcadeReplayDsPlayers)
            {
                if (rp.Player is null)
                {
                    continue;
                }
                var matchPlayer = result.Match.ProfileMatches.FirstOrDefault(f =>
                    f.Profile.RegionId == rp.Player.RegionId
                    && f.Profile.ProfileId == rp.Player.ToonId
                    && f.Profile.RealmId == rp.Player.RealmId
                );
                if (matchPlayer == null)
                {
                    logger.LogInformation("player match profile not found: RegionId {regionId}, BnetBucketId {bnetBucketId}, BnetRecordId {bnetRecordId}",
                            replay.RegionId, replay.BnetBucketId, replay.BnetRecordId);
                    continue;
                }
                rp.Discriminator = matchPlayer.Profile.Discriminator;
                rp.PlayerResult = GetPlayerResult(matchPlayer.Decision);
                if (winnerTeam > 0 && rp.PlayerResult == PlayerResult.Win && rp.Team != winnerTeam)
                {
                    logger.LogInformation("correction player result due to loser team BnetBucketId {bnetBucketId}, BnetRecordId {bnetRecordId}",
                        replay.BnetBucketId, replay.BnetRecordId);
                    rp.PlayerResult = PlayerResult.Los;
                }
                else if (winnerTeam > 0 && rp.PlayerResult == PlayerResult.Los && rp.Team == winnerTeam)
                {
                    logger.LogInformation("correction player result due to winner team BnetBucketId {bnetBucketId}, BnetRecordId {bnetRecordId}",
                        replay.BnetBucketId, replay.BnetRecordId);
                    rp.PlayerResult = PlayerResult.Win;
                }
            }

            replay.Imported = DateTime.UtcNow;

            replays.Add(replay);
            arcadeReplayIds.Add(new(replay.RegionId, replay.BnetBucketId, replay.BnetRecordId), true);
            crawlInfo.Imports++;
        }

        await MapPlayers(replays, token);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.ArcadeReplays.AddRange(replays);
        await context.SaveChangesAsync(token);
    }

    private static PlayerResult GetPlayerResult(string decision)
    {
        return decision switch
        {
            "win" => PlayerResult.Win,
            "loss" => PlayerResult.Los,
            _ => PlayerResult.None
        };
    }

    private async Task SeedReplayIds()
    {
        if (arcadeReplayIds.Count != 0)
        {
            return;
        }
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        arcadeReplayIds = (await context.ArcadeReplays
            .Select(s => new { s.RegionId, s.BnetBucketId, s.BnetRecordId }).Distinct().ToListAsync())
        .ToDictionary(k => new ArcadeReplayId(k.RegionId, k.BnetBucketId, k.BnetRecordId), v => true);
    }
}

public record ArcadeReplayId
{
    public ArcadeReplayId(int regionId, long bnetBucketId, long bnetRecordId)
    {
        RegionId = regionId;
        BnetBucketId = bnetBucketId;
        BnetRecordId = bnetRecordId;
    }
    public int RegionId { get; init; }
    public long BnetBucketId { get; init; }
    public long BnetRecordId { get; init; }
}