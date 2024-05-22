using dsstats.shared.Interfaces;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db8services;

public partial class TourneysService
{
    public async Task<List<GroupStateDto>> GetGroupStates()
    {
        using var scope = scopeFactory.CreateScope();
        var ihService = scope.ServiceProvider.GetRequiredService<IIhService>();
        return await ihService.GetOpenGroups();
    }

    public async Task<int> GetIhSessionsCount(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var ihRepository = scope.ServiceProvider.GetRequiredService<IIhRepository>();
        return await ihRepository.GetIhSessionsCount(token);
    }

    public async Task<List<IhSessionListDto>> GetIhSessions(int skip, int take, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var ihRepository = scope.ServiceProvider.GetRequiredService<IIhRepository>();
        return await ihRepository.GetIhSessions(skip, take, token);
    }

    public async Task<IhSessionDto?> GetIhSession(Guid groupId)
    {
        using var scope = scopeFactory.CreateScope();
        var ihRepository = scope.ServiceProvider.GetRequiredService<IIhRepository>();
        return await ihRepository.GetIhSession(groupId);
    }

    public async Task<List<ReplayListDto>> GetReplays(Guid groupId)
    {
        using var scope = scopeFactory.CreateScope();
        var ihRepository = scope.ServiceProvider.GetRequiredService<IIhRepository>();
        return await ihRepository.GetReplays(groupId);
    }
}
