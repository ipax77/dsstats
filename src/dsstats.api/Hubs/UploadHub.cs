using dsstats.api.Services;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

public class UploadHub(IServiceScopeFactory scopeFactory) : Hub
{
    public async Task DecodeRequest(Guid guid)
    {
        using var scope = scopeFactory.CreateScope();
        var decodeService = scope.ServiceProvider.GetRequiredService<DecodeService>();

        var completionSource = new TaskCompletionSource<DecodeEventArgs>();

        EventHandler<DecodeEventArgs>? decodeEventHandler = null;

        decodeEventHandler = (sender, args) =>
        {
            if (args.Guid == guid)
            {
                decodeService.DecodeFinished -= decodeEventHandler;
                completionSource.SetResult(args);
            }
        };

        decodeService.DecodeFinished += decodeEventHandler;

        var timeoutTask = Task.Delay(25000);

        var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            decodeService.DecodeFinished -= decodeEventHandler;
            await Clients.Client(Context.ConnectionId).SendAsync("DecodeFailed", "timeout");
        }

        var result = await completionSource.Task;
        if (string.IsNullOrEmpty(result.Error))
        {
            var hashes = result.IhReplays.Select(s => s.Replay.ReplayHash).ToList();
            await Clients.Client(Context.ConnectionId).SendAsync("NewReplays", hashes);
        }
        else
        {
            await Clients.Client(Context.ConnectionId).SendAsync("DecodeFailed", result.Error);
        }
    }

    public async Task DecodeRawRequest(Guid guid)
    {
        using var scope = scopeFactory.CreateScope();
        var decodeService = scope.ServiceProvider.GetRequiredService<DecodeService>();

        var completionSource = new TaskCompletionSource<DecodeRawEventArgs>();

        EventHandler<DecodeRawEventArgs>? decodeEventHandler = null;

        decodeEventHandler = (sender, args) =>
        {
            if (args.Guid == guid)
            {
                decodeService.DecodeRawFinished -= decodeEventHandler;
                completionSource.SetResult(args);
            }
        };

        decodeService.DecodeRawFinished += decodeEventHandler;

        var timeoutTask = Task.Delay(25000);

        var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            decodeService.DecodeRawFinished -= decodeEventHandler;
            await Clients.Client(Context.ConnectionId).SendAsync("DecodeFailed", "timeout");
        }

        var result = await completionSource.Task;
        if (string.IsNullOrEmpty(result.Error))
        {
            
            await Clients.Client(Context.ConnectionId).SendAsync("NewChallengeResponses", result.ChallengeResponses);
        }
        else
        {
            await Clients.Client(Context.ConnectionId).SendAsync("DecodeFailed", result.Error);
        }
    }
}
