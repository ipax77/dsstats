using dsstats.shared;
using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public class ReplayHashMinTests
{
    [TestMethod]
    [DeploymentItem("testdata/min1_1.json")]
    [DeploymentItem("testdata/min1_2.json")]
    public async Task CanComputeSameReplayHash()
    {
        var minReplay1 = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText("min1_1.json"));
        var minReplay2 = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText("min1_2.json"));

        Assert.IsNotNull(minReplay1);
        Assert.IsNotNull(minReplay2);

        var hash1 = minReplay1.ReComputeHash();
        Assert.IsFalse(string.IsNullOrEmpty(hash1));

        var hash2 = minReplay2.ReComputeHash();
        Assert.IsFalse(string.IsNullOrEmpty(hash2));
        Assert.AreNotEqual(hash1, hash2);

    }
}
