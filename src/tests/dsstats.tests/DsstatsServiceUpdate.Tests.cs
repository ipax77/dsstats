using dsstats.service.Services;
using System.Net;
using System.Security.Cryptography;

namespace dsstats.tests;

[TestClass]
public sealed class DsstatsServiceUpdateTests
{
    [TestMethod]
    public void CurrentVersion_IsReleaseVersion()
    {
        Assert.AreEqual(new Version(3, 0, 10), DsstatsService.CurrentVersion);
    }

    [TestMethod]
    public async Task DownloadInstaller_ValidHash_ReplacesInstaller()
    {
        var root = CreateTempDirectory();
        var destination = Path.Combine(root, "dsstats.installer.msi");
        var content = RandomNumberGenerator.GetBytes(150_000);
        await File.WriteAllTextAsync(destination, "old installer");

        try
        {
            using var client = CreateHttpClient(content);
            var expectedHash = Convert.ToHexString(SHA256.HashData(content));

            var result = await DsstatsService.DownloadInstaller(
                client,
                destination,
                expectedHash,
                CancellationToken.None);

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(destination));
            Assert.IsFalse(File.Exists(destination + ".download"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task DownloadInstaller_InvalidHash_PreservesExistingInstaller()
    {
        var root = CreateTempDirectory();
        var destination = Path.Combine(root, "dsstats.installer.msi");
        var existingContent = "old installer"u8.ToArray();
        await File.WriteAllBytesAsync(destination, existingContent);

        try
        {
            using var client = CreateHttpClient("invalid update"u8.ToArray());

            var result = await DsstatsService.DownloadInstaller(
                client,
                destination,
                new string('0', 64),
                CancellationToken.None);

            Assert.IsFalse(result);
            CollectionAssert.AreEqual(existingContent, await File.ReadAllBytesAsync(destination));
            Assert.IsFalse(File.Exists(destination + ".download"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static HttpClient CreateHttpClient(byte[] content)
    {
        return new(new StaticContentHandler(content))
        {
            BaseAddress = new("https://updates.example/")
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsstats-service-update-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StaticContentHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content),
                RequestMessage = request
            });
        }
    }
}
