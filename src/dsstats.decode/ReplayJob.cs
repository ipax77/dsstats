
using System.Threading.Channels;

namespace dsstats.decode;

public sealed record ReplayJob(Guid GroupId, string TempFilePath, string OriginalFilename, bool InHouse);

public interface IReplayQueue
{
    ChannelWriter<ReplayJob> Writer { get; }
    ChannelReader<ReplayJob> Reader { get; }

    int QueueLength { get; }
    bool TryEnqueue(ReplayJob job);
}

public class ReplayQueue : IReplayQueue
{
    private readonly Channel<ReplayJob> channel;
    private int queueLength = 0;

    public int QueueLength => queueLength;

    private readonly int maxQueueSize;

    public ReplayQueue()
    {
        maxQueueSize = 10;

        channel = Channel.CreateUnbounded<ReplayJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    public ChannelWriter<ReplayJob> Writer => channel.Writer;
    public ChannelReader<ReplayJob> Reader => channel.Reader;

    public bool TryEnqueue(ReplayJob job)
    {
        // Check threshold
        if (QueueLength >= maxQueueSize)
            return false;

        // Atomically increment
        Interlocked.Increment(ref queueLength);

        // Write without awaiting
        if (!channel.Writer.TryWrite(job))
        {
            // Roll back increment
            Interlocked.Decrement(ref queueLength);
            return false;
        }

        return true;
    }

    // Call this from the consumer
    public void Decrement()
    {
        Interlocked.Decrement(ref queueLength);
    }
}
