using dsstats.indexedDb.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.pwa.Services;

public class SessionProgressService(
    IndexedDbService dbService,
    IReplayRepository replayRepository,
    RatingService ratingService,
    PwaConfigService pwaConfigService)
{
    private const int ProfileDetectionReplayLimit = 10;

    public async Task<List<TrackedProfileDto>> GetTrackedProfilesAsync()
        => await dbService.GetTrackedProfiles();

    public async Task SaveTrackedProfilesAsync(List<TrackedProfileDto> profiles)
    {
        foreach (var profile in profiles)
        {
            profile.Name = profile.Name.Trim();
        }

        await dbService.SaveTrackedProfiles(profiles);
    }

    public async Task<List<ProfileCandidateDto>> GetProfileCandidatesAsync(int replayLimit = ProfileDetectionReplayLimit)
        => await dbService.DetectTrackedProfileCandidates(replayLimit);

    public async Task EnsureDefaultTrackedProfileAsync()
    {
        var profiles = await GetTrackedProfilesAsync();
        if (profiles.Count > 0)
        {
            return;
        }

        var candidates = await GetProfileCandidatesAsync();
        var candidate = candidates.FirstOrDefault();
        if (candidate is null)
        {
            return;
        }

        await SaveTrackedProfilesAsync([
            new TrackedProfileDto
            {
                Active = true,
                AutoDetected = true,
                Name = candidate.Name,
                ToonId = CloneToonId(candidate.ToonId),
            }
        ]);
    }

    public async Task<SessionWindowSettingsDto> GetSessionWindowSettingsAsync()
        => NormalizeSettings(await dbService.GetSessionWindowSettings());

    public async Task SaveSessionWindowSettingsAsync(SessionWindowSettingsDto settings)
        => await dbService.SaveSessionWindowSettings(NormalizeSettings(settings));

    public async Task<SessionReplayResult> GetSessionReplaysAsync(
        Func<SessionReplayResult, Task>? onUpdated = null,
        CancellationToken token = default)
    {
        var config = await pwaConfigService.GetConfig();
        if (!config.UploadCredential)
        {
            return SessionReplayResult.Empty;
        }

        await EnsureDefaultTrackedProfileAsync();

        var profiles = (await GetTrackedProfilesAsync())
            .Where(IsValidProfile)
            .ToList();

        if (profiles.Count == 0)
        {
            return SessionReplayResult.Empty;
        }

        var settings = await GetSessionWindowSettingsAsync();
        var hashes = settings.Mode == SessionWindowModeDto.Time
            ? await dbService.GetReplayHashesSince(DateTime.UtcNow.AddHours(-settings.Hours))
            : await dbService.GetRecentReplayHashes(settings.ReplayCount);

        List<SessionReplay> replays = [];

        foreach (var replayHash in hashes)
        {
            token.ThrowIfCancellationRequested();

            var replayDetails = await replayRepository.GetReplayDetails(replayHash);
            if (replayDetails is null)
            {
                continue;
            }

            var trackedProfile = GetTrackedProfile(profiles, replayDetails.Replay);
            if (trackedProfile is null)
            {
                continue;
            }

            ReplayRatingDto? rating = replayDetails.ReplayRatings.FirstOrDefault();
            if (rating is null && CanHaveRating(replayDetails.Replay))
            {
                rating = await ratingService.FetchAndSaveRating(replayHash);
                if (rating is not null)
                {
                    replayDetails.ReplayRatings = [rating];
                }
            }

            replays.Add(new SessionReplay
            {
                ReplayHash = replayHash,
                Gametime = replayDetails.Replay.Gametime,
                GameMode = replayDetails.Replay.GameMode,
                Duration = replayDetails.Replay.Duration,
                RatingGain = GetRatingGain(trackedProfile.ToonId, rating),
                IsWin = IsWin(replayDetails.Replay, trackedProfile.ToonId),
            });

            if (onUpdated is not null)
            {
                await onUpdated(BuildResult(replays));
            }
        }

        return BuildResult(replays);
    }

    public static bool CanHaveRating(ReplayDto replay)
        => replay.Players.Count > 1 && replay.Duration >= 300;

    public static SessionWindowSettingsDto NormalizeSettings(SessionWindowSettingsDto? settings)
    {
        settings ??= new SessionWindowSettingsDto();

        settings.Mode = settings.Mode is SessionWindowModeDto.Time or SessionWindowModeDto.Count
            ? settings.Mode
            : SessionWindowModeDto.Time;

        settings.Hours = settings.Hours switch
        {
            3 or 6 or 12 or 24 => settings.Hours,
            _ => 6,
        };

        settings.ReplayCount = settings.ReplayCount switch
        {
            10 or 20 or 30 or 50 => settings.ReplayCount,
            _ => 10,
        };

        return settings;
    }

    private static bool IsValidProfile(TrackedProfileDto profile)
        => profile.Active && profile.ToonId.Id > 0;

    private static TrackedProfileDto? GetTrackedProfile(IEnumerable<TrackedProfileDto> profiles, ReplayDto replay)
    {
        foreach (var profile in profiles)
        {
            if (replay.Players.Any(player => MatchesToonId(player.Player.ToonId, profile.ToonId)))
            {
                return profile;
            }
        }

        return null;
    }

    private static double GetRatingGain(ToonIdDto toonId, ReplayRatingDto? rating)
    {
        if (rating is null)
        {
            return 0;
        }

        var playerRating = rating.ReplayPlayerRatings.FirstOrDefault(player => MatchesToonId(player.ToonId, toonId));
        return playerRating?.RatingDelta ?? 0;
    }

    private static bool IsWin(ReplayDto replay, ToonIdDto toonId)
    {
        var player = replay.Players.FirstOrDefault(p => MatchesToonId(p.Player.ToonId, toonId));
        return player is not null && player.TeamId == replay.WinnerTeam;
    }

    private static SessionReplayResult BuildResult(List<SessionReplay> replays)
    {
        if (replays.Count == 0)
        {
            return SessionReplayResult.Empty;
        }

        var ordered = replays
            .OrderByDescending(replay => replay.Gametime)
            .ToList();

        var totalDuration = ordered.Sum(replay => replay.Duration);
        var totalGain = ordered.Sum(replay => replay.RatingGain);
        var wins = ordered.Count(replay => replay.IsWin);

        return new SessionReplayResult(
            ordered,
            totalDuration,
            totalGain,
            wins / (double)ordered.Count);
    }

    private static bool MatchesToonId(ToonIdDto left, ToonIdDto right)
        => left.Id == right.Id
           && left.Region == right.Region
           && left.Realm == right.Realm;

    private static ToonIdDto CloneToonId(ToonIdDto toonId)
        => new()
        {
            Id = toonId.Id,
            Region = toonId.Region,
            Realm = toonId.Realm,
        };
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

public sealed record SessionReplayResult(
    List<SessionReplay> Replays,
    long TotalDuration,
    double TotalGain,
    double Winrate)
{
    public static SessionReplayResult Empty { get; } = new([], 0, 0, 0);
}
