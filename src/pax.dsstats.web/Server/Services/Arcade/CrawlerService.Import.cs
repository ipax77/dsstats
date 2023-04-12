using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private Dictionary<PlayerId, int> arcadePlayerIds = new();
    private Dictionary<ArcadeReplayId, bool> arcadeReplayIds = new();

    private async Task ImportArcadeReplays(List<LobbyResult> results, bool teMap)
    {
        await SeedPlayerIds();
        await SeedReplayIds();

        List<ArcadeReplay> replays = new();

        foreach (var result in results)
        {
            ArcadeReplayId acradeReplayId = new(result.RegionId, result.BnetBucketId, result.BnetRecordId);
            if (arcadeReplayIds.ContainsKey(acradeReplayId))
            {
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

            var winner = result.Match.ProfileMatches.FirstOrDefault(f => f.Decision == "win");

            if (winner == null)
            {
                continue;
            }

            var winnerPlayer = result.Slots.FirstOrDefault(f => f.Name == winner.Profile.Name);

            if (winnerPlayer == null || winnerPlayer.Team == null)
            { 
                continue;
            }

            ArcadeReplay replay = new()
            {
                RegionId = result.RegionId,
                BnetRecordId = result.BnetRecordId,
                BnetBucketId = result.BnetBucketId,
                GameMode = gameMode,
                CreatedAt = result.CreatedAt,
                Duration = result.Match.CompletedAt == null ? 0 
                : Convert.ToInt32((result.Match.CompletedAt.Value - result.CreatedAt).TotalSeconds),
                PlayerCount = 6,
                WinnerTeam = winnerPlayer.Team.Value,
                TournamentEdition = teMap,
                ArcadeReplayPlayers = result.Slots.Select(s => new ArcadeReplayPlayer()
                {
                    Name = s.Name,
                    SlotNumber = s.SlotNumber ?? 0,
                    Team = s.Team ?? 0,
                    ArcadePlayer = new()
                    {
                        Name = s.Name,
                    }
                }).ToList(),
            };

            foreach (var rp in replay.ArcadeReplayPlayers)
            {
                var matchPlayer = result.Match.ProfileMatches.FirstOrDefault(f => f.Profile.Name ==  rp.Name);
                if (matchPlayer == null)
                {
                    continue; 
                }
                rp.Discriminator = matchPlayer.Profile.Discriminator;
                rp.ArcadePlayer.RegionId = matchPlayer.Profile.RegionId;
                rp.ArcadePlayer.RealmId = matchPlayer.Profile.RealmId;
                rp.ArcadePlayer.ProfileId = matchPlayer.Profile.ProfileId;
                rp.PlayerResult = matchPlayer.Decision == "win" ? pax.dsstats.shared.PlayerResult.Win
                    : pax.dsstats.shared.PlayerResult.Los;
            }

            replays.Add(replay);
            arcadeReplayIds.Add(new(replay.RegionId, replay.BnetBucketId, replay.BnetRecordId), true);
        }

        await MapPlayers(replays);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.ArcadeReplays.AddRange(replays);
        await context.SaveChangesAsync();
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