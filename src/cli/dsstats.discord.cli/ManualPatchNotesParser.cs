using System.Globalization;
using System.Text;

namespace dsstats.discord.cli;

internal static class ManualPatchNotesParser
{
    private const string HeaderSeparator = " — ";
    private static readonly TimeZoneInfo ManualTimeZone = FindManualTimeZone();

    public static async Task<List<PatchNoteEntry>> ParseAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Manual patch notes not found: {path}", path);
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(text, path);
    }

    internal static List<PatchNoteEntry> Parse(string text, string sourceName = "manual input")
    {
        var entries = new List<PatchNoteEntry>();
        using var reader = new StringReader(text);
        var body = new StringBuilder();
        string? author = null;
        DateTimeOffset timestampUtc = default;
        var sequence = 0;

        while (reader.ReadLine() is { } line)
        {
            if (TryParseHeader(line, out var parsedAuthor, out var parsedTimestampUtc))
            {
                AddEntry(entries, author, timestampUtc, body, sequence++);
                author = parsedAuthor;
                timestampUtc = parsedTimestampUtc;
                continue;
            }

            if (author is null && !string.IsNullOrWhiteSpace(line))
            {
                throw new FormatException($"Unexpected content before the first manual message header in {sourceName}.");
            }

            if (author is not null)
            {
                body.Append(line).Append('\n');
            }
        }

        AddEntry(entries, author, timestampUtc, body, sequence);
        if (entries.Count == 0)
        {
            throw new FormatException($"No manual Discord messages found in {sourceName}.");
        }

        return entries;
    }

    private static bool TryParseHeader(
        string line,
        out string author,
        out DateTimeOffset timestampUtc)
    {
        author = string.Empty;
        timestampUtc = default;
        var separatorIndex = line.IndexOf(HeaderSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var timestampText = line[(separatorIndex + HeaderSeparator.Length)..];
        if (!DateTime.TryParseExact(
            timestampText,
            "dd/MM/yyyy, HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localTime))
        {
            return false;
        }

        author = line[..separatorIndex].Trim();
        if (author.Length == 0 || ManualTimeZone.IsInvalidTime(localTime))
        {
            throw new FormatException($"Invalid manual message header: {line}");
        }

        timestampUtc = new DateTimeOffset(localTime, ManualTimeZone.GetUtcOffset(localTime)).ToUniversalTime();
        return true;
    }

    private static void AddEntry(
        List<PatchNoteEntry> entries,
        string? author,
        DateTimeOffset timestampUtc,
        StringBuilder body,
        int sequence)
    {
        if (author is null)
        {
            return;
        }

        var content = body.ToString().Trim();
        body.Clear();
        if (content.Length == 0)
        {
            throw new FormatException($"Manual message by {author} at {timestampUtc:O} has no content.");
        }

        entries.Add(new PatchNoteEntry
        {
            Id = $"manual:{timestampUtc.ToUnixTimeSeconds()}:{sequence}",
            Source = "manual",
            Commander = 0,
            TimestampUtc = timestampUtc,
            SourceSequence = sequence,
            Content = content
        });
    }

    private static TimeZoneInfo FindManualTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
