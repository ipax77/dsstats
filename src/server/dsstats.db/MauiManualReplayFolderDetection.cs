using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public static class MauiManualReplayFolderDetection
{
    public const int ReplayLimit = 10;
    private const int ScanLimit = 50;

    public static async Task<MauiManualReplayFolderProfileCandidate?> DetectProfileCandidate(
        DsstatsContext context,
        string folder,
        CancellationToken token = default)
    {
        var normalizedFolder = MauiConfigPersistence.NormalizeFolderPath(folder);
        if (string.IsNullOrWhiteSpace(normalizedFolder))
        {
            return null;
        }

        var replayRows = await context.Replays
            .AsNoTracking()
            .Where(replay => replay.FileName != null && replay.FileName.StartsWith(normalizedFolder))
            .OrderByDescending(replay => replay.Gametime)
            .Select(replay => new ReplayRow(replay.ReplayId, replay.FileName!))
            .Take(ScanLimit)
            .ToListAsync(token);

        var replayIds = replayRows
            .Where(replay => IsPathInFolder(replay.FileName, normalizedFolder))
            .Take(ReplayLimit)
            .Select(replay => replay.ReplayId)
            .ToArray();

        if (replayIds.Length == 0)
        {
            return null;
        }

        var players = await context.ReplayPlayers
            .AsNoTracking()
            .Where(player => replayIds.Contains(player.ReplayId) &&
                player.Player != null &&
                player.Player.ToonId.Id > 0)
            .Select(player => new PlayerRow(
                player.Name,
                player.Player!.ToonId.Region,
                player.Player.ToonId.Realm,
                player.Player.ToonId.Id))
            .ToListAsync(token);

        return players
            .GroupBy(player => new ToonIdKey(player.Region, player.Realm, player.Id))
            .Select(group =>
            {
                var name = group
                    .Select(player => player.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .FirstOrDefault() ?? string.Empty;

                return new MauiManualReplayFolderProfileCandidate(
                    name,
                    new()
                    {
                        Region = group.Key.Region,
                        Realm = group.Key.Realm,
                        Id = group.Key.Id,
                    },
                    group.Count());
            })
            .OrderByDescending(candidate => candidate.ReplayCount)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ToonId.Region)
            .ThenBy(candidate => candidate.ToonId.Realm)
            .ThenBy(candidate => candidate.ToonId.Id)
            .FirstOrDefault();
    }

    public static bool IsPathInFolder(string fileName, string normalizedFolder)
    {
        var normalizedFile = Path.GetFullPath(fileName.Trim());
        var folderPrefix = Path.EndsInDirectorySeparator(normalizedFolder)
            ? normalizedFolder
            : normalizedFolder + Path.DirectorySeparatorChar;

        return normalizedFile.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ReplayRow(int ReplayId, string FileName);
    private readonly record struct PlayerRow(string Name, int Region, int Realm, int Id);
    private readonly record struct ToonIdKey(int Region, int Realm, int Id);
}

public sealed record MauiManualReplayFolderProfileCandidate(
    string Name,
    ToonIdDto ToonId,
    int ReplayCount);
