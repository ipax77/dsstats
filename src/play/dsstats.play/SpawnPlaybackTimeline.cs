namespace dsstats.play;

internal static class SpawnPlaybackTimeline
{
    public static void BuildPlaybackStops(SpawnPlaybackReplayNg? replay, List<int> playbackStops)
    {
        playbackStops.Clear();
        if (replay is null)
        {
            return;
        }

        int firstGameloop = GetFirstPlaybackGameloop(replay);
        if (replay.Snapshots.Count > 0)
        {
            AddPlaybackStop(playbackStops, firstGameloop);
            foreach (var snapshot in replay.Snapshots)
            {
                AddPlaybackStop(playbackStops, snapshot.EndGameloop);
            }

            AddPlaybackStop(playbackStops, replay.DurationGameloop);
            playbackStops.Sort();
            RemoveDuplicatePlaybackStops(playbackStops);
            return;
        }

        int stepGameloops = Math.Max(1, replay.StepGameloops);
        int fallbackGameloop = firstGameloop;
        while (fallbackGameloop < replay.DurationGameloop)
        {
            playbackStops.Add(fallbackGameloop);
            if (replay.DurationGameloop - fallbackGameloop <= stepGameloops)
            {
                break;
            }

            fallbackGameloop += stepGameloops;
        }

        AddPlaybackStop(playbackStops, firstGameloop);
        AddPlaybackStop(playbackStops, replay.DurationGameloop);
        playbackStops.Sort();
        RemoveDuplicatePlaybackStops(playbackStops);
    }

    public static int ParsePlaybackStopIndex(object? value, int currentIndex, int playbackStopCount)
    {
        if (value is int index)
        {
            return ClampPlaybackStopIndex(index, playbackStopCount);
        }

        return value is string text && int.TryParse(text, out index)
            ? ClampPlaybackStopIndex(index, playbackStopCount)
            : currentIndex;
    }

    public static int FindPlaybackStopIndexAtOrBefore(IReadOnlyList<int> playbackStops, int gameloop)
    {
        int left = 0;
        int right = playbackStops.Count - 1;
        int index = 0;
        while (left <= right)
        {
            int middle = left + (right - left) / 2;
            if (playbackStops[middle] <= gameloop)
            {
                index = middle;
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        return index;
    }

    public static string FormatGameloopSeconds(int gameloop)
    {
        return $"{Math.Round(gameloop / SpawnPlaybackFactoryNg.GameloopsPerSecond, 1):0.0}s";
    }

    public static int GetFirstPlaybackGameloop(SpawnPlaybackReplayNg? replay)
    {
        if (replay is null)
        {
            return 0;
        }

        return replay.Snapshots.Count > 0
            ? replay.Snapshots[0].EndGameloop
            : GetFirstUnitSpawnGameloop(replay);
    }

    public static int ResolveRenderGameloop(SpawnPlaybackReplayNg? replay, int gameloop)
    {
        if (replay is null || replay.Snapshots.Count == 0)
        {
            return gameloop;
        }

        int left = 0;
        int right = replay.Snapshots.Count - 1;
        int candidateIndex = -1;
        while (left <= right)
        {
            int middle = left + (right - left) / 2;
            if (replay.Snapshots[middle].StartGameloop <= gameloop)
            {
                candidateIndex = middle;
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        if (candidateIndex < 0)
        {
            return gameloop;
        }

        int renderGameloop = gameloop;
        for (int i = candidateIndex; i >= 0; i--)
        {
            var snapshot = replay.Snapshots[i];
            if (snapshot.EndGameloop < gameloop)
            {
                break;
            }

            renderGameloop = Math.Max(renderGameloop, snapshot.EndGameloop);
        }

        return renderGameloop;
    }

    private static void AddPlaybackStop(List<int> playbackStops, int gameloop)
    {
        if (playbackStops.Count == 0 || playbackStops[^1] != gameloop)
        {
            playbackStops.Add(gameloop);
        }
    }

    private static void RemoveDuplicatePlaybackStops(List<int> playbackStops)
    {
        if (playbackStops.Count < 2)
        {
            return;
        }

        int writeIndex = 1;
        int previous = playbackStops[0];
        for (int readIndex = 1; readIndex < playbackStops.Count; readIndex++)
        {
            int current = playbackStops[readIndex];
            if (current == previous)
            {
                continue;
            }

            playbackStops[writeIndex] = current;
            previous = current;
            writeIndex++;
        }

        if (writeIndex < playbackStops.Count)
        {
            playbackStops.RemoveRange(writeIndex, playbackStops.Count - writeIndex);
        }
    }

    private static int ClampPlaybackStopIndex(int index, int playbackStopCount)
    {
        if (index < 0 || playbackStopCount <= 0)
        {
            return 0;
        }

        int maxIndex = playbackStopCount - 1;
        return index > maxIndex ? maxIndex : index;
    }

    private static int GetFirstUnitSpawnGameloop(SpawnPlaybackReplayNg replay)
    {
        var unitRows = SpawnPlaybackBinaryPayloads.GetPayload(replay, SpawnPlaybackBinaryPayloads.UnitRowsDatasetId);
        return unitRows is null || unitRows.Count == 0
            ? 0
            : SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitRows.Bytes, 0, SpawnPlaybackBinaryPayloads.UnitRowSpawnGameloopOffset);
    }
}
