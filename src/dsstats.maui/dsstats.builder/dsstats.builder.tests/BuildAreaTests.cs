namespace dsstats.builder.tests;

[TestClass]
public sealed class BuildAreaTests
{
    [TestMethod]
    public void CanPlaceUnit()
    {
        var buildArea = new BuildArea(2);
        var result = buildArea.PlaceUnit("Zergling", new(90, 75));
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CannotPlaceUnit()
    {
        var buildArea = new BuildArea(1);
        var result = buildArea.PlaceUnit("Zergling", new(0, 0));
        Assert.IsFalse(result);
    }
}
