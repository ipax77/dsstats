using dsstats.parser;
using dsstats.shared;
using s2protocol.NET;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dsstats.pwa.Workers;

/// <summary>
/// [JSExport] entry point called by ReplayDecodeWorker.razor.js.
/// One static instance per Web Worker — no shared state, no locks needed.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class ReplayDecodeWorker
{
    private static readonly ReplayDecoder _decoder = new();
    private static readonly ReplayDecoderOptions _opts = new()
    {
        Initdata        = true,
        Details         = true,
        Metadata        = true,
        GameEvents      = false,
        MessageEvents   = true,
        TrackerEvents   = true,
        AttributeEvents = false,
    };

    [JSExport]
    public static async Task<string> DecodeReplayAsync(byte[] replayBytes)
    {
        try
        {
            using var ms = new MemoryStream(replayBytes, writable: false);
            var sc2Replay = await _decoder.DecodeAsync(ms, _opts, CancellationToken.None);
            if (sc2Replay is null)
                return Fail("ReplayDecoder returned null.");

            var replay = DsstatsParser.ParseReplay(sc2Replay, compat: true);
            var hash   = replay.ComputeHash();

            var replayJson = JsonSerializer.Serialize(replay,
                WorkerSerializerContext.Default.ReplayDto);

            return JsonSerializer.Serialize(
                new WorkerDecodeResult { Success = true, Hash = hash, ReplayJson = replayJson },
                WorkerSerializerContext.Default.WorkerDecodeResult);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }

        static string Fail(string error) =>
            JsonSerializer.Serialize(
                new WorkerDecodeResult { Success = false, Error = error },
                WorkerSerializerContext.Default.WorkerDecodeResult);
    }
}

/// <summary>Result envelope returned by the worker as a JSON string.</summary>
public sealed class WorkerDecodeResult
{
    public bool    Success    { get; set; }
    public string? Hash       { get; set; }
    /// <summary>JSON-serialized ReplayDto. Set only when Success is true.</summary>
    public string? ReplayJson { get; set; }
    public string? Error      { get; set; }
}

/// <summary>
/// AOT-safe source-generated JSON context.
/// Required because RunAOTCompilation=true strips reflection metadata.
/// Must cover every type touched by the worker's serialization path.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WorkerDecodeResult))]
[JsonSerializable(typeof(ReplayDto))]
[JsonSerializable(typeof(ReplayPlayerDto))]
[JsonSerializable(typeof(SpawnDto))]
[JsonSerializable(typeof(UnitDto))]
[JsonSerializable(typeof(UpgradeDto))]
[JsonSerializable(typeof(PlayerDto))]
[JsonSerializable(typeof(ToonIdDto))]
internal partial class WorkerSerializerContext : JsonSerializerContext { }
