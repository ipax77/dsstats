using System.Collections.Frozen;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.PatchNotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices;

public sealed class PatchNotesService(
    IDbContextFactory<DsstatsContext> contextFactory,
    IMemoryCache memoryCache) : IPatchNotesService
{
    private const int MaximumPageSize = 100;
    private const int MaximumUnitNameLength = 100;
    private const string UnitNamesCacheKey = "patch_notes_unit_names";
    private const string PatchDatesCacheKey = "patch_notes_dates";
    private static readonly IReadOnlyList<string> EmptyUnitNames = Array.Empty<string>();

    public async Task<PatchNotesPage> GetPatchNotes(PatchNotesRequest request, CancellationToken token = default)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaximumPageSize);
        var page = Math.Clamp(request.Page, 1, int.MaxValue / pageSize);
        var unit = request.Unit?.Trim();
        if (unit is { Length: > MaximumUnitNameLength })
        {
            unit = unit[..MaximumUnitNameLength];
        }

        await using var context = await contextFactory.CreateDbContextAsync(token);
        var query = context.PatchNotes.AsNoTracking();
        if (request.Commander != Commander.None)
        {
            query = query.Where(note => note.Commander == request.Commander);
        }
        if (!string.IsNullOrEmpty(unit))
        {
            query = query.Where(note => note.Content.Contains(unit));
        }

        var orderedQuery = request.Sort == PatchNotesSort.OldestFirst
            ? query.OrderBy(note => note.PublishedAtUtc).ThenBy(note => note.PatchNoteId)
            : query.OrderByDescending(note => note.PublishedAtUtc).ThenByDescending(note => note.PatchNoteId);

        // Fetching one extra row avoids a separate COUNT query.
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(note => new PatchNoteDto
            {
                Id = note.PatchNoteId,
                PublishedAtUtc = note.PublishedAtUtc,
                Commander = note.Commander,
                Content = note.Content
            })
            .ToListAsync(token);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(pageSize);
        }

        return new PatchNotesPage { Items = items, Page = page, HasMore = hasMore };
    }

    public async Task<IReadOnlyList<string>> GetUnitNames(Commander commander, CancellationToken token = default)
    {
        var catalog = await memoryCache.GetOrCreateAsync(UnitNamesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            await using var context = await contextFactory.CreateDbContextAsync(token);
            var units = await context.DsUnits.AsNoTracking()
                .Select(unit => new { unit.Commander, unit.Name })
                .ToListAsync(token);

            var namesByCommander = units.GroupBy(unit => unit.Commander).ToFrozenDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(unit => unit.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray());
            var allNames = namesByCommander.Values.SelectMany(names => names)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new UnitNameCatalog(namesByCommander, allNames);
        });

        if (catalog is null)
        {
            return EmptyUnitNames;
        }
        if (commander != Commander.None)
        {
            return catalog.ByCommander.TryGetValue(commander, out var names) ? names : EmptyUnitNames;
        }

        return catalog.All;
    }

    public async Task<IReadOnlyList<DateTime>> GetPatchDates(int count = 12, CancellationToken token = default)
    {
        count = Math.Clamp(count, 1, 50);
        var dates = await memoryCache.GetOrCreateAsync(PatchDatesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            await using var context = await contextFactory.CreateDbContextAsync(token);
            return await context.PatchNotes.AsNoTracking()
                .Select(note => note.PublishedAtUtc.Date)
                .Distinct()
                .OrderByDescending(date => date)
                .Take(50)
                .ToArrayAsync(token);
        }) ?? [];

        return dates.Take(count).ToArray();
    }

    private sealed record UnitNameCatalog(
        IReadOnlyDictionary<Commander, IReadOnlyList<string>> ByCommander,
        IReadOnlyList<string> All);
}
