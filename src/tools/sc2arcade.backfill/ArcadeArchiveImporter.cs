using dsstats.db;
using dsstats.shared;
using dsstats.shared.Arcade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data.Common;

namespace sc2arcade.backfill;

public record ImportBatchResult(int Existing, int Inserted, int Missing, int NewPlayers);

/// <summary>Bounded, transactional imports; the database's natural keys are the resume checkpoint.</summary>
public sealed class ArcadeArchiveImporter(IDbContextFactory<DsstatsContext> factory)
{
    public async Task<ImportBatchResult> ImportAsync(IReadOnlyList<ArcadeReplayDto> batch, bool execute,
        CancellationToken token = default)
    {
        var distinct = batch.DistinctBy(r => r.GetKey()).ToList();
        await using var context = await factory.CreateDbContextAsync(token);
        var existing = new HashSet<ArcadeReplayKey>();
        foreach (var group in distinct.GroupBy(r => new { r.RegionId, r.BnetBucketId }))
        {
            var records = group.Select(r => r.BnetRecordId).ToArray();
            var ids = await context.ArcadeReplays.AsNoTracking()
                .Where(r => r.RegionId == group.Key.RegionId && r.BnetBucketId == group.Key.BnetBucketId
                    && records.Contains(r.BnetRecordId))
                .Select(r => r.BnetRecordId).ToListAsync(token);
            foreach (var id in ids) existing.Add(new(group.Key.RegionId, group.Key.BnetBucketId, id));
        }
        var missing = distinct.Where(r => !existing.Contains(r.GetKey())).ToList();
        if (!execute || missing.Count == 0) return new(existing.Count, 0, missing.Count, 0);

        // A fresh context per batch means rollback cannot leave a poisoned in-memory key cache.
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var names = missing.SelectMany(r => r.Players).GroupBy(p =>
            new ToonIdRec(p.Player.ToonId.Region, p.Player.ToonId.Realm, p.Player.ToonId.Id))
            .ToDictionary(g => g.Key, g => g.First().Player.Name);
        var players = new Dictionary<ToonIdRec, PlayerInfo>();
        foreach (var group in names.Keys.GroupBy(t => new { t.Region, t.Realm }))
        {
            var ids = group.Select(t => t.Id).ToArray();
            var found = await context.Players.AsNoTracking().Where(p => p.ToonId.Region == group.Key.Region
                && p.ToonId.Realm == group.Key.Realm && ids.Contains(p.ToonId.Id)).ToListAsync(token);
            foreach (var p in found) players.Add(new(p.ToonId.Region, p.ToonId.Realm, p.ToonId.Id), new(p.PlayerId, p.Name));
        }
        var added = names.Where(p => !players.ContainsKey(p.Key)).ToList();
        var playerType = context.Model.FindEntityType(typeof(Player))!;
        var toonType = playerType.FindNavigation(nameof(Player.ToonId))!.TargetEntityType;
        var playerTable = StoreObjectIdentifier.Table(playerType.GetTableName()!, playerType.GetSchema());
        string ToonColumn(string property) => toonType.FindProperty(property)!.GetColumnName(playerTable)!;
        await InsertAsync(context, playerTable.Name,
            [nameof(Player.Name), ToonColumn(nameof(ToonId.Region)), ToonColumn(nameof(ToonId.Realm)), ToonColumn(nameof(ToonId.Id))],
            added.Select(p => new object?[] { p.Value, p.Key.Region, p.Key.Realm, p.Key.Id }), token);
        foreach (var group in added.GroupBy(p => new { p.Key.Region, p.Key.Realm }))
        {
            var ids = group.Select(p => p.Key.Id).ToArray();
            var found = await context.Players.AsNoTracking().Where(p => p.ToonId.Region == group.Key.Region
                && p.ToonId.Realm == group.Key.Realm && ids.Contains(p.ToonId.Id)).ToListAsync(token);
            foreach (var p in found) players.Add(new(p.ToonId.Region, p.ToonId.Realm, p.ToonId.Id), new(p.PlayerId, p.Name));
        }
        var importedAt = DateTime.UtcNow;
        await InsertAsync(context, "ArcadeReplays",
            ["RegionId", "BnetBucketId", "BnetRecordId", "GameMode", "CreatedAt", "Duration", "PlayerCount", "WinnerTeam", "Imported"],
            missing.Select(r => new object?[] { r.RegionId, r.BnetBucketId, r.BnetRecordId, (int)r.GameMode, r.CreatedAt,
                r.Duration, r.PlayerCount, r.WinnerTeam, importedAt }), token);
        var replayIds = new Dictionary<ArcadeReplayKey, int>();
        foreach (var group in missing.GroupBy(r => new { r.RegionId, r.BnetBucketId }))
        {
            var ids = group.Select(r => r.BnetRecordId).ToArray();
            var found = await context.ArcadeReplays.AsNoTracking().Where(r => r.RegionId == group.Key.RegionId
                && r.BnetBucketId == group.Key.BnetBucketId && ids.Contains(r.BnetRecordId))
                .Select(r => new { r.BnetRecordId, r.ArcadeReplayId }).ToListAsync(token);
            foreach (var r in found) replayIds.Add(new(group.Key.RegionId, group.Key.BnetBucketId, r.BnetRecordId), r.ArcadeReplayId);
        }
        await InsertAsync(context, "ArcadeReplayPlayers", ["SlotNumber", "Team", "ArcadeReplayId", "PlayerId"],
            missing.SelectMany(r => r.Players.Select(p => new object?[] { p.SlotNumber, p.Team, replayIds[r.GetKey()],
                players[new(p.Player.ToonId.Region, p.Player.ToonId.Realm, p.Player.ToonId.Id)].PlayerId })), token);
        await transaction.CommitAsync(token);
        return new(existing.Count, missing.Count, 0, added.Count);
    }

    private static async Task InsertAsync(DsstatsContext context, string table, string[] columns,
        IEnumerable<object?[]> rows, CancellationToken token)
    {
        // Names come only from our model/constants; every data value is a provider parameter.
        foreach (var chunk in rows.Chunk(500))
        {
            using var command = context.Database.GetDbConnection().CreateCommand();
            List<DbParameter> parameters = [];
            var values = chunk.Select(row => "(" + string.Join(",", row.Select(value =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p" + parameters.Count;
                parameter.Value = value ?? DBNull.Value;
                parameters.Add(parameter);
                return parameter.ParameterName;
            })) + ")").ToArray();
            var sql = $"INSERT INTO `{table}` ({string.Join(",", columns.Select(c => $"`{c}`"))}) VALUES {string.Join(",", values)}";
            await context.Database.ExecuteSqlRawAsync(sql, parameters.Cast<object>(), token);
        }
    }
}
