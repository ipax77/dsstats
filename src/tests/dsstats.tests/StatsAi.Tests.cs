using System.Text.Json;
using dsstats.shared;
using dsstats.web.AI;

namespace dsstats.tests;

[TestClass]
public class StatsAiTests
{
    private static StatsQueryPlan Default => new()
    {
        Supported = true, QueryType = "Winrate", Commander = "None", Period = "Last90Days",
        Metric = "Performance", ReturnMode = "Ranked", Order = "Descending", Take = 1,
        RatingFrom = 500, RatingTo = 3000, BalancedTeams = false, LongGames = false
    };

    [TestMethod]
    public void AllTypesPreserveCommanderAndFilters()
    {
        foreach (var type in new[] { "Winrate", "Synergy", "Timeline" })
        foreach (var period in new[] { "Last90Days", "Last12Months", "AllTime" })
        {
            var plan = Default with { QueryType = type, Commander = "Kerrigan", Period = period,
                RatingFrom = 1837, RatingTo = 2456, BalancedTeams = true, LongGames = true };
            var request = StatsQueryPlan.Parse(JsonSerializer.Serialize(plan, WinrateQuery.JsonOptions)).ToRequest();
            Assert.AreEqual(Enum.Parse<StatsType>(type), request.Type);
            Assert.AreEqual(Commander.Kerrigan, request.Interest);
            Assert.AreEqual(Enum.Parse<TimePeriod>(period), request.TimePeriod);
            Assert.AreEqual(RatingType.Commanders, request.RatingType);
            Assert.IsFalse(request.WithLeavers);
            Assert.IsNull(request.Comparison);
            Assert.AreEqual(1837, request.Filter!.RatingRange.From);
            Assert.AreEqual(2456, request.Filter.RatingRange.To);
            Assert.AreEqual(40, request.Filter.Exp2WinRange.From);
            Assert.AreEqual(60, request.Filter.Exp2WinRange.To);
            Assert.AreEqual(900, request.Filter.DurationRange.From);
            Assert.AreEqual(Data.GetTimePeriodInfo(request.TimePeriod).Start, request.Filter.DateRange.From);
        }
        Assert.IsNull(Default.ToRequest().Filter);
        Assert.AreNotSame(Default.ToRequest(), Default.ToRequest());
    }

    [TestMethod]
    public void InvalidContractsCannotExecute()
    {
        foreach (var plan in new[] { Default with { QueryType = "Count" }, Default with { QueryType = "Timeline" },
            Default with { ReturnMode = "Series" }, Default with { Commander = "Terran" }, Default with { Commander = "80" },
            Default with { Metric = "AvgPerformance" }, Default with { Metric = "Games" }, Default with { Period = "Custom" },
            Default with { RatingFrom = 2000, RatingTo = 1900 }, Default with { RatingFrom = 499 },
            Default with { RatingTo = 3001 }, Default with { Take = 0 }, Default with { Take = 19 },
            Default with { ReturnMode = "Other" }, Default with { QueryType = "Timeline", Commander = "Nova", ReturnMode = "Series" },
            Default with { QueryType = "Timeline", Commander = "Nova", ReturnMode = "Series", Order = "Ascending", Take = 2 } })
            Assert.Throws<ArgumentException>(plan.Validate);
        Assert.Throws<ArgumentException>(() => (Default with { Supported = false }).ToRequest());
        foreach (var json in new[] { "{}", "null", "not json", JsonSerializer.Serialize(Default, WinrateQuery.JsonOptions).Replace("\"None\"", "null"),
            JsonSerializer.Serialize(Default, WinrateQuery.JsonOptions).Insert(1, "\"sql\":\"SELECT 1\",") })
            Assert.Throws<JsonException>(() => StatsQueryPlan.Parse(json));
        Assert.Throws<ArgumentException>(() => Default.Select(new TimelineResponse()));
    }

    [TestMethod]
    public void SynergyMatchesEitherSideAndRanksRawWinrate()
    {
        var response = new SynergyResponse { SynergyEnts = [
            new() { Commander = Commander.Abathur, Teammate = Commander.Kerrigan, Games = 10, Wins = 8, AvgGain = 1, Winrate = 0 },
            new() { Commander = Commander.Kerrigan, Teammate = Commander.Nova, Games = 100, Wins = 60, AvgGain = 2, Winrate = 1 },
            new() { Commander = Commander.Alarak, Teammate = Commander.Kerrigan, Games = 10, Wins = 8, AvgGain = 1 },
            new() { Commander = Commander.Kerrigan, Teammate = Commander.Zeratul, Games = 0, AvgGain = 99 },
            new() { Commander = Commander.Nova, Teammate = Commander.Raynor, Games = 10, Wins = 10, AvgGain = 10 }
        ] };
        var plan = Default with { QueryType = "Synergy", Commander = "Kerrigan" };
        Assert.AreEqual("Nova", plan.Select(response)[0].Label);
        Assert.AreEqual("Abathur", (plan with { Metric = "Winrate" }).Select(response)[0].Label);
        Assert.AreEqual("Abathur", (plan with { Order = "Ascending" }).Select(response)[0].Label);
        Assert.HasCount(3, (plan with { Take = 18 }).Select(response));
        Assert.AreEqual("Nova + Raynor", (plan with { Commander = "None" }).Select(response)[0].Label);
        Assert.HasCount(0, plan.Select(new SynergyResponse()));
    }

    [TestMethod]
    public void TimelineRanksBucketsOrReturnsCompleteChronologicalSeries()
    {
        var response = new TimelineResponse { TimelineEnts = [
            new() { Commander = Commander.Nova, Steps = [new() { BucketStart = 5, Count = 10, Wins = 10, AvgGain = 100 }] },
            new() { Commander = Commander.Kerrigan, Steps = [
                new() { BucketStart = 35, Count = 100, Wins = 60, AvgGain = 4 },
                new() { BucketStart = 8, Count = 10, Wins = 8, AvgGain = 1 },
                new() { BucketStart = 11, Count = 0, AvgGain = 99 },
                new() { BucketStart = 5, Count = 10, Wins = 8, AvgGain = 1 }
            ] }
        ] };
        var plan = Default with { QueryType = "Timeline", Commander = "Kerrigan" };
        Assert.AreEqual("35+ min", plan.Select(response)[0].Label);
        Assert.AreEqual("5–8 min", (plan with { Metric = "Winrate" }).Select(response)[0].Label);
        Assert.AreEqual("5–8 min", (plan with { Order = "Ascending" }).Select(response)[0].Label);
        var series = (plan with { ReturnMode = "Series", Order = "Ascending" }).Select(response);
        CollectionAssert.AreEqual(new[] { "5–8 min", "8–11 min", "35+ min" }, series.Select(x => x.Label).ToArray());
        Assert.HasCount(0, plan.Select(new TimelineResponse()));
        Assert.HasCount(0, (plan with { Commander = "Swann" }).Select(response));
    }

    [TestMethod]
    public void WinrateSelectionRetainsPreviousBehavior()
    {
        var response = new WinrateResponse { WinrateEnts = [
            new() { Commander = Commander.Nova, Count = 100, Wins = 60, AvgPerformance = 2 },
            new() { Commander = Commander.Kerrigan, Count = 10, Wins = 8, AvgPerformance = 1 },
            new() { Commander = Commander.Abathur, Count = 10, Wins = 8, AvgPerformance = 1 },
            new() { Commander = Commander.Alarak, Count = 0, AvgPerformance = 99 }
        ] };
        Assert.AreEqual("Nova", Default.Select(response)[0].Label);
        Assert.AreEqual("Abathur", (Default with { Metric = "Winrate" }).Select(response)[0].Label);
        Assert.AreEqual("Abathur", (Default with { Order = "Ascending" }).Select(response)[0].Label);
        Assert.HasCount(0, Default.Select(new WinrateResponse()));
    }

    [TestMethod]
    public void PromptAndHeldOutCasesMatchSchemaAndStartFresh()
    {
        var prompt = StatsPrompt.Load(Path.Combine(AppContext.BaseDirectory, "AI", "stats", "v1.json"));
        Assert.HasCount(8, prompt.Examples);
        Assert.AreEqual(64, prompt.Hash.Length);
        var messages = prompt.Messages("Who is best?");
        Assert.AreEqual("system", messages[0].Role);
        Assert.AreEqual(1, messages.Count(x => x.Role == "system"));
        Assert.AreEqual("Who is worst?", prompt.Messages("Who is worst?")[^1].Content);
        Assert.Throws<ArgumentException>(() => prompt.Messages(new string('a', 2001)));
        using var suite = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "AI", "stats-cases.json")));
        var plans = prompt.Examples.Select(x => JsonSerializer.SerializeToElement(x.Answer, WinrateQuery.JsonOptions))
            .Concat(suite.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetProperty("expected")));
        foreach (var json in plans)
        {
            var plan = StatsQueryPlan.Parse(json.GetRawText());
            if (plan.Supported) _ = plan.ToRequest();
            foreach (var field in json.EnumerateObject())
            {
                var schema = prompt.ResponseSchema.GetProperty("properties").GetProperty(field.Name);
                if (schema.TryGetProperty("enum", out var choices))
                    Assert.IsTrue(choices.EnumerateArray().Any(x => x.GetString() == field.Value.GetString()), field.Name);
                if (schema.TryGetProperty("minimum", out var min)) Assert.IsTrue(field.Value.GetInt32() >= min.GetInt32());
                if (schema.TryGetProperty("maximum", out var max)) Assert.IsTrue(field.Value.GetInt32() <= max.GetInt32());
            }
        }
    }
}
