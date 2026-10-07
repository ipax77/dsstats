namespace sc2arcade.crawler;

/// <summary>One production crawl at a time, with a shared server deadline across regions and runs.</summary>
public sealed class Sc2ArcadeRequestGate(TimeProvider? timeProvider = null)
{
    public TimeProvider Clock { get; } = timeProvider ?? TimeProvider.System;
    public SemaphoreSlim RunLock { get; } = new(1, 1);
    public DateTimeOffset NotBefore { get; private set; }

    public void Observe(HttpResponseMessage response) => NotBefore = Sc2ArcadeRequestPolicy.GetNotBefore(
        Sc2ArcadeRequestPolicy.SelectHeaders(response), Clock.GetUtcNow(), response.IsSuccessStatusCode);

    public async Task WaitAsync(CancellationToken token)
    {
        while (NotBefore > Clock.GetUtcNow())
        {
            var remaining = NotBefore - Clock.GetUtcNow();
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : remaining, Clock, token);
        }
    }
}
