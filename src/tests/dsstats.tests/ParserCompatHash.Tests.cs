using dsstats.shared;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.tests;

[TestClass]
public sealed class ParserCompatHashTests
{
    [TestMethod]
    public void ComputeParserCompatHash_UsesReplayCompatHashDigest()
    {
        ReplayDto replay = new()
        {
            CompatHash = "ds-compat-v1-raw-parser-value"
        };

        Assert.AreEqual(ComputeSha256(replay.CompatHash), replay.ComputeParserCompatHash());
    }

    [TestMethod]
    public void ComputeParserCompatHash_FallsBackToCompleteOrderedPlayerHashes()
    {
        ReplayDto replay = new()
        {
            Players =
            [
                CreatePlayer(2, 1, "player-hash-2"),
                CreatePlayer(1, 2, "player-hash-1")
            ]
        };

        var expectedInput = "ds-parser-player-compat-v1|2|player-hash-1|player-hash-2";

        Assert.AreEqual(ComputeSha256(expectedInput), replay.ComputeParserCompatHash());
    }

    [TestMethod]
    public void ComputeParserCompatHash_RequiresCompletePlayerHashesForFallback()
    {
        ReplayDto replay = new()
        {
            Players =
            [
                CreatePlayer(1, 1, "player-hash-1"),
                CreatePlayer(2, 2, null)
            ]
        };

        Assert.IsNull(replay.ComputeParserCompatHash());
    }

    [TestMethod]
    public void ComputeParserCompatHash_ReturnsNullWithoutParserHashData()
    {
        ReplayDto replay = new();

        Assert.IsNull(replay.ComputeParserCompatHash());
    }

    private static ReplayPlayerDto CreatePlayer(int gamePos, int toonId, string? compatHash)
    {
        return new()
        {
            CompatHash = compatHash,
            Name = $"Player{toonId}",
            GamePos = gamePos,
            TeamId = gamePos <= 3 ? 1 : 2,
            Player = new()
            {
                Name = $"Player{toonId}",
                ToonId = new()
                {
                    Region = 1,
                    Realm = 1,
                    Id = toonId
                }
            }
        };
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
