using dsstats.dbServices;
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
            var conversion = ArcadeReplayConverter.Convert(result);
            if (conversion.UnknownWinner)
            {
                crawlInfo.Errors++;
                logger.LogInformation("could not determine winnerteam: BnetBucketId {Bucket}, BnetRecordId {Record}",
                    result.BnetBucketId, result.BnetRecordId);
            }
            if (conversion.Replay is null)
            {
                if (conversion.RejectionReason is "MatchProfileIdZero" or "SlotProfileIdZero")
                    logger.LogInformation("replay rejected: {Reason}, RegionId {Region}, BnetBucketId {Bucket}, BnetRecordId {Record}",
                        conversion.RejectionReason, crawlInfo.RegionId, result.BnetBucketId, result.BnetRecordId);
                continue;
            }
            replays.Add(conversion.Replay);
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
