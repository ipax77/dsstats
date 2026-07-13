using System.Globalization;
using System.Text;

namespace dsstats.discord.cli;

internal static class SqlPatchNotesParser
{
    private const string InsertMarker = "INSERT INTO `DsUpdates` VALUES ";

    public static async Task<List<PatchNoteEntry>> ParseAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"SQL backup not found: {path}", path);
        }

        var sql = await File.ReadAllTextAsync(path, cancellationToken);
        var index = sql.IndexOf(InsertMarker, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new FormatException($"Could not find DsUpdates INSERT statement in {path}.");
        }

        index += InsertMarker.Length;
        var entries = new List<PatchNoteEntry>();
        while (true)
        {
            SkipWhitespace(sql, ref index);
            if (index >= sql.Length || sql[index] == ';')
            {
                break;
            }

            if (sql[index] == ',')
            {
                index++;
                SkipWhitespace(sql, ref index);
            }

            Expect(sql, ref index, '(');
            var legacyId = ReadInt(sql, ref index);
            Expect(sql, ref index, ',');
            var commander = ReadInt(sql, ref index);
            Expect(sql, ref index, ',');
            var timestampText = ReadSqlString(sql, ref index);
            Expect(sql, ref index, ',');
            var discordId = ReadSqlString(sql, ref index);
            Expect(sql, ref index, ',');
            var content = ReadSqlString(sql, ref index);
            Expect(sql, ref index, ')');

            if (!DateTimeOffset.TryParseExact(
                timestampText,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
            {
                throw new FormatException($"Invalid timestamp '{timestampText}' for legacy row {legacyId}.");
            }

            entries.Add(new PatchNoteEntry
            {
                Id = $"sql:{legacyId}",
                Source = "sql",
                Commander = commander,
                TimestampUtc = timestamp,
                SourceMessageId = discordId == "0" ? null : discordId,
                SourceSequence = legacyId,
                Content = content
            });
        }

        if (entries.Count == 0)
        {
            throw new FormatException($"The DsUpdates INSERT statement in {path} contains no rows.");
        }

        return entries;
    }

    private static int ReadInt(string sql, ref int index)
    {
        var start = index;
        while (index < sql.Length && char.IsAsciiDigit(sql[index]))
        {
            index++;
        }

        if (start == index || !int.TryParse(sql.AsSpan(start, index - start), out var value))
        {
            throw Error(sql, index, "integer");
        }

        return value;
    }

    private static string ReadSqlString(string sql, ref int index)
    {
        Expect(sql, ref index, '\'');
        var segmentStart = index;
        StringBuilder? builder = null;
        while (index < sql.Length)
        {
            var current = sql[index];
            if (current == '\'')
            {
                if (index + 1 < sql.Length && sql[index + 1] == '\'')
                {
                    builder ??= new StringBuilder(index - segmentStart + 16);
                    builder.Append(sql, segmentStart, index - segmentStart).Append('\'');
                    index += 2;
                    segmentStart = index;
                    continue;
                }

                var value = builder is null
                    ? sql[segmentStart..index]
                    : builder.Append(sql, segmentStart, index - segmentStart).ToString();
                index++;
                return value;
            }

            if (current == '\\')
            {
                builder ??= new StringBuilder(index - segmentStart + 16);
                builder.Append(sql, segmentStart, index - segmentStart);
                if (++index >= sql.Length)
                {
                    throw Error(sql, index, "escaped character");
                }

                builder.Append(sql[index] switch
                {
                    '0' => '\0',
                    'b' => '\b',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'Z' => '\u001a',
                    var escaped => escaped
                });
                index++;
                segmentStart = index;
                continue;
            }

            index++;
        }

        throw Error(sql, index, "closing quote");
    }

    private static void SkipWhitespace(string sql, ref int index)
    {
        while (index < sql.Length && char.IsWhiteSpace(sql[index]))
        {
            index++;
        }
    }

    private static void Expect(string sql, ref int index, char expected)
    {
        if (index >= sql.Length || sql[index] != expected)
        {
            throw Error(sql, index, $"'{expected}'");
        }

        index++;
    }

    private static FormatException Error(string sql, int index, string expected)
    {
        var previewLength = Math.Min(40, Math.Max(0, sql.Length - index));
        var preview = previewLength == 0 ? "<end>" : sql.Substring(index, previewLength);
        return new FormatException($"Invalid DsUpdates SQL near offset {index}; expected {expected}, found '{preview}'.");
    }
}
