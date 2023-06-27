using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared.Services;
using System.Text.Json;

namespace pax.dsstats.dbng.Services;

public class DsUpdateService : IDsUpdateService
{
    private readonly ReplayContext context;

    public DsUpdateService(ReplayContext context)
    {
        this.context = context;
    }

    public void SeedDsUpdates()
    {
        var jsonfile = "/data/ds/patch-notes.json";

        if (!File.Exists(jsonfile))
        {
            return;
        }

        var jsonObject = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(jsonfile));

        var discordChannel = new DiscordChannel();
        var messages = jsonObject.GetProperty("messages");

        foreach (var message in messages.EnumerateArray())
        {
            var id = message.GetProperty("id").GetString();
            var timestamp = message.GetProperty("timestamp").GetDateTime();
            var content = message.GetProperty("content").GetString();

            discordChannel.Messages.Add(new()
            {
                Id = id ?? "",
                Timestamp = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day),
                Content = content ?? ""
            });
        }

        var updates = new List<DsUpdateInfo>();

        foreach (var message in discordChannel.Messages)
        {
            var lines = message.Content.Split('\n');
            Commander cmdr = Commander.None;
            foreach (var line in lines)
            {
                var content = line.Trim();
                content = content.Replace("`", "");
                if (content.Length == 0)
                {
                    continue;
                }

                if (Enum.TryParse(typeof(Commander), content, true, out var lineCmdrObj)
                    && lineCmdrObj is Commander lineCmdr)
                {
                    cmdr = lineCmdr;
                }
                else if (content.Equals("han & horner", StringComparison.OrdinalIgnoreCase))
                {
                    cmdr = Commander.Horner;
                }
                else if (content.Equals("stettman", StringComparison.OrdinalIgnoreCase))
                {
                    cmdr = Commander.Stetmann;
                }
                else
                {
                    var info = updates.FirstOrDefault(f => f.Time == message.Timestamp && f.Commander == cmdr);
                    if (info == null)
                    {
                        info = new()
                        {
                            Time = message.Timestamp,
                            Commander = cmdr,
                            Id = message.Id
                        };
                        updates.Add(info);
                    }
                    info.Changes.Add(line);
                }
            }
        }

        var existingDiscordIds = context.DsUpdates
            .Select(s => s.DiscordId)
            .Distinct()
            .ToList();

        foreach (var update in updates)
        {
            if (existingDiscordIds.Contains(update.Id))
            {
                continue;
            }

            foreach (var change in update.Changes)
            {
                DsUpdate dbUpdate = new()
                {
                    Commander = update.Commander,
                    Time = update.Time,
                    DiscordId = update.Id,
                    Change = change
                };
                context.DsUpdates.Add(dbUpdate);
            }
        }
        context.SaveChanges();
    }

    public async Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(timePeriod);

        var dsUpdates = await context.DsUpdates
            .Where(x => x.Time >= fromDate)
            .Select(s => new
            {
                s.Commander,
                s.Time,
                s.Change
            })
            .ToListAsync(token);

        List<DsUpdateInfo> infos = new();

        foreach (var update in dsUpdates)
        {
            var info = infos.FirstOrDefault(f => f.Commander == update.Commander && f.Time == update.Time);

            if (info == null)
            {
                info = new() { Commander = update.Commander, Time = update.Time };
                infos.Add(info);
            }
            info.Changes.Add(update.Change);
        }

        return infos;
    }
}

