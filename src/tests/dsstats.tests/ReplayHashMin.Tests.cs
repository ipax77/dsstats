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
        var minReplay2 = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText("min1_1.json"));

        Assert.IsNotNull(minReplay1);
        Assert.IsNotNull(minReplay2);

        var hash1 = minReplay1.ReComputeHash();
        Assert.AreEqual("9DF74258E7B22ECF341CAD80B0D8BBAC2BC4DA9AE2C7843647079C360B3EE76B", hash1);

        var hash2 = minReplay2.ReComputeHash();
        Assert.AreEqual("97C90F2C413F26645C290677C9B8F3115B85C92D007A71B3919B6A2CB3576817", hash2);

    }
}
