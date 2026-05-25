import { normalizeReplay } from "./normalization";
import { clampGameloop, drawSpawnPlayback } from "./rendering";
import { deleteState, getState, setState } from "./store";
import type { DotNetCallbackRef, SpawnPlaybackState } from "./types";

const ALIVE_UNIT_ROW_SELECTOR = "[data-spawn-playback-alive-unit-row]";
const ALIVE_UNIT_CLEAR_SELECTOR = "[data-spawn-playback-clear-highlight]";
const ALIVE_UNIT_SELECTED_CLASS = "spawn-playback-alive-row-selected";
const longTaskObservers = new WeakMap<HTMLCanvasElement, PerformanceObserver>();

export function initializeSpawnPlayback(
    canvas: HTMLCanvasElement,
    rootElement: Element | null,
    replay: unknown,
    callbackRef: DotNetCallbackRef | null,
    gameloopsPerSecond: number,
    speedMultiplier: number): void {
    const startedAt = performance.now();
    let stageStarted = startedAt;
    const stages: string[] = [];
    const markStage = (name: string): void => {
        const now = performance.now();
        stages.push(`${name}=${(now - stageStarted).toFixed(1)}ms`);
        stageStarted = now;
    };
    const normalizedReplay = normalizeReplay(replay);
    markStage("normalizeReplay");
    const state: SpawnPlaybackState = {
        replay: normalizedReplay,
        callbackRef,
        gameloopsPerSecond: Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0
            ? gameloopsPerSecond
            : 22.4,
        speedMultiplier: Number.isFinite(speedMultiplier) && speedMultiplier > 0
            ? speedMultiplier
            : 1,
        resizeObserver: null,
        isMounted: true,
        isDisposing: false,
        pendingResizeRaf: null,
        currentGameloop: 0,
        running: false,
        animationFrameId: 0,
        lastFrameTimestamp: 0,
        lastProgressTimestamp: 0,
        activeUnits: [],
        nextUnitIndex: 0,
        lastActiveGameloop: Number.NEGATIVE_INFINITY,
        staticGeometry: null,
        renderCache: null,
        staticBackgroundCanvas: null,
        staticCanvasWidth: 0,
        staticCanvasHeight: 0,
        objectiveDeathAnnouncements: [],
        unitSpriteCache: new Map(),
        highlightedAliveUnitKey: null,
        rootElement,
        modalElement: null,
        modalHideListener: null,
        fullscreenListener: null,
        aliveUnitClickListener: null,
        aliveUnitKeydownListener: null
    };
    markStage("createState");

    stopLongTaskObserver(canvas);
    disposeState(getState(canvas));
    markStage("disposeExistingState");

    state.fullscreenListener = () => handleFullscreenChange(canvas);
    document.addEventListener("fullscreenchange", state.fullscreenListener);
    markStage("addFullscreenListener");
    initializeAliveUnitHighlightEvents(canvas, state);
    markStage("initializeAliveUnitEvents");
    setState(canvas, state);
    markStage("setState");
    startLongTaskObserver(canvas);
    markStage("startLongTaskObserver");
    logPlaybackDiagnostic(
        `initializeSpawnPlayback units=${state.replay.units.length} players=${state.replay.players.length} stages=[${stages.join(", ")}]`,
        startedAt);
}

export function observeSpawnPlaybackResize(canvas: HTMLCanvasElement): void {
    const startedAt = performance.now();
    const state = getState(canvas);
    if (!state) {
        logPlaybackDiagnostic("observe resize skipped reason=no-state", startedAt);
        return;
    }

    const initialSkipReason = getResizeSkipReason(state, canvas);
    if (initialSkipReason) {
        logPlaybackDiagnostic(`observe resize skipped reason=${initialSkipReason}`, startedAt);
        return;
    }

    if (!state.modalElement) {
        state.modalElement = state.rootElement?.closest(".modal") ?? null;
    }

    if (state.modalElement && !state.modalHideListener) {
        state.modalHideListener = () => suspendSpawnPlayback(state, "modal-hide");
        state.modalElement.addEventListener("hide.bs.modal", state.modalHideListener);
    }

    if (!state.resizeObserver) {
        state.resizeObserver = new ResizeObserver(entries => handleResizeObserved(canvas, state, entries));
    }

    state.resizeObserver.observe(canvas);
    logPlaybackDiagnostic(
        `observe resize attached modal=${state.modalElement !== null} client=${canvas.clientWidth}x${canvas.clientHeight} backing=${canvas.width}x${canvas.height}`,
        startedAt);
}

export function startSpawnPlayback(
    canvas: HTMLCanvasElement,
    currentGameloop: number,
    speedMultiplier: number): void {
    const state = getState(canvas);
    if (!state?.replay || !state.isMounted || state.isDisposing) {
        return;
    }

    if (Number.isFinite(currentGameloop)) {
        state.currentGameloop = clampGameloop(state, currentGameloop);
    }

    if (Number.isFinite(speedMultiplier) && speedMultiplier > 0) {
        state.speedMultiplier = speedMultiplier;
    }

    state.running = true;
    state.lastFrameTimestamp = 0;
    state.lastProgressTimestamp = 0;
    cancelAnimation(state);
    notifyProgress(state, "playing");
    state.animationFrameId = requestAnimationFrame(timestamp => animateSpawnPlayback(canvas, timestamp));
}

export function pauseSpawnPlayback(canvas: HTMLCanvasElement, notify = true): number {
    const state = getState(canvas);
    if (!state || state.isDisposing) {
        return 0;
    }

    state.running = false;
    cancelAnimation(state);
    if (notify) {
        notifyProgress(state, "paused");
    }

    return state.currentGameloop;
}

export function stopSpawnPlayback(canvas: HTMLCanvasElement, notify = true): number {
    const state = getState(canvas);
    if (!state || state.isDisposing) {
        return 0;
    }

    state.running = false;
    cancelAnimation(state);
    if (notify) {
        notifyProgress(state, "stopped");
    }

    return state.currentGameloop;
}

export function setSpawnPlaybackSpeed(canvas: HTMLCanvasElement, speedMultiplier: number): void {
    const state = getState(canvas);
    if (!state || state.isDisposing || !Number.isFinite(speedMultiplier) || speedMultiplier <= 0) {
        return;
    }

    state.speedMultiplier = speedMultiplier;
}

export async function setSpawnPlaybackFullscreen(
    canvas: HTMLCanvasElement,
    rootElement: Element | null,
    fullscreen: boolean): Promise<void> {
    const state = getState(canvas);
    if (!state || state.isDisposing) {
        return;
    }

    if (rootElement) {
        state.rootElement = rootElement;
    }

    if (fullscreen) {
        const target = state.rootElement as HTMLElement | null;
        if (!target || document.fullscreenElement === target) {
            notifyFullscreenChanged(state);
            return;
        }

        if (target.requestFullscreen) {
            await target.requestFullscreen();
        }
    } else if (document.fullscreenElement === state.rootElement && document.exitFullscreen) {
        await document.exitFullscreen();
    } else {
        notifyFullscreenChanged(state);
    }
}

export function disposeSpawnPlayback(canvas: HTMLCanvasElement): void {
    const startedAt = performance.now();
    const state = getState(canvas);
    console.log(`spawnPlayback dispose start hasState=${state !== undefined} frame=${state?.animationFrameId ?? 0} running=${state?.running ?? false} sprites=${state?.unitSpriteCache.size ?? 0} active=${state?.activeUnits.length ?? 0} hasStatic=${state?.staticBackgroundCanvas !== null} - ${Date.now()}`);
    let stageStarted = performance.now();
    stopLongTaskObserver(canvas);
    logPlaybackStage("dispose stopLongTaskObserver", stageStarted);
    stageStarted = performance.now();
    disposeState(state);
    logPlaybackStage("dispose disposeState", stageStarted);
    stageStarted = performance.now();
    deleteState(canvas);
    logPlaybackStage("dispose deleteState", stageStarted);
    logPlaybackDiagnostic("disposeSpawnPlayback", startedAt);
}

export function syncAliveUnitHighlightSelection(canvas: HTMLCanvasElement): void {
    const state = getState(canvas);
    syncAliveUnitHighlightRows(state);
}

function animateSpawnPlayback(canvas: HTMLCanvasElement, timestamp: number): void {
    const state = getState(canvas);
    if (!state?.running || !state.isMounted || state.isDisposing) {
        return;
    }

    if (state.lastFrameTimestamp === 0) {
        state.lastFrameTimestamp = timestamp;
    }

    const elapsedSeconds = Math.max(0, timestamp - state.lastFrameTimestamp) / 1000;
    state.lastFrameTimestamp = timestamp;
    state.currentGameloop = clampGameloop(
        state,
        state.currentGameloop + elapsedSeconds * state.gameloopsPerSecond * state.speedMultiplier);

    drawSpawnPlayback(canvas, state.currentGameloop, "animation-frame");

    if (state.currentGameloop >= state.replay.durationGameloop) {
        state.running = false;
        state.animationFrameId = 0;
        notifyProgress(state, "ended");
        return;
    }

    if (timestamp - state.lastProgressTimestamp >= 250) {
        state.lastProgressTimestamp = timestamp;
        notifyProgress(state, "playing");
    }

    state.animationFrameId = requestAnimationFrame(nextTimestamp => {
        const nextState = getState(canvas);
        if (!nextState?.isMounted || nextState.isDisposing) {
            return;
        }

        animateSpawnPlayback(canvas, nextTimestamp);
    });
}

function notifyProgress(state: SpawnPlaybackState, status: string): void {
    state.callbackRef?.invokeMethodAsync(
        "ReceiveSpawnPlaybackProgress",
        Math.round(state.currentGameloop),
        status).catch(() => { });
}

function disposeState(state: SpawnPlaybackState | undefined): void {
    if (!state) {
        return;
    }

    state.isMounted = false;
    state.isDisposing = true;
    state.running = false;
    let stageStarted = performance.now();
    cancelPendingResize(state);
    logPlaybackStage("disposeState cancelPendingResize", stageStarted);
    stageStarted = performance.now();
    cancelAnimation(state);
    logPlaybackStage("disposeState cancelAnimation", stageStarted);
    if (state.resizeObserver) {
        stageStarted = performance.now();
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
        logPlaybackStage("disposeState disconnectResizeObserver", stageStarted);
    }

    if (state.fullscreenListener) {
        stageStarted = performance.now();
        document.removeEventListener("fullscreenchange", state.fullscreenListener);
        state.fullscreenListener = null;
        logPlaybackStage("disposeState removeFullscreenListener", stageStarted);
    }

    if (state.modalElement && state.modalHideListener) {
        stageStarted = performance.now();
        state.modalElement.removeEventListener("hide.bs.modal", state.modalHideListener);
        state.modalHideListener = null;
        state.modalElement = null;
        logPlaybackStage("disposeState removeModalHideListener", stageStarted);
    }

    stageStarted = performance.now();
    disposeAliveUnitHighlightEvents(state);
    logPlaybackStage("disposeState disposeAliveUnitEvents", stageStarted);
}

function cancelAnimation(state: SpawnPlaybackState | undefined): void {
    if (state?.animationFrameId) {
        cancelAnimationFrame(state.animationFrameId);
        state.animationFrameId = 0;
    }
}

function handleResizeObserved(
    canvas: HTMLCanvasElement,
    state: SpawnPlaybackState,
    entries: ResizeObserverEntry[]): void {
    const startedAt = performance.now();
    const entry = entries[0];
    console.log(
        `spawnPlayback resizeObserver start entries=${entries.length} content=${entry?.contentRect.width.toFixed(1) ?? "-"}x${entry?.contentRect.height.toFixed(1) ?? "-"} client=${canvas.clientWidth}x${canvas.clientHeight} backing=${canvas.width}x${canvas.height} contains=${document.contains(canvas)} ${getRootVisibilityDiagnostics(state.rootElement)} - ${Date.now()}`);

    const skipReason = getResizeSkipReason(state, canvas, entry);
    if (skipReason) {
        logPlaybackDiagnostic(`resizeObserver skipped reason=${skipReason}`, startedAt);
        return;
    }

    if (state.pendingResizeRaf !== null) {
        logPlaybackDiagnostic("resizeObserver skipped reason=pending-raf", startedAt);
        return;
    }

    state.pendingResizeRaf = requestAnimationFrame(() => {
        state.pendingResizeRaf = null;
        const rafStartedAt = performance.now();
        const rafSkipReason = getResizeSkipReason(state, canvas);
        if (rafSkipReason) {
            logPlaybackDiagnostic(`resizeObserver raf skipped reason=${rafSkipReason}`, rafStartedAt);
            return;
        }

        drawSpawnPlayback(canvas, state.currentGameloop, "resize-observer");
        logPlaybackDiagnostic("resizeObserver raf draw", rafStartedAt);
    });

    logPlaybackDiagnostic("resizeObserver scheduled raf", startedAt);
}

function getResizeSkipReason(
    state: SpawnPlaybackState,
    canvas: HTMLCanvasElement,
    entry?: ResizeObserverEntry): string | null {
    if (!state.isMounted) {
        return "not-mounted";
    }

    if (state.isDisposing) {
        return "disposing";
    }

    if (!canvas.isConnected || !document.contains(canvas)) {
        return "disconnected";
    }

    if (entry && (entry.contentRect.width <= 0 || entry.contentRect.height <= 0)) {
        return "zero-content";
    }

    const rect = canvas.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0 || canvas.clientWidth <= 0 || canvas.clientHeight <= 0) {
        return "zero-client";
    }

    if (isRootOrModalHidden(state.rootElement)) {
        return "hidden";
    }

    return null;
}

function isRootOrModalHidden(rootElement: Element | null): boolean {
    if (!rootElement || !rootElement.isConnected) {
        return true;
    }

    const rootStyle = rootElement instanceof HTMLElement ? getComputedStyle(rootElement) : null;
    if (rootStyle?.display === "none" || rootStyle?.visibility === "hidden") {
        return true;
    }

    const modal = rootElement.closest(".modal");
    if (!(modal instanceof HTMLElement)) {
        return false;
    }

    const modalStyle = getComputedStyle(modal);
    return modalStyle.display === "none"
        || modalStyle.visibility === "hidden"
        || modal.getAttribute("aria-hidden") === "true";
}

function suspendSpawnPlayback(state: SpawnPlaybackState, reason: string): void {
    const startedAt = performance.now();
    state.isDisposing = true;
    state.running = false;
    cancelPendingResize(state);
    cancelAnimation(state);
    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }

    logPlaybackDiagnostic(`suspend reason=${reason}`, startedAt);
}

function cancelPendingResize(state: SpawnPlaybackState): void {
    if (state.pendingResizeRaf === null) {
        return;
    }

    cancelAnimationFrame(state.pendingResizeRaf);
    state.pendingResizeRaf = null;
    console.log(`spawnPlayback pending resize raf canceled - ${Date.now()}`);
}

function startLongTaskObserver(canvas: HTMLCanvasElement): void {
    if (!("PerformanceObserver" in window)) {
        return;
    }

    const supportedTypes = PerformanceObserver.supportedEntryTypes ?? [];
    if (!supportedTypes.includes("longtask")) {
        return;
    }

    try {
        const observer = new PerformanceObserver(list => {
            for (const entry of list.getEntries()) {
                console.log(`spawnPlayback longtask duration=${entry.duration.toFixed(1)}ms start=${entry.startTime.toFixed(1)} - ${Date.now()}`);
            }
        });
        observer.observe({ entryTypes: ["longtask"] });
        longTaskObservers.set(canvas, observer);
    } catch {
    }
}

function stopLongTaskObserver(canvas: HTMLCanvasElement): void {
    const observer = longTaskObservers.get(canvas);
    if (!observer) {
        return;
    }

    observer.disconnect();
    longTaskObservers.delete(canvas);
}

function logPlaybackStage(message: string, startedAt: number): void {
    console.log(`spawnPlayback ${message} elapsed=${(performance.now() - startedAt).toFixed(1)}ms - ${Date.now()}`);
}

function logPlaybackDiagnostic(message: string, startedAt: number): void {
    console.log(`spawnPlayback ${message} elapsed=${(performance.now() - startedAt).toFixed(1)}ms - ${Date.now()}`);
}

function handleFullscreenChange(canvas: HTMLCanvasElement): void {
    const state = getState(canvas);
    if (!state || !state.isMounted || state.isDisposing) {
        return;
    }

    notifyFullscreenChanged(state);
    requestAnimationFrame(() => {
        if (!state.isMounted || state.isDisposing) {
            return;
        }

        drawSpawnPlayback(canvas, state.currentGameloop, "fullscreen-change");
    });
}

function notifyFullscreenChanged(state: SpawnPlaybackState): void {
    state.callbackRef?.invokeMethodAsync(
        "ReceiveSpawnPlaybackFullscreenChanged",
        document.fullscreenElement === state.rootElement).catch(() => { });
}

function initializeAliveUnitHighlightEvents(canvas: HTMLCanvasElement, state: SpawnPlaybackState): void {
    if (!state.rootElement) {
        return;
    }

    state.aliveUnitClickListener = event => handleAliveUnitHighlightClick(canvas, event);
    state.aliveUnitKeydownListener = event => handleAliveUnitHighlightKeydown(canvas, event);
    state.rootElement.addEventListener("click", state.aliveUnitClickListener);
    state.rootElement.addEventListener("keydown", state.aliveUnitKeydownListener);
}

function disposeAliveUnitHighlightEvents(state: SpawnPlaybackState): void {
    if (!state.rootElement) {
        return;
    }

    if (state.aliveUnitClickListener) {
        state.rootElement.removeEventListener("click", state.aliveUnitClickListener);
        state.aliveUnitClickListener = null;
    }

    if (state.aliveUnitKeydownListener) {
        state.rootElement.removeEventListener("keydown", state.aliveUnitKeydownListener);
        state.aliveUnitKeydownListener = null;
    }
}

function handleAliveUnitHighlightClick(canvas: HTMLCanvasElement, event: Event): void {
    const target = event.target;
    if (!(target instanceof Element)) {
        return;
    }

    if (target.closest(ALIVE_UNIT_CLEAR_SELECTOR)) {
        setAliveUnitHighlight(canvas, null);
        return;
    }

    const row = target.closest<HTMLElement>(ALIVE_UNIT_ROW_SELECTOR);
    if (!row) {
        return;
    }

    const key = row.dataset.spawnPlaybackHighlightKey;
    if (key) {
        toggleAliveUnitHighlight(canvas, key);
    }
}

function handleAliveUnitHighlightKeydown(canvas: HTMLCanvasElement, event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (keyboardEvent.key !== "Enter" && keyboardEvent.key !== " ") {
        return;
    }

    const target = keyboardEvent.target;
    if (!(target instanceof Element)) {
        return;
    }

    const row = target.closest<HTMLElement>(ALIVE_UNIT_ROW_SELECTOR);
    if (!row) {
        return;
    }

    const key = row.dataset.spawnPlaybackHighlightKey;
    if (!key) {
        return;
    }

    keyboardEvent.preventDefault();
    toggleAliveUnitHighlight(canvas, key);
}

function toggleAliveUnitHighlight(canvas: HTMLCanvasElement, key: string): void {
    const state = getState(canvas);
    if (!state) {
        return;
    }

    setAliveUnitHighlight(canvas, resolveAliveUnitHighlightToggle(state.highlightedAliveUnitKey, key));
}

export function resolveAliveUnitHighlightToggle(currentKey: string | null, nextKey: string): string | null {
    return currentKey === nextKey ? null : nextKey;
}

function setAliveUnitHighlight(canvas: HTMLCanvasElement, key: string | null): void {
    const state = getState(canvas);
    if (!state || state.highlightedAliveUnitKey === key) {
        return;
    }

    state.highlightedAliveUnitKey = key;
    syncAliveUnitHighlightRows(state);
    requestAliveUnitHighlightRedraw(canvas, state);
}

function syncAliveUnitHighlightRows(state: SpawnPlaybackState | undefined): void {
    const rootElement = state?.rootElement;
    if (!rootElement) {
        return;
    }

    const selectedKey = state.highlightedAliveUnitKey;
    const rows = rootElement.querySelectorAll<HTMLElement>(ALIVE_UNIT_ROW_SELECTOR);
    for (const row of rows) {
        const selected = selectedKey !== null && row.dataset.spawnPlaybackHighlightKey === selectedKey;
        row.classList.toggle(ALIVE_UNIT_SELECTED_CLASS, selected);
        row.setAttribute("aria-pressed", selected ? "true" : "false");
    }
}

function requestAliveUnitHighlightRedraw(canvas: HTMLCanvasElement, state: SpawnPlaybackState): void {
    if (state.running || !state.isMounted || state.isDisposing) {
        return;
    }

    requestAnimationFrame(() => {
        if (!state.isMounted || state.isDisposing) {
            return;
        }

        drawSpawnPlayback(canvas, state.currentGameloop, "alive-highlight");
    });
}

function getRootVisibilityDiagnostics(rootElement: Element | null): string {
    if (!rootElement) {
        return "root=null";
    }

    const modal = rootElement.closest(".modal");
    const rootStyle = rootElement instanceof HTMLElement ? getComputedStyle(rootElement) : null;
    const modalStyle = modal instanceof HTMLElement ? getComputedStyle(modal) : null;
    return [
        `rootConnected=${rootElement.isConnected}`,
        `rootDisplay=${rootStyle?.display ?? "-"}`,
        `rootVisibility=${rootStyle?.visibility ?? "-"}`,
        `modalShow=${modal?.classList.contains("show") ?? false}`,
        `modalAriaHidden=${modal?.getAttribute("aria-hidden") ?? "-"}`,
        `modalDisplay=${modalStyle?.display ?? "-"}`
    ].join(" ");
}
