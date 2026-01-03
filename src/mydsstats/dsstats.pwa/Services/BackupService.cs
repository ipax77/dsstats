using dsstats.indexedDb.Services;

namespace dsstats.pwa.Services;

public class BackupService(IndexedDbService dbService)
{
    public Task BackupToFileAsync()
        => dbService.TriggerBackup();

    public Task RestoreFromFileAsync()
        => dbService.RestoreBackup();
}
