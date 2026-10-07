using System.Globalization;
using System.Net;
using System.Text;

namespace sc2arcade.backfill;

public sealed class BackfillRunner(HttpClient client, TimeProvider? timeProvider = null, Action<string>? log = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private const int SampleLimit = 4096;

    public async Task<BackfillState> RunAsync(Archive archive, CancellationToken token = default)
    {
        var state = BackfillState.Read(archive);
        try
        {
            while (state.Active is { } region)
            {
                if (state.NotBefore > clock.GetUtcNow())
                    log?.Invoke($"Waiting until {state.NotBefore:O}; region={region.RegionId}, cursor={region.Cursor ?? "(first page)"}");
                while (state.NotBefore > clock.GetUtcNow())
                {
                    var remaining = state.NotBefore - clock.GetUtcNow();
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : remaining, clock, token);
                }
                token.ThrowIfCancellationRequested();
                var started = new RequestRecord
                {
                    Kind = "Started", At = clock.GetUtcNow(), RegionId = region.RegionId,
                    MapId = region.MapId, Cursor = region.Cursor,
                    // If killed with an unknown response, avoid an immediate replay on restart.
                    NotBefore = clock.GetUtcNow().AddSeconds(60)
                };
                Commit(started);
                RequestRecord? headers = null;
                RequestRecord response;
                try
                {
                    using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    requestTimeout.CancelAfter(TimeSpan.FromSeconds(60));
                    var requestToken = requestTimeout.Token;
                    using var message = await client.GetAsync(BuildUri(archive.Configuration, region),
                        HttpCompletionOption.ResponseHeadersRead, requestToken);
                    var received = clock.GetUtcNow();
                    var selected = SelectHeaders(message);
                    headers = started with
                    {
                        Kind = "Headers", At = received, RequestStartedAt = started.At,
                        StatusCode = (int)message.StatusCode, Headers = selected,
                        NotBefore = GetNotBefore(selected, received, message.IsSuccessStatusCode),
                        Outcome = message.IsSuccessStatusCode ? "ReadingResponse" : $"Http{(int)message.StatusCode}"
                    };
                    // Persist server waits before reading a possibly slow or interrupted body.
                    Commit(headers);
                    if (!message.IsSuccessStatusCode)
                    {
                        var sample = await ReadSample(message.Content, requestToken);
                        response = headers with { Kind = "Response", Body = sample.Body, BodyTruncated = sample.Truncated };
                    }
                    else
                    {
                        string body = await message.Content.ReadAsStringAsync(requestToken);
                        string outcome = "Page";
                        string? next = null;
                        try
                        {
                            if (body.TrimStart().StartsWith('<')) outcome = "HtmlChallenge";
                            else
                            {
                                var page = BackfillState.ParsePage(body, region.RegionId);
                                next = page.Page.Next;
                                if (BackfillState.IsRepeatedCursor(region, next)) outcome = "RepeatedCursor";
                            }
                        }
                        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or OverflowException or ArgumentException)
                        {
                            outcome = "InvalidResponse";
                        }
                        response = headers with
                        {
                            Kind = "Response", Outcome = outcome, Next = next,
                            Body = outcome == "Page" ? body : body[..Math.Min(body.Length, SampleLimit)],
                            BodyTruncated = outcome != "Page" && body.Length > SampleLimit
                        };
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                {
                    response = (headers ?? started) with
                    {
                        Kind = "Response", RequestStartedAt = started.At, At = clock.GetUtcNow(),
                        Outcome = ex is OperationCanceledException ? "RequestTimeout" : "NetworkFailure"
                    };
                }
                response = response with { BodySha256 = response.Body is null ? null : Archive.Hash(response.Body) };
                Commit(response);
                log?.Invoke($"{response.Outcome}: region={region.RegionId}, pages={region.Pages}, lobbies={region.Lobbies}, oldest={region.Oldest:O}, next={region.Cursor ?? "(none)"}");
                if (response.Outcome != "Page") return state;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Commit(new() { Kind = "Stopped", At = clock.GetUtcNow(), Outcome = "Cancelled" });
        }
        return state;

        void Commit(RequestRecord record)
        {
            archive.Append(record);
            state.Apply(record);
        }
    }

    public static string BuildUri(ArchiveConfiguration configuration, RegionProgress region)
    {
        string path = $"lobbies/history?regionId={region.RegionId}&mapId={region.MapId}"
            + $"&profileHandle={Uri.EscapeDataString(configuration.ProfileHandle)}&orderDirection=desc"
            + "&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true"
            + $"&limit={configuration.PageSize}";
        if (region.Cursor != null) path += "&after=" + Uri.EscapeDataString(region.Cursor);
        return new Uri(new Uri(configuration.ApiBase), path).AbsoluteUri;
    }

    public static Dictionary<string, string> SelectHeaders(HttpResponseMessage response) =>
        response.Headers.Concat(response.Content.Headers)
            .Where(h => h.Key.Equals("Retry-After", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("Date", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("cf-ray", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("cf-mitigated", StringComparison.OrdinalIgnoreCase)
                || h.Key.Contains("ratelimit", StringComparison.OrdinalIgnoreCase)
                || h.Key.Contains("rate-limit", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);

    public static DateTimeOffset GetNotBefore(Dictionary<string, string> headers, DateTimeOffset now, bool success)
    {
        var lookup = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        var deadline = now.AddSeconds(3);
        if (lookup.TryGetValue("Retry-After", out var retry))
        {
            if (long.TryParse(retry, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                Extend(seconds);
            else if (DateTimeOffset.TryParse(retry, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                if (date > deadline) deadline = date;
                if (lookup.TryGetValue("Date", out var serverDate)
                    && DateTimeOffset.TryParse(serverDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var serverNow))
                    Extend(Math.Max(0, (date - serverNow).TotalSeconds));
            }
            else Extend(60);
        }
        bool hasRemaining = lookup.TryGetValue("x-ratelimit-remaining", out var remainingText)
            && int.TryParse(remainingText, out _);
        int remaining = hasRemaining ? int.Parse(remainingText!, CultureInfo.InvariantCulture) : 0;
        if (!hasRemaining || remaining <= 1 || !success)
        {
            // Matches the existing crawler and observed small reset values: relative seconds.
            if (lookup.TryGetValue("x-ratelimit-reset", out var reset)
                && long.TryParse(reset, NumberStyles.None, CultureInfo.InvariantCulture, out var resetSeconds))
                Extend(resetSeconds);
            else if (success || hasRemaining) Extend(60);
        }
        return deadline;

        void Extend(double seconds)
        {
            // Saturate rather than overflow or shorten an unexpectedly large server deadline.
            var candidate = seconds >= (DateTimeOffset.MaxValue - now).TotalSeconds
                ? DateTimeOffset.MaxValue : now.AddSeconds(seconds);
            if (candidate > deadline) deadline = candidate;
        }
    }

    private static async Task<(string Body, bool Truncated)> ReadSample(HttpContent content, CancellationToken token)
    {
        await using var stream = await content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        char[] buffer = new char[SampleLimit + 1];
        int count = 0;
        while (count < buffer.Length)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(count), token);
            if (read == 0) break;
            count += read;
        }
        return (new string(buffer, 0, Math.Min(count, SampleLimit)), count > SampleLimit);
    }
}
