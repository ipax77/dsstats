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

            if (settings.GameMode != GameMode.None && replayDetails.Replay.GameMode != settings.GameMode)
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
                await onUpdated(BuildResult(replays, profileRatings.Values));
            }
        }

        return BuildResult(replays, profileRatings.Values);
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

        settings.GameMode = Enum.IsDefined(settings.GameMode)
            ? settings.GameMode
            : GameMode.None;

        return settings;
    }

    private static bool IsValidProfile(TrackedProfileDto profile)
        => profile.Active && profile.ToonId.Id > 0;

    private static IEnumerable<TrackedProfileDto> GetTrackedProfiles(IEnumerable<TrackedProfileDto> profiles, ReplayDto replay)
        => profiles.Where(profile => replay.Players.Any(player => MatchesToonId(player.Player.ToonId, profile.ToonId)));

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

    private static SessionReplayResult BuildResult(
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

    private static SessionProfileRating? GetProfileRating(
        TrackedProfileDto profile,
        ReplayDetails replayDetails,
        ReplayRatingDto? rating)
    {
        if (rating is null)
        {
            return null;
        }

        var playerRating = rating.ReplayPlayerRatings.FirstOrDefault(player => MatchesToonId(player.ToonId, profile.ToonId));
        if (playerRating is null)
        {
            return null;
        }

        return new SessionProfileRating
        {
            ProfileName = profile.Name,
            ToonId = CloneToonId(profile.ToonId),
            ReplayHash = replayDetails.ReplayHash,
            Gametime = replayDetails.Replay.Gametime,
            GameMode = replayDetails.Replay.GameMode,
            Rating = playerRating.RatingBefore + playerRating.RatingDelta,
            RatingDelta = playerRating.RatingDelta,
        };
    }

    private static string GetToonKey(ToonIdDto toonId)
        => $"{toonId.Region}:{toonId.Realm}:{toonId.Id}";
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
