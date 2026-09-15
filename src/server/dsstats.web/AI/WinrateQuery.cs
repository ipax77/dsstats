using System.Text.Json;
using System.Text.Json.Serialization;
using dsstats.shared;

namespace dsstats.web.AI;

public sealed record WinrateQuery
{
    public required bool Supported { get; init; }
    public required string Interest { get; init; }
    public required string Period { get; init; }
    public required string Metric { get; init; }
    public required string Order { get; init; }
    public required int Take { get; init; }
    public required int RatingFrom { get; init; }
    public required int RatingTo { get; init; }
    public required bool BalancedTeams { get; init; }
    public required bool LongGames { get; init; }

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true
    };

    public static WinrateQuery Parse(string json)
    {
        var query = JsonSerializer.Deserialize<WinrateQuery>(json, JsonOptions)
            ?? throw new JsonException("The model returned an empty plan.");
        query.Validate();
        return query;
    }

    public void Validate()
    {
        if (!Enum.TryParse<Commander>(Interest, out var commander) || commander.ToString() != Interest ||
            (commander != Commander.None && !IsCommander(commander)))
            throw new ArgumentException("Unknown commander.");
        if (Period is not ("Last90Days" or "Last12Months" or "AllTime") ||
            Metric is not ("AvgPerformance" or "Winrate") || Order is not ("Ascending" or "Descending") ||
            Take is < 1 or > 18 || RatingFrom < Data.MinBuildRating || RatingTo > Data.MaxBuildRating || RatingFrom > RatingTo)
            throw new ArgumentException("The query contains unsupported settings or invalid bounds.");
    }

    public static bool IsCommander(Commander commander) => commander >= Commander.Abathur &&
        commander <= Commander.Zeratul && Enum.IsDefined(commander);

    public StatsRequest ToRequest()
    {
        Validate();
        if (!Supported) throw new ArgumentException("This question is outside the supported winrate queries.");
        var period = Enum.Parse<TimePeriod>(Period);
        var request = new StatsRequest { Type = StatsType.Winrate, RatingType = RatingType.Commanders,
            TimePeriod = period, Interest = Enum.Parse<Commander>(Interest), WithLeavers = false };
        if (RatingFrom != Data.MinBuildRating || RatingTo != Data.MaxBuildRating || BalancedTeams || LongGames)
        {
            var time = Data.GetTimePeriodInfo(period);
            request.Filter = new StatsFilter
            {
                DateRange = new() { From = time.Start, To = time.End },
                RatingRange = new() { From = RatingFrom, To = RatingTo },
                Exp2WinRange = new() { From = BalancedTeams ? 40 : 0, To = BalancedTeams ? 60 : 100 },
                DurationRange = new() { From = LongGames ? 900 : Data.MinDuration, To = Data.MaxDuration }
            };
        }
        return request;
    }

    public IReadOnlyList<WinrateEnt> Select(WinrateResponse response)
    {
        Validate();
        var rows = response.WinrateEnts.Where(x => x.Count > 0 && IsCommander(x.Commander));
        double Key(WinrateEnt row) => Metric == "Winrate" ? row.Wins / (double)row.Count : row.AvgPerformance;
        return (Order == "Ascending" ? rows.OrderBy(Key) : rows.OrderByDescending(Key))
            .ThenBy(x => x.Commander).Take(Take).ToArray();
    }

    public string Describe() => $"Commanders · {Data.GetTimePeriodInfo(Enum.Parse<TimePeriod>(Period)).Name} · " +
        $"{(Interest == "None" ? "All commanders" : $"Opponents of {Interest}")} · no leavers · " +
        $"rating {RatingFrom}–{RatingTo}" + (BalancedTeams ? " · balanced teams (40–60%)" : "") +
        (LongGames ? " · games ≥15 minutes" : "") +
        $" · {(Order == "Ascending" ? "bottom" : "top")} {Take} by {(Metric == "Winrate" ? "raw winrate" : "average rating gain")}";
}
