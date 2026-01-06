using dsstats.shared;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.tests;

[TestClass]
public class ReplayHashTests
{
    public static ReplayDto GetTestReplay(DateTime gametime)
    {
        return new()
        {
            Title = "Test Replay",
            Version = "1.0.0",
            GameMode = GameMode.Standard,
            Cannon = 10,
            Bunker = 10,
            Duration = 300,
            Gametime = gametime,
            RegionId = 1,
            BaseBuild = 1000,
            MiddleChanges = new List<int> { 150, 250 },
            Players = new List<ReplayPlayerDto>
            {
                new()
                {
                    GamePos = 1,
                    TeamId = 1,
                    Name = "PlayerOne",
                    Race = Commander.Terran,
                    Player = new PlayerDto() {
                        Name = "PlayerOne",
                        ToonId = new ToonIdDto()
                        {
                            Id = 1,
                            Realm = 1,
                            Region = 1,
                        }
                    }
                },
                new()
                {
                    GamePos = 4,
                    TeamId = 2,
                    Name = "PlayerTwo",
                    Race = Commander.Zerg,
                    Player = new PlayerDto() {
                        Name = "PlayerTwo",
                        ToonId = new ToonIdDto()
                        {
                            Id = 2,
                            Realm = 1,
                            Region = 1,
                        }
                    }
                },
            }
        };
    }

    public static string ComputeHash(ReplayDto replay, int timeBucketMinutes = 4)
    {
        var sb = new StringBuilder();

        sb.Append("TITLE:");
        sb.Append(replay.Title);

        sb.Append("|TIME:");
        sb.Append(timeBucketMinutes);
        sb.Append('|');
        sb.Append(RoundToNearestMinutes(replay.Gametime, timeBucketMinutes).ToString("o"));

        sb.Append("|PLAYERS:");

        foreach (var player in replay.Players
            .OrderBy(p => p.GamePos)
            .ThenBy(p => p.Player?.ToonId.Id)
            .ThenBy(p => p.Player?.ToonId.Region)
            .ThenBy(p => p.Player?.ToonId.Realm))
        {
            sb.Append('|');
            sb.Append(player.Player?.ToonId.Region);
            sb.Append(':');
            sb.Append(player.Player?.ToonId.Realm);
            sb.Append(':');
            sb.Append(player.Player?.ToonId.Id);
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    public static DateTime RoundToNearestMinutes(DateTime value, int minutes = 2)
    {
        value = value.ToUniversalTime();

        long bucketTicks = TimeSpan.FromMinutes(minutes).Ticks;
        long roundedTicks =
            value.Ticks / bucketTicks * bucketTicks;

        return new DateTime(roundedTicks, DateTimeKind.Utc);
    }

    [TestMethod]
    [DataRow(55)]
    [DataRow(56)]
    [DataRow(57)]
    [DataRow(58)]
    [DataRow(59)]
    [DataRow(60)]
    [DataRow(61)]
    [DataRow(62)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    public void ReplayHash_GametimeSensitivity(int offset)
    {
        DateTime gametime = new(2024, 1, 1, 12, 0, 0);
        gametime = gametime.AddSeconds(offset);
        var replay1 = GetTestReplay(gametime);
        var referenceHash = ComputeHash(replay1);

        for (int i = 0; i < 120; i++)
        {
            gametime = gametime.AddSeconds(1);
            var replayN = GetTestReplay(gametime);
            var referenceN = ComputeHash(replayN);
            Assert.AreEqual(referenceHash, referenceN, $"Hashes should be equal for gametime offset of {i + 1} seconds.");
        }
    }

    [TestMethod]
    public void ReplayHash_DeepGametimeSensitivity()
    {
        DateTime gametime = new(2024, 1, 1, 12, 0, 0);
        for (int i = 0; i < 120; i++)
        {
            var currentGametime = gametime.AddSeconds(i);
            var replay1 = GetTestReplay(gametime);
            var referenceHash = ComputeHash(replay1);

            for (int j = 0; j < 120; j++)
            {
                currentGametime = currentGametime.AddSeconds(1);
                var replayN = GetTestReplay(currentGametime);
                var referenceN = ComputeHash(replayN);
                Assert.AreEqual(referenceHash, referenceN, $"Hashes should be equal for gametime offset of {i + 1}/{j + 1} seconds.");
            }
        }
    }

    [TestMethod]
    public void ReplayHash_DiffGametimeSensitivity()
    {
        var replay1 = GetTestReplay(new DateTime(2024, 1, 1, 12, 0, 0));
        var replay2 = GetTestReplay(new DateTime(2024, 1, 1, 12, 10, 0));

        var referenceHash = ComputeHash(replay1);
        var referenceN = ComputeHash(replay2);
        Assert.AreNotEqual(referenceHash, referenceN, $"Hashes should be different for gametime difference of 10 minutes.");
    }

    [TestMethod]
    public void ReplayHash_DeepDiffGametimeSensitivity()
    {
        DateTime referenceGametime = new(2024, 1, 1, 12, 0, 0);
        for (int i = 10; i > 3; i--)
        {
            var diffGametime = referenceGametime.AddMinutes(i);
            var replay1 = GetTestReplay(referenceGametime);
            var replay2 = GetTestReplay(diffGametime);
            var referenceHash = ComputeHash(replay1);
            var referenceN = ComputeHash(replay2);
            Assert.AreNotEqual(referenceHash, referenceN, $"Hashes should be different for gametime difference of {i} minutes.");
        }
    }

    [TestMethod]
    public void ReplayHash_VeryDeepDiffGametimeSensitivity()
    {
        DateTime referenceGametime = new(2024, 1, 1, 12, 0, 0);

        for (int i = 0; i < 120; i++)
        {
            var currentReferenceGametime = referenceGametime.AddSeconds(1);

            for (int j = 10; j > 3; j--)
            {
                var diffGametime = currentReferenceGametime.AddMinutes(j);
                var replay1 = GetTestReplay(currentReferenceGametime);
                var replay2 = GetTestReplay(diffGametime);
                var referenceHash = ComputeHash(replay1);
                var referenceN = ComputeHash(replay2);
                Assert.AreNotEqual(referenceHash, referenceN, $"Hashes should be different for gametime difference of {i}/{j} minutes.");
            }
        }
    }
}
