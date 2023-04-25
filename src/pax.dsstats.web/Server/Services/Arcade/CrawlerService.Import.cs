using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private Dictionary<PlayerId, int> arcadePlayerIds = new();
    private Dictionary<ArcadeReplayId, bool> arcadeReplayIds = new();

    public async Task TestImportArcadeReplays(CrawlInfo crawlInfo)
    {
        await ImportArcadeReplays(crawlInfo);
    }

    private async Task ImportArcadeReplays(CrawlInfo crawlInfo)
    {
        await SeedPlayerIds();
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
                ArcadeReplayPlayers = result.Slots.Select(s => new ArcadeReplayPlayer()
                {
                    Name = s.Name,
                    SlotNumber = s.SlotNumber ?? 0,
                    Team = s.Team ?? 0,
                    ArcadePlayer = new()
                    {
                        Name = s.Name,
                        RegionId = s.Profile?.RegionId ?? 0,
                        RealmId = s.Profile?.RealmId ?? 0,
                        ProfileId = s.Profile?.ProfileId ?? 0
                    }
                }).ToList(),
            };

            foreach (var rp in replay.ArcadeReplayPlayers)
            {
                var matchPlayer = result.Match.ProfileMatches.FirstOrDefault(f => 
                    f.Profile.RegionId == rp.ArcadePlayer.RegionId
                    && f.Profile.ProfileId == rp.ArcadePlayer.ProfileId
                    && f.Profile.RealmId == rp.ArcadePlayer.RealmId
                );
                if (matchPlayer == null)
                {
                    logger.LogInformation($"player match profile not found: RegionId {replay.RegionId}, BnetBucketId {replay.BnetBucketId}, BnetRecordId {replay.BnetRecordId}");
                    continue; 
                }
                rp.Discriminator = matchPlayer.Profile.Discriminator;
                rp.PlayerResult = GetPlayerResult(matchPlayer.Decision);
                if (winnerTeam > 0 && rp.PlayerResult == PlayerResult.Win && rp.Team != winnerTeam)
                {
                    logger.LogInformation($"correction player result due to loser team BnetBucketId {replay.BnetBucketId}, BnetRecordId {replay.BnetRecordId}");
                    rp.PlayerResult = PlayerResult.Los;
                }
                else if (winnerTeam > 0 && rp.PlayerResult == PlayerResult.Los && rp.Team == winnerTeam)
                {
                    logger.LogInformation($"correction player result due to winner team BnetBucketId {replay.BnetBucketId}, BnetRecordId {replay.BnetRecordId}");
                    rp.PlayerResult = PlayerResult.Win;
                }
            }

            replay.Imported = DateTime.UtcNow;
            replay.GenHash(md5);

            replays.Add(replay);
            arcadeReplayIds.Add(new(replay.RegionId, replay.BnetBucketId, replay.BnetRecordId), true);
            crawlInfo.Imports++;
        }

        await MapPlayers(replays);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.ArcadeReplays.AddRange(replays);
        await context.SaveChangesAsync();
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
        if (arcadeReplayIds.Any())
        {
            return;
        }
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        arcadeReplayIds = (await context.ArcadeReplays
            .Select(s => new { s.RegionId, s.BnetBucketId, s.BnetRecordId }).Distinct().ToListAsync())
        .ToDictionary(k => new ArcadeReplayId(k.RegionId, k.BnetBucketId, k.BnetRecordId), v => true);
    }

    private async Task SeedPlayerIds()
    {
        if (arcadePlayerIds.Any())
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var players = await context.ArcadePlayers
            .Select(s => new
            {
                s.ArcadePlayerId,
                s.RegionId,
                s.RealmId,
                s.ProfileId
            }).ToListAsync();
        
        arcadePlayerIds = players.ToDictionary(k => new PlayerId(k.RegionId, k.RealmId, k.ProfileId), v => v.ArcadePlayerId);
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