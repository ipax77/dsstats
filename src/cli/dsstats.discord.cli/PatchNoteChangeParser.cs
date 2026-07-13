using System.Collections.Frozen;
using dsstats.shared;

namespace dsstats.discord.cli;

internal static class PatchNoteChangeParser
{
    private static readonly FrozenDictionary<string, Commander> SectionCommanders =
        new Dictionary<string, Commander>(StringComparer.OrdinalIgnoreCase)
        {
            ["GENERAL"] = Commander.None,
            ["FIXES"] = Commander.None,
            ["CHANGES"] = Commander.None,
            ["TYA"] = Commander.None,
            ["PROTOSS"] = Commander.Protoss,
            ["TERRAN"] = Commander.Terran,
            ["ZERG"] = Commander.Zerg,
            ["ABATHUR"] = Commander.Abathur,
            ["ALARAK"] = Commander.Alarak,
            ["ARTANIS"] = Commander.Artanis,
            ["DEHAKA"] = Commander.Dehaka,
            ["FENIX"] = Commander.Fenix,
            ["HAN & HORNER"] = Commander.Horner,
            ["HAN AND HORNER"] = Commander.Horner,
            ["HAN/HORNER"] = Commander.Horner,
            ["HORNER"] = Commander.Horner,
            ["KARAX"] = Commander.Karax,
            ["KERRIGAN"] = Commander.Kerrigan,
            ["MENGSK"] = Commander.Mengsk,
            ["NOVA"] = Commander.Nova,
            ["RAYNOR"] = Commander.Raynor,
            ["STETMANN"] = Commander.Stetmann,
            ["STUKOV"] = Commander.Stukov,
            ["SWANN"] = Commander.Swann,
            ["TYCHUS"] = Commander.Tychus,
            ["VORAZUN"] = Commander.Vorazun,
            ["ZAGARA"] = Commander.Zagara,
            ["ZERATUL"] = Commander.Zeratul
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> SectionOnlyContent =
        SectionCommanders.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static List<PatchNoteEntry> SplitMessages(IEnumerable<PatchNoteEntry> messages)
    {
        var changes = new List<PatchNoteEntry>();
        foreach (var message in messages)
        {
            SplitMessage(message, changes);
        }

        return changes;
    }

    public static bool IsSectionOnly(string content) =>
        SectionOnlyContent.Contains(NormalizeHeading(content));

    public static string NormalizeChange(string content)
    {
        var value = content.Trim();
        return value.StartsWith("- ", StringComparison.Ordinal) ? value[2..].TrimStart() : value;
    }

    private static void SplitMessage(PatchNoteEntry message, List<PatchNoteEntry> target)
    {
        var commander = Commander.None;
        var sequence = 0;
        foreach (var rawLine in message.Content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line == "```")
            {
                continue;
            }

            var heading = NormalizeHeading(line);
            if (SectionCommanders.TryGetValue(heading, out var sectionCommander))
            {
                commander = sectionCommander;
                continue;
            }

            var content = NormalizeChange(line);
            if (content.Length == 0)
            {
                continue;
            }

            target.Add(new PatchNoteEntry
            {
                Id = $"{message.Id}:{sequence}",
                Source = message.Source,
                Commander = (int)commander,
                TimestampUtc = message.TimestampUtc,
                SourceMessageId = message.SourceMessageId,
                SourceSequence = sequence++,
                Content = content
            });
        }
    }

    private static string NormalizeHeading(string content)
    {
        var value = content.Trim();
        if (value.Length >= 4 && value.StartsWith("**", StringComparison.Ordinal)
            && value.EndsWith("**", StringComparison.Ordinal))
        {
            value = value[2..^2].Trim();
        }

        return value.TrimStart('#').Trim().TrimEnd(':').TrimEnd();
    }
}
