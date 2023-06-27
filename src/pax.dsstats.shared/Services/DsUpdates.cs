using System.Text.Json;

namespace pax.dsstats.shared.Services;

public static class DsUpdates
{
    public static void ExtractUpdates()
    {
        string jsonfile = "C:/data/ds/patchnotes/Direct Strike - Info - patch-notes [420630538122952704].json";

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
                else
                {
                    var info = updates.FirstOrDefault(f => f.Time == message.Timestamp && f.Commander == cmdr);
                    if (info == null)
                    {
                        info = new()
                        {
                            Time = message.Timestamp,
                            Commander = cmdr
                        };
                        updates.Add(info);
                    }
                    info.Changes.Add(line);
                }
            }
        }

        Console.WriteLine(updates.Count);
    }
}

public record DsUpdateInfo
{
    public Commander Commander { get; set; }
    public DateTime Time { get; set; }
    public string Id { get; set; } = string.Empty;
    public List<string> Changes { get; set; } = new();
}


public record DiscordChannel
{
    public List<DiscordMessage> Messages { get; set; } = new();
}

public record DiscordMessage
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
}