using dsstats.shared;
using dsstats.shared.Units;

namespace dsstats.tests;

[TestClass]
public sealed class UnitRepresentationTests
{
    private static readonly (string RawName, Commander Commander, string CanonicalName)[] RestoredAliases =
    [
        ("SiegeTank", Commander.Raynor, "Siege Tank"),
        ("SiegeTank", Commander.Swann, "Siege Tank"),
        ("BroodQueen", Commander.Stukov, "Brood Queen"),
        ("ShadowGuard", Commander.Vorazun, "Shadow Guard")
    ];

    [TestMethod]
    public void Resolve_RestoredAliases_UsesCanonicalAndDisplayNames()
    {
        foreach (var (rawName, commander, canonicalName) in RestoredAliases)
        {
            var representation = UnitMapNg.Resolve(rawName, commander);

            Assert.AreEqual(canonicalName, representation.CanonicalName);
            Assert.AreEqual(canonicalName, representation.DisplayName);
        }
    }

    [TestMethod]
    public void Resolve_CommanderSiegeTanks_PreservesVisualMetadata()
    {
        var raynorTank = UnitMapNg.Resolve("SiegeTank", Commander.Raynor);
        var swannTank = UnitMapNg.Resolve("SiegeTank", Commander.Swann);

        Assert.AreEqual(UnitSize.Normal, raynorTank.Size);
        Assert.AreEqual(UnitType.Ground, raynorTank.Type);
        Assert.AreEqual(WeaponTarget.Ground, raynorTank.MovementType);
        Assert.AreEqual(10, raynorTank.Radius);
        Assert.AreEqual(UnitMapNg.GetUnitColor(UnitColor.Color14), raynorTank.Color);

        Assert.AreEqual(UnitSize.Normal, swannTank.Size);
        Assert.AreEqual(UnitType.Ground, swannTank.Type);
        Assert.AreEqual(WeaponTarget.Ground, swannTank.MovementType);
        Assert.AreEqual(10, swannTank.Radius);
        Assert.AreEqual(UnitMapNg.GetUnitColor(UnitColor.Color9), swannTank.Color);
    }

    [TestMethod]
    public void Resolve_UnknownUnit_PreservesExistingFallback()
    {
        var representation = UnitMapNg.Resolve("UnknownUnit", Commander.Swann);

        Assert.AreEqual("UnknownUnit", representation.CanonicalName);
        Assert.AreEqual("UnknownUnit", representation.DisplayName);
        Assert.AreEqual(12, representation.Radius);
        Assert.AreEqual("#EC7063", representation.Color);
        Assert.IsNull(representation.Cost);
        Assert.IsNull(representation.Life);
    }
}
