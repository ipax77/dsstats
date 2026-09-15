using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using dsstats.web.AI;

if (args.Contains("--help"))
{
    Console.WriteLine("--prompt PATH --cases PATH [--case ID] [--endpoint http://localhost:1234/v1/] [--model google/gemma-4-12b] [--repeat 1] [--output report.json]");
    return 0;
}

try
{
    if (args.Length % 2 != 0) throw new ArgumentException("Options require values; use --help.");
    var options = new Dictionary<string, string>();
    var allowed = new[] { "--prompt", "--cases", "--case", "--endpoint", "--model", "--repeat", "--output" };
    for (var i = 0; i < args.Length; i += 2)
    {
        if (!allowed.Contains(args[i]) || !options.TryAdd(args[i], args[i + 1]))
            throw new ArgumentException($"Unknown or duplicate option: {args[i]}");
    }
    var promptPath = options.GetValueOrDefault("--prompt") ?? "src/server/dsstats.web/AI/Prompts/stats/v1.json";
    var casesPath = options.GetValueOrDefault("--cases") ?? "src/tools/dsstats.aiEval/Cases/stats-v1.json";
    var model = options.GetValueOrDefault("--model") ?? "google/gemma-4-12b";
    var endpoint = options.GetValueOrDefault("--endpoint") ?? "http://localhost:1234/v1/";
    var repeat = int.Parse(options.GetValueOrDefault("--repeat") ?? "1");
    if (repeat is < 1 or > 20) throw new ArgumentException("Repeat must be between 1 and 20.");
    var prompt = StatsPrompt.Load(promptPath);
    var suite = JsonSerializer.Deserialize<EvaluationSuite>(File.ReadAllText(casesPath), StatsQueryPlan.JsonOptions)
        ?? throw new InvalidDataException("Empty evaluation suite.");
    if (suite.FormatVersion != 1 || suite.Cases.Length == 0 || suite.Cases.Select(x => x.Id).Distinct().Count() != suite.Cases.Length)
        throw new InvalidDataException("Invalid evaluation suite.");
    foreach (var item in suite.Cases) item.Expected.Validate();
    var cases = options.TryGetValue("--case", out var caseId) ? suite.Cases.Where(x => x.Id == caseId).ToArray() : suite.Cases;
    if (cases.Length == 0) throw new ArgumentException("The selected case ID does not exist.");

    using var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
    using var http = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(3) };
    List<object> results = [];
    var failures = 0;
    for (var run = 1; run <= repeat && !cancel.IsCancellationRequested; run++)
    {
        foreach (var item in cases)
        {
            if (cancel.IsCancellationRequested) break;
            var clock = Stopwatch.StartNew();
            string? raw = null;
            string? rawResponse = null;
            string? error = null;
            StatsQueryPlan? actual = null;
            var expected = item.Expected;
            string[] differences = [];
            try
            {
                var body = new
                {
                    model,
                    messages = prompt.Messages(item.Question).Select(x => new { role = x.Role, content = x.Content }),
                    temperature = 0,
                    max_tokens = 4096,
                    stream = false,
                    response_format = new { type = "json_schema", json_schema = new { name = "stats_query", strict = true, schema = prompt.ResponseSchema } }
                };
                using var response = await http.PostAsJsonAsync("chat/completions", body, cancel.Token);
                rawResponse = await response.Content.ReadAsStringAsync(cancel.Token);
                response.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(rawResponse);
                raw = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                actual = StatsQueryPlan.Parse(raw ?? "");
                if (actual.Supported) _ = actual.ToRequest();
                // Unsupported plans are compared by outcome, not their unused default fields.
                if (expected.Supported || actual.Supported)
                    differences = typeof(StatsQueryPlan).GetProperties()
                        .Where(p => !Equals(p.GetValue(expected), p.GetValue(actual)))
                        .Select(p => $"{p.Name}: expected {p.GetValue(expected)}, got {p.GetValue(actual)}").ToArray();
            }
            catch (Exception ex) { error = ex.Message; }
            var passed = error is null && differences.Length == 0;
            if (!passed) failures++;
            Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {item.Id} ({clock.ElapsedMilliseconds} ms) {error ?? string.Join("; ", differences)}");
            results.Add(new { item.Id, item.Question, run, passed, elapsedMs = clock.ElapsedMilliseconds,
                expected, actual, differences, error, raw, rawResponse });
        }
    }
    var report = JsonSerializer.Serialize(new { model, endpoint, prompt.PromptVersion, prompt.Hash, contract = "stats",
        systemPromptCharacters = prompt.Messages("x")[0].Content.Length, promptCharacters = prompt.Messages("x").Sum(x => x.Content.Length) - 1,
        promptPath, casesPath, utc = DateTime.UtcNow, cancelled = cancel.IsCancellationRequested, failures, results },
        new JsonSerializerOptions(StatsQueryPlan.JsonOptions) { WriteIndented = true });
    if (options.TryGetValue("--output", out var output)) await File.WriteAllTextAsync(output, report);
    else Console.WriteLine(report);
    return failures == 0 && !cancel.IsCancellationRequested ? 0 : 1;
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }

sealed record EvaluationSuite(int FormatVersion, EvaluationCase[] Cases);
sealed record EvaluationCase(string Id, string Question, StatsQueryPlan Expected);
