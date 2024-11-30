using dsstats.shared;

namespace dsstats.ratings.tests;

[TestClass]
public class ReplayChartDtoTests
{
    [TestMethod]
    public void TestYearWeekToDate1()
    {
        int year = 2023;
        int week = 52;

        ReplayChartDto dto = new ReplayChartDto(year, week);

        Assert.AreEqual(new DateTime(2023, 12, 25), dto.GameTime);
    }

    [TestMethod]
    public void TestYearWeekToDate2()
    {
        int year = 2023;
        int week = 53;

        ReplayChartDto dto = new ReplayChartDto(year, week);

        Assert.AreEqual(new DateTime(2024, 1, 1), dto.GameTime);
    }

    //[TestMethod]
    //public void TestYearWeekToDate3()
    //{
    //    int year = 2024;
    //    int week = 0;

    //    ReplayChartDto dto = new ReplayChartDto(year, week);

    //    Assert.AreEqual(new DateTime(2024, 1, 1), dto.GameTime);
    //}

    [TestMethod]
    public void TestYearWeekToDate4()
    {
        int year = 2024;
        int week = 1;

        ReplayChartDto dto = new ReplayChartDto(year, week);

        Assert.AreEqual(new DateTime(2024, 1, 1), dto.GameTime);
    }

    [TestMethod]
    public void TestYearWeekToDate5()
    {
        int year = 2024;
        int week = 2;

        ReplayChartDto dto = new ReplayChartDto(year, week);

        Assert.AreEqual(new DateTime(2024, 1, 8), dto.GameTime);
    }

    [TestMethod]
    public void TestYearWeekToDate6()
    {
        int year = 2024;
        int week = 3;

        ReplayChartDto dto = new ReplayChartDto(year, week);

        Assert.AreEqual(new DateTime(2024, 1, 15), dto.GameTime);
    }
}
