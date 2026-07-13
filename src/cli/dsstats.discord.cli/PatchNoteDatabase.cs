using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.discord.cli;

internal readonly record struct DatabaseImportResult(
    int Added,
    int Updated,
    int Removed,
    string? DiscordCursor);

internal static class PatchNoteDatabase
{
    public static async Task<DatabaseImportResult> InitializeAsync(
        string sqlPath,
        string manualPath,
        DiscordSettings settings,
        CancellationToken cancellationToken)
    {
        var sqlEntries = await SqlPatchNotesParser.ParseAsync(sqlPath, cancellationToken);
        var manualMessages = await ManualPatchNotesParser.ParseAsync(manualPath, cancellationToken);
        var desired = new List<PatchNote>(sqlEntries.Count + manualMessages.Count * 20);

        foreach (var entry in sqlEntries)
        {
            if (!PatchNoteChangeParser.IsSectionOnly(entry.Content))
            {
                desired.Add(Map(entry, PatchNoteSource.LegacySql));
            }
        }

        foreach (var entry in PatchNoteChangeParser.SplitMessages(manualMessages))
        {
            desired.Add(Map(entry, PatchNoteSource.Manual));
        }

        await using var context = CreateContext(settings.ConnectionString);
        var existing = await context.PatchNotes
            .Where(note => note.Source != PatchNoteSource.Discord)
            .ToDictionaryAsync(note => note.SourceKey, StringComparer.Ordinal, cancellationToken);

        var added = 0;
        var updated = 0;
        foreach (var note in desired)
        {
            if (existing.Remove(note.SourceKey, out var current))
            {
                if (UpdateIfChanged(current, note))
                {
                    updated++;
                }
            }
            else
            {
                context.PatchNotes.Add(note);
                added++;
            }
        }

        var removed = existing.Count;
        context.PatchNotes.RemoveRange(existing.Values);
        await context.SaveChangesAsync(cancellationToken);
        return new(added, updated, removed, null);
    }

    public static async Task<DatabaseImportResult> AddDiscordAsync(
        DiscordApiClient client,
        DiscordSettings settings,
        CancellationToken cancellationToken)
    {
        await using var context = CreateContext(settings.ConnectionString);
        var state = await context.PatchNoteSyncStates
            .SingleOrDefaultAsync(item => item.PatchNoteSyncStateId == PatchNoteSyncState.DiscordStateId,
                cancellationToken);
        if (state is null)
        {
            state = new PatchNoteSyncState
            {
                PatchNoteSyncStateId = PatchNoteSyncState.DiscordStateId,
                UpdatedAtUtc = DateTime.UtcNow
            };
            context.PatchNoteSyncStates.Add(state);
        }

        if (state.Cursor is null)
        {
            await client.ValidateChannelAsync(cancellationToken);
        }

        var batch = await client.GetMessagesAsync(state.Cursor, cancellationToken);
        var changes = PatchNoteChangeParser.SplitMessages(batch.Entries);
        var sourceKeys = changes.Select(entry => entry.Id).ToArray();
        HashSet<string> existingKeys = sourceKeys.Length == 0
            ? []
            : await context.PatchNotes
                .Where(note => sourceKeys.Contains(note.SourceKey))
                .Select(note => note.SourceKey)
                .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);

        var added = 0;
        foreach (var entry in changes)
        {
            if (existingKeys.Add(entry.Id))
            {
                context.PatchNotes.Add(Map(entry, PatchNoteSource.Discord));
                added++;
            }
        }

        state.Cursor = batch.LastMessageId;
        state.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new(added, 0, 0, state.Cursor);
    }

    private static DsstatsContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DsstatsContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 6)), mysql =>
            {
                mysql.MigrationsAssembly("dsstats.migrations.mysql");
                mysql.EnableRetryOnFailure();
            })
            .Options;
        return new DsstatsContext(options);
    }

    private static PatchNote Map(PatchNoteEntry entry, PatchNoteSource source) => new()
    {
        SourceKey = entry.Id,
        Source = source,
        SourceMessageId = entry.SourceMessageId,
        SourceSequence = entry.SourceSequence,
        PublishedAtUtc = entry.TimestampUtc.UtcDateTime,
        Commander = (Commander)entry.Commander,
        Content = PatchNoteChangeParser.NormalizeChange(entry.Content)
    };

    private static bool UpdateIfChanged(PatchNote current, PatchNote desired)
    {
        var changed = current.Source != desired.Source
            || current.SourceMessageId != desired.SourceMessageId
            || current.SourceSequence != desired.SourceSequence
            || current.PublishedAtUtc != desired.PublishedAtUtc
            || current.Commander != desired.Commander
            || current.Content != desired.Content;
        if (!changed)
        {
            return false;
        }

        current.Source = desired.Source;
        current.SourceMessageId = desired.SourceMessageId;
        current.SourceSequence = desired.SourceSequence;
        current.PublishedAtUtc = desired.PublishedAtUtc;
        current.Commander = desired.Commander;
        current.Content = desired.Content;
        return true;
    }
}
