using dsstats.parser;
using dsstats.shared;
using s2protocol.NET;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace dsstats.decode;

public static class DecodeService
{
    private static readonly ReplayDecoder replayDecoder = new();
    private static readonly ReplayDecoderOptions replayDecoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    public static async Task<ReplayDto?> DecodeReplay(string replayPath, CancellationToken token = default)
    {
        if (!File.Exists(replayPath))
        {
            throw new ArgumentNullException(nameof(replayPath), "Replay not found.");
        }
        var replayDto = await DecodeReplay(new FileStream(replayPath, FileMode.Open, FileAccess.Read, FileShare.Read), token);
        if (replayDto is not null)
        {
            replayDto.FileName = replayPath;
        }
        return replayDto;
    }

    public static async Task<ReplayDto?> DecodeReplay(Stream stream, CancellationToken token = default)
    {
        var sc2Replay = await replayDecoder.DecodeAsync(stream, replayDecoderOptions, token);
        if (sc2Replay is null)
        {
            return null;
        }
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        if (replayDto is null)
        {
            return null;
        }
        return replayDto;
    }

    public static async IAsyncEnumerable<ReplayResult> DecodeReplays(
        ICollection<string> replayPaths,
        int threads,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var channel = Channel.CreateBounded<ReplayResult>(new BoundedChannelOptions(threads)
        {
            SingleWriter = false,
            SingleReader = true
        });

        var writer = Task.Run(async () =>
        {
            try
            {
                await Parallel.ForEachAsync(replayPaths, new ParallelOptions
                {
                    MaxDegreeOfParallelism = threads,
                    CancellationToken = token
                },
                async (replayPath, ct) =>
                {
                    try
                    {
                        var sc2Replay = await replayDecoder.DecodeAsync(replayPath, replayDecoderOptions, ct);
                        if (sc2Replay is null)
                        {
                            await channel.Writer.WriteAsync(new(null, "Failed decoding replay."), ct);
                            return;
                        }

                        var replayDto = DsstatsParser.ParseReplay(sc2Replay, false);
                        if (replayDto is null)
                        {
                            await channel.Writer.WriteAsync(new(null, "Failed parsing replay."), ct);
                            return;
                        }

                        await channel.Writer.WriteAsync(new(replayDto, null), ct);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        await channel.Writer.WriteAsync(new(null, ex.Message), ct);
                    }
                });
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, token);

        await foreach (var item in channel.Reader.ReadAllAsync(token))
            yield return item;

        await writer;
    }
}

public sealed record ReplayResult(ReplayDto? Replay, string? Error);