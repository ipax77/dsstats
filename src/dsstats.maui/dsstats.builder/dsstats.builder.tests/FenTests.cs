namespace dsstats.builder.tests;

[TestClass]
public sealed class FenTests
{
    [TestMethod]
    public void CanCreateFen()
    {
        int team = 2;
        var build = CmdrBuildFactory.Create(shared.Commander.Zerg);
        Assert.IsNotNull(build);
        var possString = "90,79";
        var unitName = "Ultralisk";

        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits(unitName, possString, team);

        var fen = buildArea.ToFenString(build);

        var fenBuildArea = new BuildArea(team);
        fenBuildArea.FromFenString(fen, build);
        var reverseFen = fenBuildArea.ToFenString(build);

        Assert.AreEqual(fen, reverseFen);
    }

    [TestMethod]
    public void CanCreateFen_WithAirUnit()
    {
        int team = 2;
        var build = CmdrBuildFactory.Create(shared.Commander.Zerg);
        Assert.IsNotNull(build);

        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Mutalisk", "91,80", team); // air unit

        var fen = buildArea.ToFenString(build);
        var fenBuildArea = new BuildArea(team);
        fenBuildArea.FromFenString(fen, build);
        var reverseFen = fenBuildArea.ToFenString(build);

        Assert.AreEqual(fen, reverseFen);
    }

    [TestMethod]
    public void CanCreateFen_WithMultipleUnits()
    {
        int team = 2;
        var build = CmdrBuildFactory.Create(shared.Commander.Zerg);
        Assert.IsNotNull(build);

        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Zergling", "91,80", team);
        buildArea.PlaceUnits("Queen", "92,80", team);
        buildArea.PlaceUnits("Overseer", "91,81", team); // air

        var fen = buildArea.ToFenString(build);
        var fenBuildArea = new BuildArea(team);
        fenBuildArea.FromFenString(fen, build);
        var reverseFen = fenBuildArea.ToFenString(build);

        Assert.AreEqual(fen, reverseFen);
    }

    [TestMethod]
    public void CanCreateFen_WithToggledUnit()
    {
        int team = 2;
        var build = CmdrBuildFactory.Create(shared.Commander.Zerg);
        Assert.IsNotNull(build);

        // Assume "Lurker" shares key 'a' with Hydralisk, but Hydralisk is active,
        // so Lurker should appear as 'A' (uppercase)
        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Lurker", "93,82", team);

        var fen = buildArea.ToFenString(build);
        Assert.IsTrue(fen.Contains('A'), $"fen does not contain upper case A: {fen}");

        var fenBuildArea = new BuildArea(team);
        fenBuildArea.FromFenString(fen, build);
        var reverseFen = fenBuildArea.ToFenString(build);

        Assert.AreEqual(fen, reverseFen);
    }
}
