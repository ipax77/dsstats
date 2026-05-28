using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using dsstats.parser;
using dsstats.play;
using dsstats.shared;

BenchmarkRunner.Run<SpawnPlaybackFactoryNgBenchmarks>(args: args);

[MemoryDiagnoser]
public class SpawnPlaybackFactoryNgBenchmarks
{
    private const string StressReplayEnvironmentVariable = "DSSTATS_PLAY_BENCH_STRESS_REPLAY";
    private const string UsualReplayEnvironmentVariable = "DSSTATS_PLAY_BENCH_USUAL_REPLAY";

    private const string DefaultStressReplayPath =
        @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (10265).SC2Replay";

    private const string DefaultUsualReplayPath =
        @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike TE (1932).SC2Replay";

    private BenchmarkFixture stressReplay = null!;
    private BenchmarkFixture usualReplay = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        stressReplay = LoadFixture(
            "stress",
            StressReplayEnvironmentVariable,
            DefaultStressReplayPath);

        usualReplay = LoadFixture(
            "usual",
            UsualReplayEnvironmentVariable,
            DefaultUsualReplayPath);
    }

    [Benchmark]
    public SpawnPlaybackReplayNg UsualReplay_Create()
    {
        return SpawnPlaybackFactoryNg.Create(usualReplay.Replay, usualReplay.Sidecar);
    }

    [Benchmark]
    public SpawnPlaybackReplayNg StressReplay_Create()
    {
        return SpawnPlaybackFactoryNg.Create(stressReplay.Replay, stressReplay.Sidecar);
    }

    private static BenchmarkFixture LoadFixture(
        string name,
        string environmentVariable,
        string defaultPath)
    {
        string path = Environment.GetEnvironmentVariable(environmentVariable) ?? defaultPath;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The {name} replay fixture was not found. Set {environmentVariable} to override the default path.",
                path);
        }

        var sc2Replay = DsstatsParser.GetSc2Replay(path).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException($"The {name} replay fixture could not be decoded: {path}");

        ReplayDto replay = DsstatsParser.ParseReplay(sc2Replay);
        var directStrikeReplay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
        SpawnPlaybackSidecarDto sidecar = SpawnPlaybackSidecarFactory.Create(sc2Replay, directStrikeReplay)
            ?? throw new InvalidOperationException($"The {name} replay fixture is not eligible for spawn playback: {path}");

        return new(replay, sidecar);
    }

    private sealed record BenchmarkFixture(
        ReplayDto Replay,
        SpawnPlaybackSidecarDto Sidecar);
}
