using System.Net;
using dsstats.api;

namespace dsstats.tests;

[TestClass]
public sealed class ApiClientIpTests
{
    [TestMethod]
    public void GetClientPartitionKey_NormalizesMappedIpv4Address()
    {
        Assert.AreEqual(
            ClientPartitionKeys.GetClientPartitionKey(IPAddress.Parse("127.0.0.1")),
            ClientPartitionKeys.GetClientPartitionKey(IPAddress.Parse("::ffff:127.0.0.1")));
    }

    [TestMethod]
    public void GetClientPartitionKey_UsesUnknownClientForMissingIp()
    {
        Assert.AreEqual("unknown-client", ClientPartitionKeys.GetClientPartitionKey((IPAddress?)null));
    }
}
