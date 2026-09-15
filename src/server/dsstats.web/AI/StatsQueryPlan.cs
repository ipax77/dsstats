using System.Text.Json;
using System.Text.Json.Serialization;
using dsstats.shared;

namespace dsstats.web.AI;

public sealed record StatsQueryPlan
{
    public required bool Supported { get; init; }
    public required string QueryType { get; init; }
    public required string Commander { get; init; }
    public required string Period { get; init; }
    public required string Metric { get; init; }
    public required string ReturnMode { get; init; }
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

    public static StatsQueryPlan Parse(string json)
    {
        var plan = JsonSerializer.Deserialize<StatsQueryPlan>(json, JsonOptions)
            ?? throw new JsonException("The model returned an empty plan.");
        plan.Validate();
        return plan;
    }

    public void Validate()
    {
        if (!Enum.TryParse<shared.Commander>(Commander, out var commander) || commander.ToString() != Commander ||
            (commander != shared.Commander.None && !IsCommander(commander)))
            throw new ArgumentException("Unknown commander.");
        if (QueryType is not ("Winrate" or "Synergy" or "Timeline") ||
            Period is not ("Last90Days" or "Last12Months" or "AllTime") ||
            Order is not ("Ascending" or "Descending") || Take is < 1 or > 18 ||
            RatingFrom < Data.MinBuildRating || RatingTo > Data.MaxBuildRating || RatingFrom > RatingTo ||
            Metric is not ("Performance" or "Winrate") || ReturnMode is not ("Ranked" or "Series") ||
            (QueryType == "Timeline" && Commander == "None") ||
            (ReturnMode == "Series" && (QueryType != "Timeline" || Order != "Ascending" || Take != 1)))
            throw new ArgumentException("The query contains unsupported settings.");
    }

    public static bool IsCommander(shared.Commander commander) => commander >= shared.Commander.Abathur &&
        commander <= shared.Commander.Zeratul && Enum.IsDefined(commander);

    public StatsRequest ToRequest()
    {
        Validate();
        if (!Supported) throw new ArgumentException("This question is outside the supported statistics queries.");
        var period = Enum.Parse<TimePeriod>(Period);
        var request = new StatsRequest
        {
            Type = Enum.Parse<StatsType>(QueryType), RatingType = RatingType.Commanders,
            TimePeriod = period, Interest = Enum.Parse<shared.Commander>(Commander), WithLeavers = false
        };
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

    public IReadOnlyList<StatsAnswerRow> Select(IStatsResponse response)
    {
        Validate();
        var commander = Enum.Parse<shared.Commander>(Commander);
        IEnumerable<StatsAnswerRow> rows = (QueryType, response) switch
        {
            ("Winrate", WinrateResponse winrate) => winrate.WinrateEnts
                .Where(x => IsCommander(x.Commander))
                .Select(x => new StatsAnswerRow(x.Commander.ToString(), x.Count, x.Wins, x.AvgPerformance, (int)x.Commander)),
            ("Synergy", SynergyResponse synergy) => synergy.SynergyEnts
                .Where(x => IsCommander(x.Commander) && IsCommander(x.Teammate))
                .Where(x => commander == shared.Commander.None || x.Commander == commander || x.Teammate == commander)
                .Select(x => new StatsAnswerRow(commander == shared.Commander.None
                    ? $"{x.Commander} + {x.Teammate}"
                    : (x.Commander == commander ? x.Teammate : x.Commander).ToString(),
                    x.Games, x.Wins, x.AvgGain, Math.Min((int)x.Commander, (int)x.Teammate), Math.Max((int)x.Commander, (int)x.Teammate))),
            ("Timeline", TimelineResponse timeline) => timeline.TimelineEnts.Where(x => x.Commander == commander)
                .SelectMany(x => x.Steps).Select(x => new StatsAnswerRow(BucketLabel(x.BucketStart), x.Count, x.Wins, x.AvgGain, x.BucketStart)),
            _ => throw new ArgumentException("The statistics response does not match the query.")
        };
        rows = rows.Where(x => x.Games > 0);
        if (ReturnMode == "Series") return rows.OrderBy(x => x.SortKey).ToArray();
        double Key(StatsAnswerRow row) => Metric == "Winrate" ? row.Winrate : row.Performance;
        return (Order == "Ascending" ? rows.OrderBy(Key) : rows.OrderByDescending(Key))
            .ThenBy(x => x.SortKey).ThenBy(x => x.SecondarySortKey).Take(Take).ToArray();
    }

    public static string BucketLabel(int start) => start == 35 ? "35+ min" : $"{start}–{start + 3} min";

    public string Describe()
    {
        var subject = QueryType switch
        {
            "Winrate" => Commander == "None" ? "All commanders" : $"Opponents of {Commander}",
            "Synergy" => Commander == "None" ? "All commander pairs" : $"Teammates of {Commander}",
            _ => $"{Commander} by game duration"
        };
        return $"{QueryType} · Commanders · {Data.GetTimePeriodInfo(Enum.Parse<TimePeriod>(Period)).Name} · {subject} · no leavers · " +
            $"rating {RatingFrom}–{RatingTo}" + (BalancedTeams ? " · balanced teams (40–60%)" : "") +
            (LongGames ? " · games ≥15 minutes" : "") +
            (ReturnMode == "Series" ? " · chronological series" : $" · {(Order == "Ascending" ? "bottom" : "top")} {Take}") +
            $" · {(Metric == "Winrate" ? "raw winrate" : "average rating gain")}";
    }
}

public sealed record StatsAnswerRow(string Label, int Games, int Wins, double Performance, int SortKey, int SecondarySortKey = 0)
{
    public double Winrate => Games > 0 ? Wins / (double)Games : 0;
}
