using System.Globalization;

namespace sc2arcade.crawler;

public static class Sc2ArcadeRequestPolicy
{
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

}
