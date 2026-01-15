using dsstats.shared;
using System.Threading.Channels;

namespace dsstats.decode;

public abstract class ReplayProducer
{
    protected abstract Task<IReadOnlyCollection<string>> GetReplayPaths();
    protected abstract Task ConsumeReplays(IReadOnlyList<ReplayDto> replays, CancellationToken token);

    public async Task ProduceReplays(int threads, int batchCount, CancellationToken token)
    {
        var channel = Channel.CreateUnbounded<ReplayResult>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
        });

        var replayPaths = await GetReplayPaths();

        var producer = DecodeService.DecodeReplaysToWriter(replayPaths, threads, channel.Writer, token);

        var batch = new List<ReplayDto>(batchCount);

        try
        {
            await foreach (var result in channel.Reader.ReadAllAsync(token))
            {
                if (result.Replay is not null)
                {
                    batch.Add(result.Replay);

                    if (batch.Count >= batchCount)
                    {
                        await ConsumeReplays(batch, token);
                        batch.Clear();
                    }
                }
            }

            await producer;
            channel.Writer.Complete();

        }
        catch (OperationCanceledException)
        {
            await producer;
            channel.Writer.Complete();
        }
        finally
        {
            if (batch.Count > 0)
            {
                await ConsumeReplays(batch, CancellationToken.None);
            }
        }
    }
}

