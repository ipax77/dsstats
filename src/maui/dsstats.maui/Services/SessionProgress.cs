using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Maui;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui.Services;

public class SessionProgress(
    IServiceScopeFactory scopeFactory,
    IReplayRepository replayRepository,
    [FromKeyedServices("api")] IReplayRepository apiReplayRepository,
    DsstatsService dsstatsService)
{
    private readonly SemaphoreSlim _ratingFetchLock = new(1, 1);
    private DateTimeOffset _nextAllowedRatingRequest = DateTimeOffset.MinValue;

    public async Task<SessionReplayResult> GetSessionReplaysAsync(
        Func<SessionReplayResult, Task>? onUpdated = null,
        CancellationToken token = default)
    {
        var config = await dsstatsService.GetConfig();
        if (!config.UploadCredential)
        {
            return SessionReplayResult.Empty;
        }

        await EnsureManualReplayFolderIdentities(config, token);

        var profiles = GetSessionProfiles(config);

        if (profiles.Count == 0)
        {
            return SessionReplayResult.Empty;
        }

        var hashes = await GetSessionReplayHashes(config, token);
        List<SessionReplay> replays = [];
        Dictionary<string, SessionProfileRating> profileRatings = [];

        foreach (var replayHash in hashes)
        {
            token.ThrowIfCancellationRequested();

            var replayDetails = await replayRepository.GetReplayDetails(replayHash);
            if (replayDetails is null)
            {
                continue;
            }

            var matchingProfiles = GetTrackedProfiles(profiles, replayDetails.Replay).ToList();
            if (matchingProfiles.Count == 0)
            {
                continue;
            }

            if (config.SessionWindowGameMode != GameMode.None &&
                replayDetails.Replay.GameMode != config.SessionWindowGameMode)
            {
                continue;
            }

            var rating = replayDetails.ReplayRatings.FirstOrDefault(r => r.RatingType == RatingType.All);
            if (rating is null && CanHaveRating(replayDetails.Replay))
            {
                rating = await FetchAndSaveRating(replayHash, forceRefresh: false, token);
                if (rating is not null)
                {
                    replayDetails.ReplayRatings = [rating];
                }
            }

            foreach (var profile in matchingProfiles)
            {
                var key = GetToonKey(profile.ToonId);
                if (profileRatings.ContainsKey(key))
                {
                    continue;
                }

                var profileRating = GetProfileRating(profile, replayDetails, rating);
                if (profileRating is not null)
                {
                    profileRatings.Add(key, profileRating);
                }
            }

            var trackedProfile = matchingProfiles[0];
            var trackedToonId = CloneToonId(trackedProfile.ToonId);
            replays.Add(new SessionReplay
            {
                ReplayHash = replayHash,
                Gametime = replayDetails.Replay.Gametime,
                GameMode = replayDetails.Replay.GameMode,
                Duration = replayDetails.Replay.Duration,
                RatingGain = GetRatingGain(trackedToonId, rating),
                IsWin = IsWin(replayDetails.Replay, trackedToonId),
            });

            if (onUpdated is not null)
            {
                await onUpdated(BuildResult(replays, profileRatings.Values));
            }
        }

        return BuildResult(replays, profileRatings.Values);
    }

    public async Task<ReplayRatingDto?> UpdateRating(
        string replayHash,
        bool forceRefresh = true,
        CancellationToken token = default)
        => await FetchAndSaveRating(replayHash, forceRefresh, token);

    public static bool CanHaveRating(ReplayDto replay)
        => replay.Players.Count > 1 && replay.Duration >= 300;

    public static MauiConfigDto NormalizeSettings(MauiConfigDto config)
        => MauiSessionProgressCalculator.NormalizeSettings(config);

    public static SessionReplayResult BuildResult(
        List<SessionReplay> replays,
        IEnumerable<SessionProfileRating> profileRatings)
    {
        var ordered = replays
            .OrderByDescending(replay => replay.Gametime)
            .ToList();

        var orderedProfileRatings = profileRatings
            .OrderBy(profile => profile.ProfileName)
            .ThenBy(profile => profile.ToonId.Region)
            .ToList();

        if (ordered.Count == 0)
        {
            return new SessionReplayResult(ordered, 0, 0, 0, orderedProfileRatings);
        }

        var totalDuration = ordered.Sum(replay => replay.Duration);
        var totalGain = ordered.Sum(replay => replay.RatingGain);
        var wins = ordered.Count(replay => replay.IsWin);

        return new SessionReplayResult(
            ordered,
            totalDuration,
            totalGain,
            wins / (double)ordered.Count,
            orderedProfileRatings);
    }

    public static bool MatchesToonId(ToonIdDto left, ToonIdDto right)
        => MauiSessionProgressCalculator.MatchesToonId(left, right);

    public static double GetRatingGain(ToonIdDto toonId, ReplayRatingDto? rating)
        => MauiSessionProgressCalculator.GetRatingGain(toonId, rating);

    public static bool IsWin(ReplayDto replay, ToonIdDto toonId)
        => MauiSessionProgressCalculator.IsWin(replay, toonId);

    private async Task EnsureManualReplayFolderIdentities(MauiConfig config, CancellationToken token)
    {
        var folders = config.ManualReplayFolders
            .Where(folder => folder.Active &&
                !string.IsNullOrWhiteSpace(folder.Folder) &&
                !HasDetectedToonId(folder))
            .Select(folder => new ManualFolderDetectionTarget(
                folder.MauiReplayFolderId,
                MauiConfigPersistence.NormalizeFolderPath(folder.Folder)))
            .Where(folder => folder.MauiReplayFolderId > 0 && !string.IsNullOrWhiteSpace(folder.Folder))
            .DistinctBy(folder => folder.MauiReplayFolderId)
            .ToList();

        if (folders.Count == 0)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var folderIds = folders
            .Select(target => target.MauiReplayFolderId)
            .ToArray();
        var dbFolders = await context.ManualReplayFolders
            .Where(folder => folderIds.Contains(folder.MauiReplayFolderId))
            .ToDictionaryAsync(folder => folder.MauiReplayFolderId, token);

        var changed = false;
        foreach (var folder in folders)
        {
            token.ThrowIfCancellationRequested();

            if (!dbFolders.TryGetValue(folder.MauiReplayFolderId, out var dbFolder) ||
                HasDetectedToonId(dbFolder))
            {
                continue;
            }

            var candidate = await MauiManualReplayFolderDetection.DetectProfileCandidate(context, folder.Folder, token);
            if (candidate is null)
            {
                continue;
            }

            changed |= MauiConfigPersistence.SetDetectedProfile(
                dbFolder,
                candidate.Name,
                candidate.ToonId,
                DateTime.UtcNow,
                candidate.ReplayCount);

            var configFolder = config.ManualReplayFolders
                .FirstOrDefault(existing => existing.MauiReplayFolderId == folder.MauiReplayFolderId);
            if (configFolder is not null)
            {
                configFolder.DetectedName = dbFolder.DetectedName;
                configFolder.DetectedToonIdRegion = dbFolder.DetectedToonIdRegion;
                configFolder.DetectedToonIdRealm = dbFolder.DetectedToonIdRealm;
                configFolder.DetectedToonIdId = dbFolder.DetectedToonIdId;
                configFolder.DetectedAtUtc = dbFolder.DetectedAtUtc;
                configFolder.DetectedReplayCount = dbFolder.DetectedReplayCount;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync(token);
        }
    }

    private async Task<List<string>> GetSessionReplayHashes(MauiConfig config, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        if (config.SessionWindowMode == MauiSessionWindowMode.Time)
        {
            var fromUtc = DateTime.UtcNow.AddHours(-NormalizeHours(config.SessionWindowHours));
            return await context.Replays
                .AsNoTracking()
                .Where(replay => replay.Gametime >= fromUtc)
                .OrderByDescending(replay => replay.Gametime)
                .Select(replay => replay.ReplayHash)
                .ToListAsync(token);
        }

        return await context.Replays
            .AsNoTracking()
            .OrderByDescending(replay => replay.Gametime)
            .Take(NormalizeReplayCount(config.SessionWindowReplayCount))
            .Select(replay => replay.ReplayHash)
            .ToListAsync(token);
    }

    private async Task<ReplayRatingDto?> FetchAndSaveRating(
        string replayHash,
        bool forceRefresh,
        CancellationToken token)
    {
        if (!forceRefresh)
        {
            var cached = await replayRepository.GetReplayRating(replayHash);
            if (cached is not null)
            {
                return cached;
            }
        }

        await _ratingFetchLock.WaitAsync(token);
        try
        {
            if (!forceRefresh)
            {
                var cached = await replayRepository.GetReplayRating(replayHash);
                if (cached is not null)
                {
                    return cached;
                }
            }

            var now = DateTimeOffset.UtcNow;
            var delay = _nextAllowedRatingRequest - now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, token);
            }

            var rating = await apiReplayRepository.GetReplayRating(replayHash);
            _nextAllowedRatingRequest = DateTimeOffset.UtcNow.AddSeconds(3);
            if (rating is null)
            {
                return null;
            }

            await replayRepository.SaveReplayRatingAll(replayHash, rating);
            return rating;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _nextAllowedRatingRequest = DateTimeOffset.UtcNow.AddSeconds(3);
            return null;
        }
        finally
        {
            _ratingFetchLock.Release();
        }
    }

    private static List<TrackedSessionProfile> GetSessionProfiles(MauiConfig config)
    {
        var profiles = config.Sc2Profiles
            .Where(IsValidProfile)
            .Select(profile => new TrackedSessionProfile(
                profile.Name,
                CloneToonId(profile.ToonId)))
            .ToList();

        profiles.AddRange(config.ManualReplayFolders
            .Where(folder => folder.Active && HasDetectedToonId(folder))
            .Select(folder => new TrackedSessionProfile(
                string.IsNullOrWhiteSpace(folder.DetectedName)
                    ? Path.GetFileName(folder.Folder)
                    : folder.DetectedName!,
                new()
                {
                    Region = folder.DetectedToonIdRegion.GetValueOrDefault(),
                    Realm = folder.DetectedToonIdRealm.GetValueOrDefault(),
                    Id = folder.DetectedToonIdId.GetValueOrDefault(),
                })));

        return profiles
            .GroupBy(profile => new ToonIdKey(profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id))
            .Select(group => group.First())
            .ToList();
    }

    private static bool IsValidProfile(Sc2Profile profile)
        => profile.Active && profile.ToonId.Id > 0;

    private static bool HasDetectedToonId(MauiReplayFolder folder)
        => folder.DetectedToonIdId > 0;

    private static IEnumerable<TrackedSessionProfile> GetTrackedProfiles(IEnumerable<TrackedSessionProfile> profiles, ReplayDto replay)
        => profiles.Where(profile => replay.Players.Any(player => MatchesToonId(player.Player.ToonId, profile.ToonId)));

    private static SessionProfileRating? GetProfileRating(
        TrackedSessionProfile profile,
        ReplayDetails replayDetails,
        ReplayRatingDto? rating)
    {
        if (rating is null)
        {
            return null;
        }

        var toonId = CloneToonId(profile.ToonId);
        var playerRating = rating.ReplayPlayerRatings.FirstOrDefault(player => MatchesToonId(player.ToonId, toonId));
        if (playerRating is null)
        {
            return null;
        }

        return new SessionProfileRating
        {
            ProfileName = profile.Name,
            ToonId = toonId,
            ReplayHash = replayDetails.ReplayHash,
            Gametime = replayDetails.Replay.Gametime,
            GameMode = replayDetails.Replay.GameMode,
            Rating = playerRating.RatingBefore + playerRating.RatingDelta,
            RatingDelta = playerRating.RatingDelta,
        };
    }

    private static string GetToonKey(ToonIdDto toonId)
        => $"{toonId.Region}:{toonId.Realm}:{toonId.Id}";

    private static ToonIdDto CloneToonId(ToonId toonId)
        => new()
        {
            Id = toonId.Id,
            Region = toonId.Region,
            Realm = toonId.Realm,
        };

    private static ToonIdDto CloneToonId(ToonIdDto toonId)
        => new()
        {
            Id = toonId.Id,
            Region = toonId.Region,
            Realm = toonId.Realm,
        };

    private static int NormalizeHours(int hours)
        => hours is 3 or 6 or 12 or 24 ? hours : 6;

    private static int NormalizeReplayCount(int replayCount)
        => replayCount is 10 or 20 or 30 or 50 ? replayCount : 10;

    private readonly record struct TrackedSessionProfile(string Name, ToonIdDto ToonId);
    private readonly record struct ManualFolderDetectionTarget(int MauiReplayFolderId, string Folder);
    private readonly record struct ToonIdKey(int Region, int Realm, int Id);
}

public sealed class SessionReplay
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime Gametime { get; set; }
    public GameMode GameMode { get; set; }
    public int Duration { get; set; }
    public double RatingGain { get; set; }
    public bool IsWin { get; set; }
}

public sealed class SessionProfileRating
{
    public string ProfileName { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime Gametime { get; set; }
    public GameMode GameMode { get; set; }
    public double Rating { get; set; }
    public double RatingDelta { get; set; }
}

public sealed record SessionReplayResult(
    List<SessionReplay> Replays,
    long TotalDuration,
    double TotalGain,
    double Winrate,
    List<SessionProfileRating> ProfileRatings)
{
    public static SessionReplayResult Empty { get; } = new([], 0, 0, 0, []);
}
