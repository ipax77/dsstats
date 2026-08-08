using dsstats.api;
using Microsoft.Extensions.Configuration;

namespace dsstats.tests;

[TestClass]
public class DatabaseServerVersionTests
{
    [TestMethod]
    public void GetServerVersionUsesConfiguredVersion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["dsstats:ServerVersion"] = "8.4.11",
            })
            .Build();

        var serverVersion = DatabaseServiceExtensions.GetServerVersion(configuration);

        Assert.AreEqual(new Version(8, 4, 11), serverVersion.Version);
    }

    [TestMethod]
    public void GetServerVersionUsesDefaultWhenMissing()
    {
        var serverVersion = DatabaseServiceExtensions.GetServerVersion(
            new ConfigurationBuilder().Build());

        Assert.AreEqual(new Version(9, 7, 0), serverVersion.Version);
    }

    [TestMethod]
    public void GetServerVersionRejectsInvalidValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["dsstats:ServerVersion"] = "latest",
            })
            .Build();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => DatabaseServiceExtensions.GetServerVersion(configuration));

        StringAssert.Contains(exception.Message, "dsstats:ServerVersion");
    }
}
