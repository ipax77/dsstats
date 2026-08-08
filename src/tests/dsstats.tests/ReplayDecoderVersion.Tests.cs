using dsstats.shared.Upload;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayDecoderVersionTests
{
    [TestMethod]
    [DataRow(ReplayDecoderSource.Maui, "ma3.1.0")]
    [DataRow(ReplayDecoderSource.MyDsstats, "myds3.1.0")]
    [DataRow(ReplayDecoderSource.Service, "ser3.1.0")]
    [DataRow(ReplayDecoderSource.Api, "api3.1.0")]
    public void Format_UsesCanonicalPrefix(ReplayDecoderSource source, string expected)
    {
        Assert.AreEqual(expected, ReplayDecoderVersion.Format(source, new Version(3, 1, 0)));
    }

    [TestMethod]
    [DataRow("ma3.1.2", ReplayDecoderSource.Maui, "3.1.2")]
    [DataRow("MYDS3.1.3", ReplayDecoderSource.MyDsstats, "3.1.3")]
    [DataRow(" ser3.1.4 ", ReplayDecoderSource.Service, "3.1.4")]
    [DataRow("api3.1.5", ReplayDecoderSource.Api, "3.1.5")]
    [DataRow("3.0.12", ReplayDecoderSource.Maui, "3.0.12")]
    [DataRow("other-client", ReplayDecoderSource.Maui, "other-client")]
    [DataRow("", ReplayDecoderSource.Maui, ReplayDecoderVersion.UnknownVersion)]
    [DataRow(null, ReplayDecoderSource.Maui, ReplayDecoderVersion.UnknownVersion)]
    public void Parse_NormalizesCanonicalAndLegacyValues(
        string? raw,
        ReplayDecoderSource expectedSource,
        string expectedVersion)
    {
        var parsed = ReplayDecoderVersion.Parse(raw);

        Assert.AreEqual(expectedSource, parsed.Source);
        Assert.AreEqual(expectedVersion, parsed.Version);
    }

    [TestMethod]
    public void Format_RejectsUnknownSource()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ReplayDecoderVersion.Format(ReplayDecoderSource.Unknown, new Version(3, 1, 0)));
    }

    [TestMethod]
    public void LimitVersionForStorage_OnlyAllocatesWhenTruncationIsRequired()
    {
        var unchanged = new string('1', ReplayDecoderVersion.MaxDecoderVersionLength);
        Assert.AreSame(unchanged, ReplayDecoderVersion.LimitVersionForStorage(unchanged));

        var oversized = new string('2', ReplayDecoderVersion.MaxDecoderVersionLength + 1);
        Assert.AreEqual(
            new string('2', ReplayDecoderVersion.MaxDecoderVersionLength),
            ReplayDecoderVersion.LimitVersionForStorage(oversized));
    }
}
