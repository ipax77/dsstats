
using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Collections.Concurrent;

namespace dsstats.api.Services;

public partial class IhService(IServiceScopeFactory scopeFactory) : IIhService
{
    private ConcurrentDictionary<Guid, GroupState> groups = [];
    private ConcurrentDictionary<Guid, List<IhReplay>> groupReplays = [];
    SemaphoreSlim decodeSS = new(1, 1);

    public async Task<List<GroupStateDto>> GetOpenGroups()
    {
        var groupDtos = groups
            .Select(s => new GroupStateDto()
            {
                GroupId = s.Value.GroupId,
                RatingType = s.Value.RatingType,
                Visitors = s.Value.Visitors,
                Created = s.Value.Created,
            })
            .OrderByDescending(o => o.Created)
            .ToList();
        return await Task.FromResult(groupDtos);
    }

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

        if (!groupReplays.TryGetValue(groupId, out List<IhReplay>? replays)
            || replays is null)
        {
            groupReplays.AddOrUpdate(groupId, [], (k, v) => v = []);
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

    public async Task<GroupState?> GetDecodeResultAsync(Guid guid)
    {
        List<IhReplay> replays = [];

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
                return null;
            }

            var result = await completionSource.Task;
            await decodeSS.WaitAsync();
            try
            {
                foreach (var replay in result)
                {
                    if (groupState.ReplayHashes.Contains(replay.Replay.ReplayHash))
                    {
                        continue;
                    }
                    groupState.ReplayHashes.Add(replay.Replay.ReplayHash);
                    replays.Add(replay);
                    groupReplays[guid].Add(replay);
                }
                await SetReplayStats(groupState, replays);
            }
            finally
            {
                decodeSS.Release();
            }
        }
        return groupState;
    }

    public async Task<PlayerState?> AddPlayerToGroup(Guid groupId, RequestNames requestNames)
    {

        if (!groups.TryGetValue(groupId, out GroupState? groupState)
            || groupState is null)
        {
            return null;
        }

        PlayerId playerId = new(requestNames.ToonId, requestNames.RealmId, requestNames.RegionId);
        if (groupState.PlayerStates.Any(a => a.PlayerId == playerId))
        {
            return null;
        }

        (var name, var rating) = await GetNameAndRating(groupState, playerId);

        PlayerState playerState = new()
        {
            PlayerId = playerId,
            Name = requestNames.Name,
            RatingStart = rating,
            InQueue = true,
            QueuePriority = QueuePriority.High
        };
        groupState.PlayerStates.Add(playerState);
        return playerState;
    }

    public async Task<PlayerState?> RemovePlayerFromGroup(Guid groupId, RequestNames requestNames)
    {
        if (!groups.TryGetValue(groupId, out GroupState? groupState)
            || groupState is null)
        {
            return null;
        }

        PlayerId playerId = new(requestNames.ToonId, requestNames.RealmId, requestNames.RegionId);
        var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

        if (playerState is null)
        {
            return null;
        }
                
        groupState.PlayerStates.Remove(playerState);
        return await Task.FromResult(playerState);
    }

    public async Task<bool> AddPlayerToQueue(Guid groupId, PlayerId playerId)
    {
        if (!groups.TryGetValue(groupId, out GroupState? groupState)
            || groupState is null)
        {
            return false;
        }

        var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

        if (playerState is null)
        {
            return false;
        }

        playerState.InQueue = true;
        return await Task.FromResult(true);
    }

    public async Task<bool> RemovePlayerFromQueue(Guid groupId, PlayerId playerId)
    {
        if (!groups.TryGetValue(groupId, out GroupState? groupState)
            || groupState is null)
        {
            return false;
        }

        var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

        if (playerState is null)
        {
            return false;
        }

        playerState.InQueue = false;
        return await Task.FromResult(true);
    }
}

