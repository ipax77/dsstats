using System.Security.Cryptography;
using System.Text;

namespace dsstats.shared;

public class ReplayDto
{
    public string FileName { get; set; } = string.Empty;
    public string CompatHash { get; set; } = string.Empty; // For compatibility with old replays
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public DateTime Gametime { get; set; }
    public int BaseBuild { get; set; }
    public int Duration { get; set; }
    public int Cannon { get; set; }
    public int Bunker { get; set; }
    public int WinnerTeam { get; set; }
    public List<int> MiddleChanges { get; set; } = [];
    public List<ReplayPlayerDto> Players { get; set; } = [];
}

public class ReplayPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Clan { get; set; }
    public Commander Race { get; set; }
    public Commander SelectedRace { get; set; }
    public int TeamId { get; set; }
    public int GamePos { get; set; }
    public PlayerResult Result { get; set; }
    public int Duration { get; set; }
    public int Apm { get; set; }
    public int Messages { get; set; }
    public int Pings { get; set; }
    public bool IsMvp { get; set; }
    public bool IsUploader { get; set; }
    public List<SpawnDto> Spawns { get; set; } = [];
    public List<UpgradeDto> Upgrades { get; set; } = [];
    public List<int> TierUpgrades { get; set; } = [];
    public List<int> Refineries { get; set; } = [];
    public PlayerDto Player { get; set; } = new();
}

public class PlayerDto
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
}

public record ToonIdDto
{
    public int Region { get; set; }
    public int Realm { get; set; }
    public int Id { get; set; }
}

public class SpawnDto
{
    public Breakpoint Breakpoint { get; set; }
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradeSpent { get; set; }
    public List<UnitDto> Units { get; set; } = [];
}

public class UnitDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<int> Positions { get; set; } = [];
}

public class UpgradeDto
{
    public string Name { get; set; } = string.Empty;
    public int Gameloop { get; set; }
}

public class ReplayListDto
{
    public string ReplayHash { get; init; } = string.Empty;
    public DateTime Gametime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public List<Commander> CommandersTeam1 { get; init; } = [];
    public List<Commander> CommandersTeam2 { get; init; } = [];
    public double? Exp2Win { get; init; }
    public int? AvgRating { get; init; }
    public LeaverType LeaverType { get; init; }
    public int PlayerPos { get; set; }
}

public static class ReplayDtoExtensions
{
    public static (int, int) GetMiddleIncome(this ReplayDto replay, int targetGameloop)
    {
        int team1 = 0;
        int team2 = 0;

        // No middle crossings → nobody ever gets middle
        if (replay.MiddleChanges == null || replay.MiddleChanges.Count < 2)
            return (team1, team2);

        var changes = replay.MiddleChanges;

        int currentTeam = changes[0];
        int currentStart = changes[1];

        // If the first control start is AFTER the target time → no one ever gets mid
        if (currentStart >= targetGameloop)
            return (team1, team2);

        // Iterate through all switches
        for (int i = 2; i < changes.Count; i++)
        {
            int nextTime = changes[i];

            if (nextTime > targetGameloop)
            {
                // The interval ends at the breakpoint
                int duration = targetGameloop - currentStart;
                if (currentTeam == 1) team1 += duration;
                else team2 += duration;

                return (team1, team2);
            }

            // Normal interval end
            int interval = nextTime - currentStart;
            if (interval > 0)
            {
                if (currentTeam == 1) team1 += interval;
                else team2 += interval;
            }

            // Switch team
            currentTeam = currentTeam == 1 ? 2 : 1;
            currentStart = nextTime;
        }

        // Handle final interval (control continues to targetGameloop)
        if (currentStart < targetGameloop)
        {
            int duration = targetGameloop - currentStart;
            if (currentTeam == 1) team1 += duration;
            else team2 += duration;
        }

        return (team1, team2);
    }

    public static string ComputeHash(this ReplayDto replay)
    {
        var sb = new StringBuilder();
        sb.Append(replay.Title);
        sb.Append(Data.CanonicalizeGametime(replay.Gametime).ToString("o")); // ISO 8601 UTC

        // Add players (sorted by ToonId for determinism)
        foreach (var player in replay.Players.OrderBy(p => p.GamePos)
                                            .ThenBy(p => p.Player?.ToonId.Realm)
                                            .ThenBy(p => p.Player?.ToonId.Id))
        {
            sb.Append(player.Player?.ToonId.Region);
            sb.Append(player.Player?.ToonId.Realm);
            sb.Append(player.Player?.ToonId.Id);
        }
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    public static (int, int) GetMiddleIncome2(this ReplayDto replay, int targetGameloop)
    {
        if (replay.MiddleChanges.Count < 2 || replay.Duration <= 0)
        {
            return (0, 0);
        }

        int team1control = 0;
        int team2control = 0;

        int currentTeam = replay.MiddleChanges[0];
        int currentGameloop = replay.MiddleChanges[1];

        foreach (var middle in replay.MiddleChanges[2..])
        {
            if (middle > targetGameloop)
            {
                var finalGameloops = targetGameloop - currentGameloop;
                if (finalGameloops > 0)
                {
                    if (currentTeam == 1)
                    {
                        team1control += finalGameloops;
                    }
                    else if (currentTeam == 2)
                    {
                        team2control += finalGameloops;
                    }
                }
                return (
                    (int)(team1control),
                    (int)(team2control)
                );
            }


            var controlledGameloops = middle - currentGameloop;
            if (currentTeam == 1)
            {
                team2control += controlledGameloops;
            }
            else
            {
                team1control += controlledGameloops;
            }

            currentTeam = currentTeam == 1 ? 2 : 1;
            currentGameloop = middle;
        }

        var finalControlledGameloops = targetGameloop - currentGameloop;
        if (finalControlledGameloops > 0)
        {
            if (currentTeam == 1)
                team1control += finalControlledGameloops;
            else if (currentTeam == 2)
                team2control += finalControlledGameloops;
        }

        return (
            (int)(team1control),
            (int)(team2control)
        );
    }

    public static void SetUploader(this ReplayDto replay, List<ToonIdDto> toonIds)
    {
        foreach (var toonId in toonIds)
        {
            var player = replay.Players.FirstOrDefault(p =>
                p.Player != null &&
                p.Player.ToonId.Region == toonId.Region &&
                p.Player.ToonId.Realm == toonId.Realm &&
                p.Player.ToonId.Id == toonId.Id);
            if (player != null)
            {
                player.IsUploader = true;
                break;
            }
        }
    }
}

public class MiddleControlHelper
{
    private readonly int duration;
    private readonly List<int> changes;

    private readonly int[] team1Prefix;
    private readonly int[] team2Prefix;

    public MiddleControlHelper(ReplayDto replay)
    {
        duration = replay.Duration;
        changes = replay.MiddleChanges ?? new List<int>();
        team1Prefix = new int[duration + 1];
        team2Prefix = new int[duration + 1];
        Precompute();
    }

    private void Precompute()
    {

        if (changes.Count < 2)
            return;

        int currentTeam = changes[0];
        int currentStart = changes[1];

        int switchIndex = 2;

        for (int t = 0; t <= duration; t++)
        {
            // Before the first control, nothing accumulated
            if (t < currentStart)
            {
                if (t > 0)
                {
                    team1Prefix[t] = team1Prefix[t - 1];
                    team2Prefix[t] = team2Prefix[t - 1];
                }
                continue;
            }

            // Process any switches that occur at this exact second
            while (switchIndex < changes.Count && changes[switchIndex] == t)
            {
                currentTeam = currentTeam == 1 ? 2 : 1;
                switchIndex++;
            }

            // Accumulate from previous second
            if (t > 0)
            {
                team1Prefix[t] = team1Prefix[t - 1];
                team2Prefix[t] = team2Prefix[t - 1];
            }

            // Add one second of control for the current team
            if (currentTeam == 1)
                team1Prefix[t]++;
            else if (currentTeam == 2)
                team2Prefix[t]++;
        }
    }

    /// <summary>
    /// Returns middle control percentages (0–100) up to atSecond.
    /// </summary>
    public (double, double) GetPercent(int atSecond)
    {
        if (duration <= 0)
            return (0, 0);

        int t = Math.Min(Math.Max(0, atSecond), duration);

        int t1 = team1Prefix[t];
        int t2 = team2Prefix[t];

        double p1 = Math.Round(t1 * 100.0 / duration, 2);
        double p2 = Math.Round(t2 * 100.0 / duration, 2);

        return (p1, p2);
    }
}

