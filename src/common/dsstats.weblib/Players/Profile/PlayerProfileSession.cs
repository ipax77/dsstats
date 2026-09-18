using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.weblib.Players.Profile;

/// <summary>Owns a modal session's selections, request lifetimes and successful responses.</summary>
public sealed class PlayerProfileSession(IPlayerProfileService profiles, IStatsService stats) : IDisposable
{
    private readonly Dictionary<string, PlayerProfileState> states = [];
    private readonly List<PlayerProfileState> history = [];
    private CancellationTokenSource lifetime = new();
    private CancellationTokenSource? chartCancellation;
    private CancellationTokenSource? gainCancellation;
    private int generation;
    private int chartGeneration;
    private int gainGeneration;
    public PlayerProfileState? Current { get; private set; }
    public bool CanGoBack => history.Count > 0;
    public event Action? Changed;

    public async Task Navigate(PlayerProfileRequest request, bool remember = true)
    {
        bool expanded = Current?.Expanded ?? false;
        if (remember && Current is not null && Key(Current.Request) != Key(request))
        {
            history.Add(Current);
            if (history.Count > 3) history.RemoveAt(0);
        }
        CancelRequests();
        if (!states.TryGetValue(Key(request), out var state))
        {
            state = new() { Request = request with { ToonId = new()
                { Id = request.ToonId.Id, Region = request.ToonId.Region, Realm = request.ToonId.Realm } } };
            states[Key(request)] = state;
        }
        state.Expanded |= expanded;
        Current = state;
        await LoadCurrent();
    }

    public Task SwitchRating(RatingType ratingType) =>
        Navigate(Current!.Request with { RatingType = ratingType }, remember: false);

    public async Task Back()
    {
        if (!CanGoBack) return;
        var previous = history[^1];
        history.RemoveAt(history.Count - 1);
        CancelRequests();
        Current = previous;
        await LoadCurrent();
    }

    private async Task LoadCurrent()
    {
        int version = generation;
        await LoadOverview();
        if (version == generation && Current?.Expanded == true && Current.Overview is not null)
            await Expand();
    }

    public async Task LoadOverview()
    {
        var state = Current!;
        if (state.Overview is not null) { Changed?.Invoke(); return; }
        int version = generation;
        var token = lifetime.Token;
        state.OverviewLoading = true;
        state.OverviewError = null;
        state.NotFound = false;
        Changed?.Invoke();
        try
        {
            var result = await profiles.GetOverview(state.Request, token);
            if (version != generation || token.IsCancellationRequested) return;
            state.Overview = result;
            state.NotFound = result is null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { if (version == generation) state.OverviewError = "We couldn’t load this player. Please try again."; }
        finally { if (version == generation) state.OverviewLoading = false; Changed?.Invoke(); }
    }

    public async Task Expand()
    {
        var state = Current!;
        state.Expanded = true;
        int version = generation;
        var token = lifetime.Token;
        if (state.Details is null && !state.DetailsLoading)
        {
            state.DetailsLoading = true;
            state.DetailsError = null;
            Changed?.Invoke();
            try
            {
                var result = await profiles.GetDetails(state.Request, token);
                if (version != generation || token.IsCancellationRequested) return;
                state.Details = result;
                if (result is null) state.DetailsError = "Details are no longer available for this player.";
                else state.Gains[result.CommanderPerformance.TimePeriod] = result.CommanderPerformance;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception) { if (version == generation) state.DetailsError = "We couldn’t load player details. Your overview is still available."; }
            finally { if (version == generation) state.DetailsLoading = false; Changed?.Invoke(); }
        }
        if (version == generation && !token.IsCancellationRequested)
            await SelectSection(state.Section);
    }

    public async Task SelectSection(string section)
    {
        Current!.Section = section;
        Changed?.Invoke();
        if (section == "Performance") await LoadChart();
        if (section == "Breakdown" && Current.Details is not null) await LoadGains();
    }

    public async Task SelectChart(StatsType type, TimePeriod period)
    {
        Current!.ChartType = type;
        Current.ChartPeriod = period;
        await LoadChart();
    }

    public async Task LoadChart()
    {
        var state = Current!;
        var key = (state.ChartType, state.ChartPeriod);
        int version = generation;
        int chartVersion = ++chartGeneration;
        chartCancellation?.Cancel();
        chartCancellation?.Dispose();
        chartCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = chartCancellation.Token;
        state.ChartError = null;
        state.ChartLoading = !state.Charts.ContainsKey(key);
        Changed?.Invoke();
        if (!state.ChartLoading) return;
        var request = new StatsRequest { Type = key.ChartType, TimePeriod = key.ChartPeriod,
            RatingType = state.Request.RatingType, WithLeavers = true };
        try
        {
            IStatsResponse result = key.ChartType switch
            {
                StatsType.Count => await stats.GetUserStatsAsync<CountResponse>(key.ChartType, request, state.Request.ToonId, token),
                StatsType.Synergy => await stats.GetUserStatsAsync<SynergyResponse>(key.ChartType, request, state.Request.ToonId, token),
                StatsType.Timeline => await stats.GetUserStatsAsync<TimelineResponse>(key.ChartType, request, state.Request.ToonId, token),
                _ => await stats.GetUserStatsAsync<WinrateResponse>(key.ChartType, request, state.Request.ToonId, token)
            };
            if (version == generation && !token.IsCancellationRequested) state.Charts[key] = result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { if (version == generation && chartVersion == chartGeneration) state.ChartError = "We couldn’t load this chart. Please try again."; }
        finally
        {
            if (chartVersion == chartGeneration) state.ChartLoading = false;
            Changed?.Invoke();
        }
    }

    public async Task LoadGains(TimePeriod? period = null)
    {
        var state = Current!;
        if (period.HasValue) state.GainPeriod = period.Value;
        var selected = state.GainPeriod;
        int version = generation;
        int gainVersion = ++gainGeneration;
        gainCancellation?.Cancel();
        gainCancellation?.Dispose();
        gainCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = gainCancellation.Token;
        state.GainError = null;
        state.GainLoading = !state.Gains.ContainsKey(selected);
        Changed?.Invoke();
        if (!state.GainLoading) return;
        try
        {
            var result = await profiles.GetCommanderPerformance(new()
            {
                ToonId = state.Request.ToonId, RatingType = state.Request.RatingType, TimePeriod = selected
            }, token);
            if (version == generation && !token.IsCancellationRequested) state.Gains[selected] = result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { if (version == generation && gainVersion == gainGeneration) state.GainError = "We couldn’t load commander performance. Please try again."; }
        finally
        {
            if (gainVersion == gainGeneration) state.GainLoading = false;
            Changed?.Invoke();
        }
    }

    private void CancelRequests()
    {
        generation++;
        chartGeneration++;
        gainGeneration++;
        lifetime.Cancel();
        lifetime.Dispose();
        lifetime = new();
        if (Current is { } state)
        {
            state.OverviewLoading = state.DetailsLoading = state.ChartLoading = state.GainLoading = false;
        }
    }

    public void Suspend() => CancelRequests();
    public Task Resume() => LoadCurrent();
    public void Dispose()
    {
        CancelRequests();
        chartCancellation?.Dispose();
        gainCancellation?.Dispose();
        lifetime.Dispose();
    }

    private static string Key(PlayerProfileRequest request) =>
        $"{request.ToonId.Region}:{request.ToonId.Realm}:{request.ToonId.Id}:{request.RatingType}";
}

public sealed class PlayerProfileState
{
    public required PlayerProfileRequest Request { get; init; }
    public PlayerOverview? Overview { get; set; }
    public PlayerProfileDetails? Details { get; set; }
    public bool Expanded { get; set; }
    public string Section { get; set; } = "Performance";
    public StatsType ChartType { get; set; } = StatsType.Winrate;
    public TimePeriod ChartPeriod { get; set; } = TimePeriod.Last90Days;
    public TimePeriod GainPeriod { get; set; } = TimePeriod.Last90Days;
    public Dictionary<(StatsType, TimePeriod), IStatsResponse> Charts { get; } = [];
    public Dictionary<TimePeriod, CmdrAvgGainResponse> Gains { get; } = [];
    public double ScrollTop { get; set; }
    public bool OverviewLoading { get; set; }
    public bool DetailsLoading { get; set; }
    public bool ChartLoading { get; set; }
    public bool GainLoading { get; set; }
    public bool NotFound { get; set; }
    public string? OverviewError { get; set; }
    public string? DetailsError { get; set; }
    public string? ChartError { get; set; }
    public string? GainError { get; set; }
}
