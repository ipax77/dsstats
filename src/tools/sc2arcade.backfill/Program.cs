using System.Globalization;
using System.Text.Json;
using sc2arcade.backfill;

if (args.Length == 0 || args[0] is "--help" or "help")
{
    Console.WriteLine("""
        SC2Arcade diagnostic backfill (no database)
          crawl  --archive <directory> --cutoff 2026-05-31T03:31:59Z
          resume --archive <directory> [--cutoff <same UTC cutoff>]
          status --archive <directory>
        The first unsuccessful response stops ALL regions. Resume is an explicit new attempt.
        Disable production crawling before crawl/resume. See README.md.
        """);
    return 0;
}

try
{
    string command = args[0];
    if (command is not ("crawl" or "resume" or "status")) throw new ArgumentException("Unknown command.");
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (int i = 1; i < args.Length; i += 2)
    {
        if (i + 1 >= args.Length || args[i] is not ("--archive" or "--cutoff") || !options.TryAdd(args[i], args[i + 1]))
            throw new ArgumentException("Expected unique --archive and optional --cutoff options.");
    }
    if (!options.TryGetValue("--archive", out var directory) || string.IsNullOrWhiteSpace(directory))
        throw new ArgumentException("--archive is required.");
    DateTimeOffset? cutoff = null;
    if (options.TryGetValue("--cutoff", out var text))
    {
        if (!(text.EndsWith('Z') || text.EndsWith("+00:00", StringComparison.Ordinal))
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            throw new ArgumentException("--cutoff must be an explicit UTC timestamp ending in Z or +00:00.");
        cutoff = parsed;
    }
    if (command == "crawl" && cutoff is null) throw new ArgumentException("crawl requires --cutoff.");
    using var archive = command == "crawl"
        ? Archive.Create(directory, ArchiveConfiguration.Create(DateTimeOffset.UtcNow, cutoff!.Value))
        : Archive.Open(directory, write: command == "resume");
    if (cutoff is not null && cutoff != archive.Configuration.Cutoff)
        throw new ArgumentException("Cutoff differs from the existing archive. Use a new directory for a new run.");
    BackfillState state;
    if (command == "status") state = BackfillState.Read(archive);
    else
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "dsstats-crawler/1.0");
        state = await new BackfillRunner(client, log: Console.WriteLine).RunAsync(archive, cancellation.Token);
    }
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        state.Configuration, state.StopReason, state.NotBefore, state.Regions,
        OutstandingRequest = state.Active is { } active ? new { active.RegionId, active.MapId, active.Cursor } : null,
        PreviousSuccessfulResponse = Diagnostic(state.PreviousSuccessfulResponse),
        LastResponse = Diagnostic(state.LastResponse),
        Note = "DuplicateKeys counts eligible replay keys repeated within this archive; no production comparison. HistoryExhaustedBeforeCutoff means limited coverage."
    }, Archive.Json));
    return command == "status" || state.Active is null ? 0 : state.StopReason == "Cancelled" ? 130 : 2;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine($"Stopped: {ex.Message}. Committed records are retained; inspect the archive before resuming.");
    return 1;
}

static object? Diagnostic(RequestRecord? record) => record is null ? null : new
{
    record.Kind, record.RegionId, record.MapId, record.Cursor, record.RequestStartedAt,
    ReceivedAt = record.At, ElapsedMs = (record.At - record.RequestStartedAt)?.TotalMilliseconds,
    record.StatusCode, record.Headers, record.NotBefore, record.Outcome,
    BodySample = record.Outcome == "Page" ? null : record.Body, record.BodyTruncated
};
