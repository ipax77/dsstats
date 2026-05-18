namespace dsstats.shared.Interfaces;

public interface IBuildDetailsService
{
    Task<List<BuildDetailsOverviewRow>> GetOverview(BuildDetailsRequest request, CancellationToken token = default);
    Task<List<BuildDetailsMatchupRow>> GetMatchups(BuildDetailsMatchupRequest request, CancellationToken token = default);
    Task<List<BuildDetailsSampleReplay>> GetSampleReplays(BuildDetailsSamplesRequest request, CancellationToken token = default);
    Task<List<BuildDetailsTeamBuildOverviewRow>> GetTeamBuildOverview(BuildDetailsRequest request, CancellationToken token = default);
    Task<List<BuildDetailsTeamBuildSampleReplay>> GetTeamBuildSampleReplays(BuildDetailsTeamBuildSamplesRequest request, CancellationToken token = default);
    Task<List<BuildDetailsRaceRosterOverviewRow>> GetRaceRosterOverview(BuildDetailsRequest request, CancellationToken token = default);
    Task<List<BuildDetailsRaceRosterMatchupRow>> GetRaceRosterMatchups(BuildDetailsRaceRosterMatchupRequest request, CancellationToken token = default);
    Task<List<BuildDetailsRaceRosterSampleReplay>> GetRaceRosterSampleReplays(BuildDetailsRaceRosterSamplesRequest request, CancellationToken token = default);
}
