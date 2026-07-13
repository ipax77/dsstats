using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dsstats.discord.cli;

internal sealed class DiscordApiClient(HttpClient httpClient, DiscordSettings settings)
{
    private const int PageSize = 100;
    private const int MaxRateLimitRetries = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static HttpClient CreateHttpClient(string token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://discord.com/api/v10/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("dsstats.discord.cli/1.0");
        return client;
    }

    public async Task ValidateChannelAsync(CancellationToken cancellationToken)
    {
        var channel = await GetAsync<DiscordChannel>($"channels/{settings.ChannelId}", cancellationToken)
            ?? throw new DiscordApiException("Discord returned an empty channel response.");
        if (!string.Equals(channel.GuildId, settings.ServerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Discord channel {settings.ChannelId} does not belong to configured server {settings.ServerId}.");
        }
    }

    public async Task<DiscordMessageBatch> GetMessagesAsync(
        string? afterMessageId,
        CancellationToken cancellationToken)
    {
        var result = new List<PatchNoteEntry>();
        var seenIds = new HashSet<ulong>();
        ulong lastSeenId;
        if (afterMessageId is not null)
        {
            var after = ParseId(afterMessageId);
            lastSeenId = await GetNewerMessagesAsync(after, result, seenIds, cancellationToken);
        }
        else
        {
            lastSeenId = await GetAllMessagesAsync(result, seenIds, cancellationToken);
        }

        result.Sort(static (left, right) =>
            ParseId(left.SourceMessageId!).CompareTo(ParseId(right.SourceMessageId!)));
        return new(lastSeenId == 0 ? afterMessageId : lastSeenId.ToString(CultureInfo.InvariantCulture), result);
    }

    private async Task<ulong> GetNewerMessagesAsync(
        ulong after,
        List<PatchNoteEntry> result,
        HashSet<ulong> seenIds,
        CancellationToken cancellationToken)
    {
        var cursor = after;
        while (true)
        {
            var page = await GetMessagePageAsync($"after={cursor}", cancellationToken);
            if (page.Count == 0)
            {
                return cursor;
            }

            var pageMaximum = cursor;
            foreach (var message in page)
            {
                var id = ParseId(message.Id);
                if (id > after && seenIds.Add(id) && IsPatchNote(message))
                {
                    result.Add(Map(message));
                }

                pageMaximum = Math.Max(pageMaximum, id);
            }

            if (page.Count < PageSize || pageMaximum <= cursor)
            {
                return pageMaximum;
            }

            cursor = pageMaximum;
        }
    }

    private async Task<ulong> GetAllMessagesAsync(
        List<PatchNoteEntry> result,
        HashSet<ulong> seenIds,
        CancellationToken cancellationToken)
    {
        ulong? before = null;
        ulong maximum = 0;
        while (true)
        {
            var query = before is ulong cursor ? $"before={cursor}" : null;
            var page = await GetMessagePageAsync(query, cancellationToken);
            if (page.Count == 0)
            {
                return maximum;
            }

            var pageMinimum = ulong.MaxValue;
            foreach (var message in page)
            {
                var id = ParseId(message.Id);
                maximum = Math.Max(maximum, id);
                if (seenIds.Add(id) && IsPatchNote(message))
                {
                    result.Add(Map(message));
                }

                pageMinimum = Math.Min(pageMinimum, id);
            }

            if (page.Count < PageSize || pageMinimum == ulong.MaxValue || pageMinimum == before)
            {
                return maximum;
            }

            before = pageMinimum;
        }
    }

    private async Task<List<DiscordMessage>> GetMessagePageAsync(
        string? cursorQuery,
        CancellationToken cancellationToken)
    {
        var separator = cursorQuery is null ? string.Empty : $"&{cursorQuery}";
        return await GetAsync<List<DiscordMessage>>(
            $"channels/{settings.ChannelId}/messages?limit={PageSize}{separator}", cancellationToken) ?? [];
    }

    private async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var response = await httpClient.GetAsync(
                requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRateLimitRetries)
            {
                var delay = await GetRetryDelayAsync(response, cancellationToken);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadErrorAsync(response, cancellationToken);
                throw new DiscordApiException(
                    $"Discord API request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {detail}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
    }

    private static async Task<TimeSpan> GetRetryDelayAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rateLimit = await JsonSerializer.DeserializeAsync<RateLimitResponse>(stream, JsonOptions, cancellationToken);
        return TimeSpan.FromSeconds(Math.Clamp(rateLimit?.RetryAfter ?? 1, 0.05, 60));
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (content.Length > 500)
        {
            content = content[..500];
        }

        return string.IsNullOrWhiteSpace(content) ? "No response body." : content;
    }

    private static ulong ParseId(string value)
    {
        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            throw new DiscordApiException($"Discord returned invalid message ID '{value}'.");
        }

        return id;
    }

    private static bool IsPatchNote(DiscordMessage message) =>
        message.Type == 0 && message.WebhookId is not null;

    private static PatchNoteEntry Map(DiscordMessage message) => new()
    {
        Id = $"discord:{message.Id}",
        Source = "discord",
        Commander = 0,
        TimestampUtc = message.Timestamp.ToUniversalTime(),
        SourceMessageId = message.Id,
        Content = message.Content ?? string.Empty
    };

    private sealed class DiscordChannel
    {
        [JsonPropertyName("guild_id")]
        public string? GuildId { get; init; }
    }

    private sealed class DiscordMessage
    {
        public int Type { get; init; }
        public required string Id { get; init; }

        [JsonPropertyName("webhook_id")]
        public string? WebhookId { get; init; }
        public DateTimeOffset Timestamp { get; init; }

        public string? Content { get; init; }
    }

    private sealed class RateLimitResponse
    {
        [JsonPropertyName("retry_after")]
        public double RetryAfter { get; init; }
    }
}

internal sealed class DiscordApiException(string message) : Exception(message);
