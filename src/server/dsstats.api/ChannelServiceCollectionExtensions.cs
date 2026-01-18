using dsstats.api.Services;
using dsstats.db;
using System.Threading.Channels;

namespace dsstats.api;

public static class ChannelServiceCollectionExtensions
{
    public static IServiceCollection AddUploadChannels(this IServiceCollection services)
    {
        var blobChannel = Channel.CreateUnbounded<UploadJob>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });
        services.AddSingleton<Channel<UploadJob>>(blobChannel);

        // --- Replay Channel Setup ---
        var replayChannel = Channel.CreateUnbounded<ReplayUploadJob>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });
        services.AddSingleton<Channel<ReplayUploadJob>>(replayChannel);

        services.AddHostedService<UploadProcessingService>();
        services.AddHostedService<ReplayProcessingService>();

        return services;
    }
}
