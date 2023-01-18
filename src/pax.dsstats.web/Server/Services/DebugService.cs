using pax.dsstats.shared;
using System.Text.Json;

namespace pax.dsstats.web.Server.Services;

public static class DebugService
{
    public static void DebugDupReplayPlayerLastSpawnHash()
    {
        var jsonFile = @"C:\data\ds\replayblobs\73909620-7c41-4030-bcbc-b2ae393b1c7f\20230118-011752.json";

        var replays = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(jsonFile));

        if (replays == null || !replays.Any())
        {
            return;
        }

        foreach (var replay in replays)
        {

        }
    }
}
