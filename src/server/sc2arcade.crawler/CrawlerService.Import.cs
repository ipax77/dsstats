using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Arcade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace sc2arcade.crawler;

public partial class CrawlerService
{
    private async Task ImportArcadeReplays(CrawlInfo crawlInfo, CancellationToken token)
    {
        List<ArcadeReplayDto> replays = new();

        foreach (var result in crawlInfo.Results)
        {
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

            ArcadeReplayDto replay = new()
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
                Players = result.Slots.Select(s => new ArcadeReplayPlayerDto()
                {
                    SlotNumber = s.SlotNumber ?? 0,
                    Team = s.Team ?? 0,
                    Player = new()
                    {
                        Name = s.Name,
                        ToonId = new()
                        {
                            Realm = s.Profile?.RealmId ?? 0,
                            Region = s.Profile?.RegionId ?? 0,
                            Id = s.Profile?.ProfileId ?? 0,
                        },
                    }
                }).ToList()
            };

            replays.Add(replay);
            crawlInfo.Imports++;
        }
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        await importService.ImportArcadeReplays(replays);
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