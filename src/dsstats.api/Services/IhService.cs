
using dsstats.shared;
using System.Collections.Concurrent;

namespace dsstats.api.Services;

public partial class IhService(IServiceScopeFactory scopeFactory)
{
    private ConcurrentDictionary<Guid, GroupState> groups = [];

    public GroupState? CreateOrVisitGroup(Guid groupId)
    {
        if (groups.TryGetValue(groupId, out GroupState? groupState)
            && groupState is not null)
        {
            groupState.Visitors++;
        }
        else
        {
            groupState = groups.AddOrUpdate(groupId, new GroupState() { GroupId = groupId, Visitors = 1 },
                (k, v) => v = v with { Visitors = 1 });
        }
        return groupState;
    }

    public GroupState? LeaveGroup(Guid groupId)
    {
        if (groups.TryGetValue(groupId, out GroupState? groupState)
            && groupState is not null)
        {
            groupState.Visitors--;
            return groupState;
        }
        return null;
    }

    public List<IhReplay> GetDecodeResult(Guid guid)
    {
        using var scope = scopeFactory.CreateScope();
        var decodeService = scope.ServiceProvider.GetRequiredService<DecodeService>();

        ManualResetEvent mr = new(false);
        DecodeEventArgs? decodeEvent = null;
        decodeService.DecodeFinished += (o, e) =>
        {
            if (e.Guid == guid)
            {
                decodeEvent = e;
                mr.Set();
            }
        };
        mr.WaitOne(20000);
        return decodeEvent?.IhReplays ?? [];
    }

    public async Task<List<string>> GetDecodeResultAsync(Guid guid)
    {
        if (groups.TryGetValue(guid, out GroupState? groupState)
            && groupState is not null)
        {
            using var scope = scopeFactory.CreateScope();
            var decodeService = scope.ServiceProvider.GetRequiredService<DecodeService>();

            var completionSource = new TaskCompletionSource<List<IhReplay>>();

            EventHandler<DecodeEventArgs>? decodeEventHandler = null;

            decodeEventHandler = (sender, args) =>
            {
                if (args.Guid == guid)
                {
                    decodeService.DecodeFinished -= decodeEventHandler;
                    completionSource.SetResult(args.IhReplays);
                }
            };

            decodeService.DecodeFinished += decodeEventHandler;

            var timeoutTask = Task.Delay(20000);

            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                decodeService.DecodeFinished -= decodeEventHandler;
                throw new TimeoutException("Decoding operation timed out.");
            }

            var result = await completionSource.Task;
            var hashes = result.Select(s => s.Replay.ReplayHash).ToList();
            groupState.ReplayHashes.UnionWith(result.Select(s => s.Replay.ReplayHash));
            return hashes;
        }
        return [];
    }
}

