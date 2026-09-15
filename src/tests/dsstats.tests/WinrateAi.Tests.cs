using System.Text.Json;
using dsstats.shared;
using dsstats.web.AI;

namespace dsstats.tests;

[TestClass]
public class WinrateAiTests
{
    private static WinrateQuery Default => new()
    {
        Supported = true, Interest = "None", Period = "Last90Days", Metric = "AvgPerformance",
        Order = "Descending", Take = 1, RatingFrom = 500, RatingTo = 3000, BalancedTeams = false, LongGames = false
    };

    [TestMethod]
    public void DefaultsProduceFreshCommanderRequest()
    {
        var request = WinrateQuery.Parse(JsonSerializer.Serialize(Default, WinrateQuery.JsonOptions)).ToRequest();
        Assert.AreEqual(StatsType.Winrate, request.Type);
        Assert.AreEqual(RatingType.Commanders, request.RatingType);
        Assert.AreEqual(TimePeriod.Last90Days, request.TimePeriod);
        Assert.AreEqual(Commander.None, request.Interest);
        Assert.IsFalse(request.WithLeavers);
        Assert.IsNull(request.Filter);
        Assert.IsNull(request.Comparison);
    }

    [TestMethod]
    public void CombinedFiltersPreserveSelectedPeriod()
    {
        var request = (Default with { Interest = "Kerrigan", Period = "Last12Months", RatingFrom = 2000,
            BalancedTeams = true, LongGames = true }).ToRequest();
        Assert.AreEqual(Commander.Kerrigan, request.Interest);
        Assert.AreEqual(2000, request.Filter!.RatingRange.From);
        Assert.AreEqual(40, request.Filter.Exp2WinRange.From);
        Assert.AreEqual(60, request.Filter.Exp2WinRange.To);
        Assert.AreEqual(900, request.Filter.DurationRange.From);
        var resolved = dbServices.Stats.StatsFilterResolver.Resolve(request);
        Assert.AreEqual(Data.GetTimePeriodInfo(TimePeriod.Last12Months).Start, resolved.FromDate);
    }

    [TestMethod]
    public void InvalidPlansAreRejected()
    {
        foreach (var query in new[] { Default with { Interest = "Batman" }, Default with { Interest = "80" },
            Default with { Interest = "Terran" }, Default with { Period = "Custom" }, Default with { Take = 19 },
            Default with { RatingFrom = 2400, RatingTo = 1800 }, Default with { RatingFrom = 499 },
            Default with { RatingTo = 3001 }, Default with { Metric = "Games" }, Default with { Order = "random" } })
            Assert.Throws<ArgumentException>(query.Validate);
        Assert.Throws<JsonException>(() => WinrateQuery.Parse("{}"));
        Assert.Throws<JsonException>(() => WinrateQuery.Parse("not json"));
        var json = JsonSerializer.Serialize(Default, WinrateQuery.JsonOptions);
        Assert.Throws<JsonException>(() => WinrateQuery.Parse(json.Insert(1, "\"sql\":\"drop table\",")));
        Assert.Throws<ArgumentException>(() => (Default with { Supported = false }).ToRequest());
    }

    [TestMethod]
    public void SelectionUsesRequestedMetricAndStableTies()
    {
        var response = new WinrateResponse { WinrateEnts = [
            new() { Commander = Commander.Kerrigan, Count = 100, Wins = 80, AvgPerformance = 1 },
            new() { Commander = Commander.Nova, Count = 100, Wins = 60, AvgPerformance = 2 },
            new() { Commander = Commander.Abathur, Count = 10, Wins = 8, AvgPerformance = 1 },
            new() { Commander = Commander.Alarak, Count = 0, Wins = 0, AvgPerformance = 100 }] };
        Assert.AreEqual(Commander.Nova, Default.Select(response)[0].Commander);
        Assert.AreEqual(Commander.Abathur, (Default with { Metric = "Winrate" }).Select(response)[0].Commander);
        Assert.AreEqual(Commander.Abathur, (Default with { Order = "Ascending" }).Select(response)[0].Commander);
        Assert.HasCount(3, (Default with { Take = 5 }).Select(response));
        Assert.HasCount(0, Default.Select(new()));
    }

    [TestMethod]
    public void PromptBundleLoadsAndBuildsIndependentMessages()
    {
        var prompt = WinratePrompt.Load(Path.Combine(AppContext.BaseDirectory, "AI", "v1.json"));
        Assert.AreEqual("v2", WinratePrompt.Load(Path.Combine(AppContext.BaseDirectory, "AI", "v2.json")).PromptVersion);
        Assert.AreEqual("v1", prompt.PromptVersion);
        Assert.AreEqual(64, prompt.Hash.Length);
        var messages = prompt.Messages("Who is best?");
        Assert.AreEqual("system", messages[0].Role);
        Assert.AreEqual("Who is best?", messages[^1].Content);
        Assert.AreEqual("Who is worst?", prompt.Messages("Who is worst?")[^1].Content);
        Assert.Throws<ArgumentException>(() => prompt.Messages(new string('a', 2001)));
        var path = Path.GetTempFileName();
        try
        {
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "AI", "v1.json"));
            File.WriteAllText(path, json.Replace("\"formatVersion\": 1", "\"formatVersion\": 2"));
            Assert.Throws<InvalidDataException>(() => WinratePrompt.Load(path));
            File.WriteAllText(path, json.Replace("\"additionalProperties\": false", "\"additionalProperties\": true"));
            Assert.Throws<InvalidDataException>(() => WinratePrompt.Load(path));
        }
        finally { File.Delete(path); }
    }
}
