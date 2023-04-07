using pax.dsstats.shared;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    public ImportResult GetImportResult()
    {
        List<ImportStepInfo> infos = stepQueue.ToList();
        return new()
        {
            UnitsCount = dbCache.Units.Count,
            UpgradesCount = dbCache.Upgrades.Count,
            PlayersCount = dbCache.Players.Count,
            UploadersCount = dbCache.Uploaders.Count,
            ReplaysCount = dbCache.ReplayHashes.Count,
            LastSpawnsCount = dbCache.SpawnHashes.Count,
            BlobCount = blobCaches.Count,
            LatestImports = infos.Sum(s => s.Imported),
            LatestDuplicates = infos.Sum(s => s.Duplicates),
            LatestErrors = infos.Sum(s => s.Errors),
            LatestDuration = infos.Sum(s => s.ElapsedMs)
        };
    }
}
