import { normalizeReplay, normalizeUnitLifeCosts } from "./normalization";
import { clampGameloop, drawSpawnPlayback } from "./rendering";
import { deleteState, getState, setState } from "./store";
import type { DotNetCallbackRef, SpawnPlaybackState } from "./types";

const ALIVE_UNIT_ROW_SELECTOR = "[data-spawn-playback-alive-unit-row]";
const ALIVE_UNIT_CLEAR_SELECTOR = "[data-spawn-playback-clear-highlight]";
const ALIVE_UNIT_SELECTED_CLASS = "spawn-playback-alive-row-selected";

export function initializeSpawnPlayback(
    canvas: HTMLCanvasElement,
    rootElement: Element | null,
    replay: unknown,
    unitLifeCosts: unknown,
    showSpawnWaveOverlay: boolean,
    callbackRef: DotNetCallbackRef | null,
    gameloopsPerSecond: number,
    speedMultiplier: number): void {
    const normalizedReplay = normalizeReplay(replay);
    const loopsPerSecond = Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0
        ? gameloopsPerSecond
        : 22.4;
    const state: SpawnPlaybackState = {
        replay: normalizedReplay,
        callbackRef,
        gameloopsPerSecond: loopsPerSecond,
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
        unitLifeCostByKey: normalizeUnitLifeCosts(unitLifeCosts),
        showSpawnWaveOverlay,
        spawnWaveEvents: createSpawnWaveEvents(normalizedReplay.units, loopsPerSecond),
        spawnWaveTableCache: new Map(),
        unitSpriteCache: new Map(),
        highlightedAliveUnitKey: null,
        rootElement,
        modalElement: null,
        modalHideListener: null,
        fullscreenListener: null,
        aliveUnitClickListener: null,
        aliveUnitKeydownListener: null
    };

    disposeState(getState(canvas));

    state.fullscreenListener = () => handleFullscreenChange(canvas);
    document.addEventListener("fullscreenchange", state.fullscreenListener);
    initializeAliveUnitHighlightEvents(canvas, state);
    setState(canvas, state);
}

function createSpawnWaveEvents(
    units: readonly {
        teamId: number;
        gamePos: number;
        playerName: string;
        spawnNumber: number;
        spawnGameloop: number;
    }[],
    gameloopsPerSecond: number): {
        key: string;
        teamId: number;
        spawnNumber: number;
        playerName: string;
        gamePos: number;
        anchorGameloop: number;
        startGameloop: number;
        holdEndGameloop: number;
        endGameloop: number;
    }[] {
    const fadeGameloops = Math.max(1, Math.round(gameloopsPerSecond * 1.2));
    const holdGameloops = Math.max(1, Math.round(gameloopsPerSecond * 5));
    const starts = new Map<string, {
        teamId: number;
        spawnNumber: number;
        playerName: string;
        gamePos: number;
        anchorGameloop: number;
    }>();
    for (const unit of units) {
        if (unit.spawnNumber <= 0 || !Number.isFinite(unit.spawnGameloop)) {
            continue;
        }

        const key = createSpawnWaveEventKey(unit.teamId, unit.gamePos, unit.playerName, unit.spawnNumber);
        const existing = starts.get(key);
        if (existing === undefined || unit.spawnGameloop < existing.anchorGameloop) {
            starts.set(key, {
                teamId: unit.teamId,
                spawnNumber: unit.spawnNumber,
                playerName: unit.playerName,
                gamePos: unit.gamePos,
                anchorGameloop: unit.spawnGameloop
            });
        }
    }

    return [...starts]
        .map(([key, event]) => ({
            key,
            teamId: event.teamId,
            spawnNumber: event.spawnNumber,
            playerName: event.playerName,
            gamePos: event.gamePos,
            anchorGameloop: event.anchorGameloop,
            startGameloop: Math.max(0, event.anchorGameloop - fadeGameloops),
            holdEndGameloop: event.anchorGameloop + holdGameloops,
            endGameloop: event.anchorGameloop + holdGameloops + fadeGameloops
        }))
        .sort((left, right) =>
            left.startGameloop - right.startGameloop
            || left.anchorGameloop - right.anchorGameloop
            || left.teamId - right.teamId
            || left.gamePos - right.gamePos);
}

function createSpawnWaveEventKey(teamId: number, gamePos: number, playerName: string, spawnNumber: number): string {
    return `${teamId}|${gamePos}|${spawnNumber}|${playerName.length}:${playerName}`;
}

export function observeSpawnPlaybackResize(canvas: HTMLCanvasElement): void {
    const state = getState(canvas);
    if (!state) {
        return;
    }

    if (getResizeSkipReason(state, canvas)) {
        return;
    }

    if (!state.modalElement) {
        state.modalElement = state.rootElement?.closest(".modal") ?? null;
    }

    if (state.modalElement && !state.modalHideListener) {
        state.modalHideListener = () => suspendSpawnPlayback(state);
        state.modalElement.addEventListener("hide.bs.modal", state.modalHideListener);
    }

    if (!state.resizeObserver) {
        state.resizeObserver = new ResizeObserver(entries => handleResizeObserved(canvas, state, entries));
    }

    state.resizeObserver.observe(canvas);
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

export function setSpawnWaveOverlayVisible(canvas: HTMLCanvasElement, visible: boolean): void {
    const state = getState(canvas);
    if (!state || state.isDisposing || state.showSpawnWaveOverlay === visible) {
        return;
    }

    state.showSpawnWaveOverlay = visible;
    if (!state.running && state.isMounted) {
        requestAnimationFrame(() => {
            if (!state.isMounted || state.isDisposing) {
                return;
            }

            drawSpawnPlayback(canvas, state.currentGameloop);
        });
    }
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
    const state = getState(canvas);
    disposeState(state);
    deleteState(canvas);
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

    drawSpawnPlayback(canvas, state.currentGameloop);

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
    cancelPendingResize(state);
    cancelAnimation(state);
    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }

    if (state.fullscreenListener) {
        document.removeEventListener("fullscreenchange", state.fullscreenListener);
        state.fullscreenListener = null;
    }

    if (state.modalElement && state.modalHideListener) {
        state.modalElement.removeEventListener("hide.bs.modal", state.modalHideListener);
        state.modalHideListener = null;
        state.modalElement = null;
    }

    disposeAliveUnitHighlightEvents(state);
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
    const entry = entries[0];

    if (getResizeSkipReason(state, canvas, entry)) {
        return;
    }

    if (state.pendingResizeRaf !== null) {
        return;
    }

    state.pendingResizeRaf = requestAnimationFrame(() => {
        state.pendingResizeRaf = null;
        if (getResizeSkipReason(state, canvas)) {
            return;
        }

        drawSpawnPlayback(canvas, state.currentGameloop);
    });
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

function suspendSpawnPlayback(state: SpawnPlaybackState): void {
    state.isDisposing = true;
    state.running = false;
    cancelPendingResize(state);
    cancelAnimation(state);
    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }
}

function cancelPendingResize(state: SpawnPlaybackState): void {
    if (state.pendingResizeRaf === null) {
        return;
    }

    cancelAnimationFrame(state.pendingResizeRaf);
    state.pendingResizeRaf = null;
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

        drawSpawnPlayback(canvas, state.currentGameloop);
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

        drawSpawnPlayback(canvas, state.currentGameloop);
    });
}
