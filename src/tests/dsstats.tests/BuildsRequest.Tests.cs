using dsstats.shared;
using dsstats.shared.Extensions;
using System.Collections.Frozen;

namespace dsstats.tests;

[TestClass]
public sealed class BuildsRequestTests
{
    private static readonly FrozenDictionary<string, string> QueryParameterMap =
        new Dictionary<string, string>
        {
            [nameof(BuildsRequest.RatingType)] = "Rt",
            [nameof(BuildsRequest.TimePeriod)] = "Tp",
            [nameof(BuildsRequest.Interest)] = "Int",
            [nameof(BuildsRequest.Versus)] = "Vs",
            [nameof(BuildsRequest.FromRating)] = "Fr",
            [nameof(BuildsRequest.ToRating)] = "Tr",
            [nameof(BuildsRequest.Breakpoint)] = "Bp"
        }.ToFrozenDictionary();

    [TestMethod]
    public void BasicTimePeriods_ContainsOnlySupportedBuildPeriods()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                TimePeriod.Last90Days,
                TimePeriod.Previous90Days,
                TimePeriod.Last12Months,
                TimePeriod.Previous12Months,
                TimePeriod.ThisYear,
                TimePeriod.LastYear,
                TimePeriod.AllTime
            },
            Data.BasicTimePeriods.ToArray());
    }

    [TestMethod]
    [DataRow(TimePeriod.None)]
    [DataRow(TimePeriod.Custom)]
    [DataRow((TimePeriod)999)]
    public void NormalizeBasicTimePeriod_UnsupportedValue_ReturnsLast90Days(TimePeriod timePeriod)
    {
        Assert.AreEqual(TimePeriod.Last90Days, Data.NormalizeBasicTimePeriod(timePeriod));
    }

    [TestMethod]
    public void BuildQueryParams_UnsupportedTimePeriod_DoesNotPersistInvalidUrlValue()
    {
        var request = new BuildsRequest
        {
            TimePeriod = TimePeriod.None,
            Interest = Commander.Abathur,
            FromRating = 2000,
            ToRating = Data.MaxBuildRating,
            Breakpoint = Breakpoint.Min10
        };

        var queryParameters = request.BuildQueryParams(QueryParameterMap);

        Assert.IsNull(queryParameters["Tp"]);
    }
}
