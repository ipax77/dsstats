using dsstats.shared;
using dsstats.shared.Units;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace dsstats.play;

public partial class SpawnPlaybackCanvas
{
    [Parameter]
    public ReplayDto? Replay { get; set; }

    [Parameter]
    public SpawnPlaybackSidecarDto? Sidecar { get; set; }

    [Parameter]
    public SpawnPlaybackReplayNg? Playback { get; set; }

    private ElementReference playbackElement;
    private ElementReference canvasElement;
    private IJSObjectReference? module;
    private DotNetObjectReference<SpawnPlaybackCanvas>? dotNetReference;
    private SpawnPlaybackReplayNg? playback;
    private SpawnPlaybackReplayNg? initializedReplay;
    private int currentGameloop;
    private int renderGameloop;
    private readonly List<int> playbackStops = [];
    private int currentPlaybackStopIndex;
    private bool canvasDrawPending;
    private readonly List<AliveUnitRow> team1AliveUnitRows = [];
    private readonly List<AliveUnitRow> team2AliveUnitRows = [];
    private readonly Dictionary<AliveUnitKey, AliveUnitAccumulator> aliveUnitAccumulators = [];
    private IReadOnlyList<SpawnPlaybackLifeCostEntry> unitLifeCostEntries = [];
    private int aliveUnitCount;
    private int team1AliveCount;
    private int team2AliveCount;
    private int currentSpawnNumber;
    private bool isFullscreen;
    private bool showAliveUnits;
    private bool aliveUnitsDefaultResolved;
    private bool showSpawnWaveOverlay = false;
    private SpawnPlaybackAnimationState animationState = SpawnPlaybackAnimationState.Stopped;
    private string selectedPlaybackSpeed = "normal";

    private string PlayPauseTitle => animationState == SpawnPlaybackAnimationState.Playing
        ? "Pause"
        : animationState == SpawnPlaybackAnimationState.Paused
            ? "Resume"
            : "Play";

    private string PlayPauseIcon => animationState == SpawnPlaybackAnimationState.Playing
        ? "bi-pause-fill"
        : "bi-play-fill";

    private string FullscreenTitle => isFullscreen ? "Exit fullscreen" : "Enter fullscreen";

    private string FullscreenIcon => isFullscreen ? "bi-fullscreen-exit" : "bi-fullscreen";

    private string AliveUnitsToggleTitle => showAliveUnits ? "Collapse Alive Units" : "Expand Alive Units";

    private string AliveUnitsToggleIcon => showAliveUnits ? "bi-chevron-up" : "bi-chevron-down";

    private bool HasSpawnWaveOverlayData => unitLifeCostEntries.Count > 0;

    private string SpawnWaveOverlayTitle => HasSpawnWaveOverlayData
        ? showSpawnWaveOverlay ? "Hide spawn cost/life overlay" : "Show spawn cost/life overlay"
        : "Spawn cost/life overlay unavailable";

    private string SpawnWaveOverlayButtonClass => showSpawnWaveOverlay && HasSpawnWaveOverlayData
        ? "btn-warning"
        : "btn-outline-light";

    private string PlaybackStatusLabel => animationState switch
    {
        SpawnPlaybackAnimationState.Playing => $"Playing {GetPlaybackSpeedMultiplier():0.##}x",
        SpawnPlaybackAnimationState.Paused => "Paused",
        _ => "Stopped"
    };

    private string CurrentTimeLabel =>
        playback is null
            ? string.Empty
            : $"{Math.Round(renderGameloop / SpawnPlaybackFactoryNg.GameloopsPerSecond, 1):0.0}s / gameloop {renderGameloop:N0}";

    private string CurrentSpawnLabel => currentSpawnNumber > 0 ? currentSpawnNumber.ToString("N0") : "-";

    private int PlaybackStopMaxIndex => Math.Max(0, playbackStops.Count - 1);

    private string FirstPlaybackStopLabel => playbackStops.Count == 0 ? string.Empty : SpawnPlaybackTimeline.FormatGameloopSeconds(playbackStops[0]);

    private string CurrentPlaybackStopLabel => playbackStops.Count == 0
        ? string.Empty
        : $"{currentPlaybackStopIndex + 1:N0} / {playbackStops.Count:N0}";

    private string LastPlaybackStopLabel => playbackStops.Count == 0
        ? string.Empty
        : SpawnPlaybackTimeline.FormatGameloopSeconds(playbackStops[^1]);

    protected override async Task OnInitializedAsync()
    {
        var nextPlayback = Playback
            ?? (Replay is not null && Sidecar is not null
                ? SpawnPlaybackFactoryNg.Create(Replay, Sidecar)
                : null);
        initializedReplay = null;
        SpawnPlaybackTimeline.BuildPlaybackStops(null, playbackStops);
        currentGameloop = 0;
        renderGameloop = 0;
        currentPlaybackStopIndex = 0;
        animationState = SpawnPlaybackAnimationState.Stopped;
        isFullscreen = false;
        showAliveUnits = false;
        aliveUnitsDefaultResolved = false;
        showSpawnWaveOverlay = true;
        unitLifeCostEntries = await CreateUnitLifeCostEntries(nextPlayback);
        playback = nextPlayback;
        SpawnPlaybackTimeline.BuildPlaybackStops(playback, playbackStops);
        currentGameloop = SpawnPlaybackTimeline.GetFirstPlaybackGameloop(playback);
        currentPlaybackStopIndex = SpawnPlaybackTimeline.FindPlaybackStopIndexAtOrBefore(playbackStops, currentGameloop);
        SetRenderGameloop(force: true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (playback is null)
        {
            return;
        }

        if (module is null)
        {
            module = await ImportModuleAsync();
        }

        if (dotNetReference is null)
        {
            dotNetReference = DotNetObjectReference.Create(this);
        }

        if (!aliveUnitsDefaultResolved)
        {
            bool isMobile = await module.InvokeAsync<bool>("isSpawnPlaybackMobileViewport");
            showAliveUnits = !isMobile;
            aliveUnitsDefaultResolved = true;
            UpdateAliveUnitRows();
            canvasDrawPending = true;
            if (showAliveUnits)
            {
                StateHasChanged();
            }
        }

        if (!ReferenceEquals(initializedReplay, playback))
        {
            initializedReplay = playback;
            await module.InvokeVoidAsync(
                "initializeSpawnPlaybackNg",
                canvasElement,
                playbackElement,
                SpawnPlaybackBinaryPayloads.CreateMetadata(playback),
                unitLifeCostEntries,
                showSpawnWaveOverlay,
                dotNetReference,
                SpawnPlaybackFactoryNg.GameloopsPerSecond,
                GetPlaybackSpeedMultiplier(),
                SpawnPlaybackBinaryPayloads.GetPayloadBytes(playback, SpawnPlaybackBinaryPayloads.UnitRowsDatasetId),
                SpawnPlaybackBinaryPayloads.GetPayloadBytes(playback, SpawnPlaybackBinaryPayloads.PathRowsDatasetId),
                SpawnPlaybackBinaryPayloads.GetPayloadBytes(playback, SpawnPlaybackBinaryPayloads.PathPointsDatasetId),
                SpawnPlaybackBinaryPayloads.GetPayloadBytes(playback, SpawnPlaybackBinaryPayloads.KillGameloopsDatasetId));
            canvasDrawPending = true;
        }

        if (canvasDrawPending)
        {
            await module.InvokeVoidAsync("drawSpawnPlayback", canvasElement, renderGameloop);
            await module.InvokeVoidAsync("observeSpawnPlaybackResize", canvasElement);
            canvasDrawPending = false;
        }

        if (showAliveUnits)
        {
            await module.InvokeVoidAsync("hydrateUnitIcons", playbackElement);
            await module.InvokeVoidAsync("syncAliveUnitHighlightSelection", canvasElement);
        }
    }

    private ValueTask<IJSObjectReference> ImportModuleAsync()
    {
        return JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/dsstats.play/spawnPlayback.js");
    }

    private async Task TogglePlayback()
    {
        if (playback is null || module is null)
        {
            return;
        }

        if (animationState == SpawnPlaybackAnimationState.Playing)
        {
            await PauseAnimation();
            return;
        }

        animationState = SpawnPlaybackAnimationState.Playing;
        await module.InvokeVoidAsync(
            "startSpawnPlayback",
            canvasElement,
            currentGameloop,
            GetPlaybackSpeedMultiplier());
    }

    private async Task PauseAnimation()
    {
        if (module is null)
        {
            animationState = SpawnPlaybackAnimationState.Paused;
            return;
        }

        double stoppedGameloop = await module.InvokeAsync<double>("pauseSpawnPlayback", canvasElement, false);
        ApplyAnimationProgress(stoppedGameloop, SpawnPlaybackAnimationState.Paused, requestCanvasDraw: false);
        StateHasChanged();
    }

    private async Task StopAnimation()
    {
        await StopAnimationAndSync(SpawnPlaybackAnimationState.Stopped);
        StateHasChanged();
    }

    private async Task StepBackward()
    {
        if (playback is null)
        {
            return;
        }

        await StopAnimationAndSync(SpawnPlaybackAnimationState.Stopped);
        currentGameloop = Math.Max(SpawnPlaybackTimeline.GetFirstPlaybackGameloop(playback), currentGameloop - playback.StepGameloops);
        SetRenderGameloop();
        currentPlaybackStopIndex = SpawnPlaybackTimeline.FindPlaybackStopIndexAtOrBefore(playbackStops, renderGameloop);
    }

    private async Task StepForward()
    {
        if (playback is null)
        {
            return;
        }

        await StopAnimationAndSync(SpawnPlaybackAnimationState.Stopped);
        currentGameloop = Math.Min(playback.DurationGameloop, currentGameloop + playback.StepGameloops);
        SetRenderGameloop();
        currentPlaybackStopIndex = SpawnPlaybackTimeline.FindPlaybackStopIndexAtOrBefore(playbackStops, renderGameloop);
    }

    private async Task SeekPlaybackStop(ChangeEventArgs args)
    {
        if (playback is null || playbackStops.Count == 0)
        {
            return;
        }

        await StopAnimationAndSync(SpawnPlaybackAnimationState.Stopped);
        int nextIndex = SpawnPlaybackTimeline.ParsePlaybackStopIndex(args.Value, currentPlaybackStopIndex, playbackStops.Count);
        if (nextIndex == currentPlaybackStopIndex && playbackStops[nextIndex] == currentGameloop)
        {
            return;
        }

        currentPlaybackStopIndex = nextIndex;
        currentGameloop = playbackStops[nextIndex];
        SetRenderGameloop();
    }

    private async Task SetPlaybackSpeed(ChangeEventArgs args)
    {
        string nextSpeed = args.Value?.ToString() ?? "normal";
        selectedPlaybackSpeed = nextSpeed is "slow" or "normal" or "fast"
            ? nextSpeed
            : "normal";

        if (module is not null)
        {
            await module.InvokeVoidAsync("setSpawnPlaybackSpeed", canvasElement, GetPlaybackSpeedMultiplier());
        }
    }

    private async Task ToggleFullscreen()
    {
        if (playback is null)
        {
            return;
        }

        module ??= await ImportModuleAsync();
        dotNetReference ??= DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync(
            "setSpawnPlaybackFullscreen",
            canvasElement,
            playbackElement,
            !isFullscreen);
    }

    private void ToggleAliveUnits()
    {
        showAliveUnits = !showAliveUnits;
        UpdateAliveUnitRows();
        canvasDrawPending = true;
    }

    private async Task ToggleSpawnWaveOverlay()
    {
        if (!HasSpawnWaveOverlayData)
        {
            return;
        }

        showSpawnWaveOverlay = !showSpawnWaveOverlay;
        if (module is not null)
        {
            await module.InvokeVoidAsync("setSpawnWaveOverlayVisible", canvasElement, showSpawnWaveOverlay);
            return;
        }

        canvasDrawPending = true;
    }

    private async Task StopAnimationAndSync(SpawnPlaybackAnimationState nextState)
    {
        animationState = nextState;
        if (module is null)
        {
            return;
        }

        double stoppedGameloop = await module.InvokeAsync<double>("stopSpawnPlayback", canvasElement, false);
        ApplyAnimationProgress(stoppedGameloop, nextState, requestCanvasDraw: false);
    }

    [JSInvokable]
    public Task ReceiveSpawnPlaybackProgress(double gameloop, string state)
    {
        return InvokeAsync(() =>
        {
            SpawnPlaybackAnimationState nextState = state switch
            {
                "playing" => SpawnPlaybackAnimationState.Playing,
                "paused" => SpawnPlaybackAnimationState.Paused,
                _ => SpawnPlaybackAnimationState.Stopped
            };
            ApplyAnimationProgress(gameloop, nextState, requestCanvasDraw: false);
            StateHasChanged();
        });
    }

    [JSInvokable]
    public Task ReceiveSpawnPlaybackFullscreenChanged(bool fullscreen)
    {
        return InvokeAsync(() =>
        {
            isFullscreen = fullscreen;
            canvasDrawPending = true;
            StateHasChanged();
        });
    }

    private void ApplyAnimationProgress(
        double gameloop,
        SpawnPlaybackAnimationState nextState,
        bool requestCanvasDraw)
    {
        if (playback is null)
        {
            animationState = SpawnPlaybackAnimationState.Stopped;
            return;
        }

        int firstGameloop = SpawnPlaybackTimeline.GetFirstPlaybackGameloop(playback);
        int nextGameloop = (int)Math.Round(gameloop);
        currentGameloop = Math.Clamp(nextGameloop, firstGameloop, playback.DurationGameloop);
        SetRenderGameloop(requestCanvasDraw: requestCanvasDraw);
        currentPlaybackStopIndex = SpawnPlaybackTimeline.FindPlaybackStopIndexAtOrBefore(playbackStops, renderGameloop);
        animationState = nextState;
    }

    private double GetPlaybackSpeedMultiplier()
    {
        return selectedPlaybackSpeed switch
        {
            "slow" => 2,
            "fast" => 6,
            _ => 4
        };
    }

    private bool SetRenderGameloop(bool force = false, bool requestCanvasDraw = true)
    {
        int nextRenderGameloop = SpawnPlaybackTimeline.ResolveRenderGameloop(playback, currentGameloop);
        if (!force && nextRenderGameloop == renderGameloop)
        {
            return false;
        }

        renderGameloop = nextRenderGameloop;
        UpdateAliveUnitRows();
        if (requestCanvasDraw)
        {
            canvasDrawPending = true;
        }

        return true;
    }

    private void UpdateAliveUnitRows()
    {
        var summary = SpawnPlaybackAliveUnits.UpdateRows(
            playback,
            renderGameloop,
            showAliveUnits,
            team1AliveUnitRows,
            team2AliveUnitRows,
            aliveUnitAccumulators);

        aliveUnitCount = summary.AliveUnitCount;
        team1AliveCount = summary.Team1AliveCount;
        team2AliveCount = summary.Team2AliveCount;
        currentSpawnNumber = summary.CurrentSpawnNumber;
    }

    private async Task<IReadOnlyList<SpawnPlaybackLifeCostEntry>> CreateUnitLifeCostEntries(SpawnPlaybackReplayNg? replay)
    {
        if (replay is null)
        {
            return [];
        }

        Dictionary<string, SpawnPlaybackLifeCostEntry> entries = [];
        Dictionary<Commander, IReadOnlyDictionary<string, DsUnitLifeCostDto>> lifeCostsByCommander = [];
        HashSet<AliveUnitKey> seenUnits = [];
        var unitRows = SpawnPlaybackBinaryPayloads.GetPayload(replay, SpawnPlaybackBinaryPayloads.UnitRowsDatasetId);
        if (unitRows is null)
        {
            return [];
        }

        byte[] unitBytes = unitRows.Bytes;
        for (int rowIndex = 0; rowIndex < unitRows.Count; rowIndex++)
        {
            int playerIndex = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowPlayerIndexOffset);
            int unitKindIndex = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowUnitKindIndexOffset);
            if ((uint)playerIndex >= (uint)replay.Players.Count
                || (uint)unitKindIndex >= (uint)replay.UnitKinds.Count)
            {
                continue;
            }

            var player = replay.Players[playerIndex];
            var unitKind = replay.UnitKinds[unitKindIndex];
            var aliveUnitKey = new AliveUnitKey(player.TeamId, player.Commander, unitKind.Name);
            if (!seenUnits.Add(aliveUnitKey))
            {
                continue;
            }

            if (!Enum.TryParse(player.Commander, out Commander commander))
            {
                continue;
            }

            if (!lifeCostsByCommander.TryGetValue(commander, out var lifeCosts))
            {
                lifeCosts = await UnitLifeCostService.GetUnitLifeCosts(commander);
                lifeCostsByCommander[commander] = lifeCosts;
            }

            if (lifeCosts.Count == 0)
            {
                continue;
            }

            var normalizedName = UnitMap.GetNormalizedUnitName(unitKind.Name, commander);
            if (!lifeCosts.TryGetValue(normalizedName, out var unitLifeCost))
            {
                continue;
            }

            var key = SpawnPlaybackAliveUnits.CreateHighlightKey(player.TeamId, player.Commander, unitKind.Name);
            entries.TryAdd(key, new(key, unitLifeCost.Cost, unitLifeCost.Life));
        }

        return entries.Values.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("disposeSpawnPlayback", canvasElement);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }

    private enum SpawnPlaybackAnimationState
    {
        Stopped,
        Playing,
        Paused
    }

}
