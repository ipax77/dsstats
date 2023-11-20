

using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.maui8.Services;

public class WinrateService : IWinrateService, IDisposable
{
    private readonly IWinrateService localWinrateService;
    private readonly IWinrateService remoteWinrateService;
    private readonly IRemoteToggleService remoteToggleService;
    
    public WinrateService([FromKeyedServices("local")] IWinrateService localWinrateService,
                          [FromKeyedServices("remote")] IWinrateService remoteWinrateService,
                          IRemoteToggleService remoteToggleService)
    {
        this.localWinrateService = localWinrateService;
        this.remoteWinrateService = remoteWinrateService;
        this.remoteToggleService = remoteToggleService;
        remoteToggleService.FromServerChanged += FromServerChanged;
    }

    private void FromServerChanged(object? sender, EventArgs e)
    {
        
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteWinrateService.GetWinrate(request, token);
        }
        else
        {
            return await localWinrateService.GetWinrate(request, token);
        }
    }

    public async Task<WinrateResponse> GetWinrate(StatsRequest request, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteWinrateService.GetWinrate(request, token);
        }
        else
        {
            return await localWinrateService.GetWinrate(request, token);
        }
    }

    public void Dispose()
    {
        remoteToggleService.FromServerChanged -= FromServerChanged;
    }
}
