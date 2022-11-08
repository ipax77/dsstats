namespace dsstats.import.tests;

public class UnitTest1 : TestWithSqlite
{
    [Fact]
    public async Task DbConnectTest()
    {
        Assert.True(await DbContext.Database.CanConnectAsync());
    }
}