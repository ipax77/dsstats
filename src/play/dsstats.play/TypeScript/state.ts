import { resizeCanvas } from "./canvasUtils";
import { normalizeReplay } from "./normalization";
import { clampGameloop, drawSpawnPlayback } from "./rendering";
import { deleteState, getState, setState } from "./store";
import type { DotNetCallbackRef, SpawnPlaybackState } from "./types";

export function initializeSpawnPlayback(
    canvas: HTMLCanvasElement,
    rootElement: Element | null,
    replay: unknown,
    callbackRef: DotNetCallbackRef | null,
    gameloopsPerSecond: number,
    speedMultiplier: number): void {
    const state: SpawnPlaybackState = {
        replay: normalizeReplay(replay),
        callbackRef,
        gameloopsPerSecond: Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0
            ? gameloopsPerSecond
            : 22.4,
        speedMultiplier: Number.isFinite(speedMultiplier) && speedMultiplier > 0
            ? speedMultiplier
            : 1,
        resizeObserver: null,
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
        unitSpriteCache: new Map(),
        rootElement,
        fullscreenListener: null
    };

    disposeState(getState(canvas));

    state.resizeObserver = new ResizeObserver(() => drawSpawnPlayback(canvas, state.currentGameloop));
    state.resizeObserver.observe(canvas);
    state.fullscreenListener = () => handleFullscreenChange(canvas);
    document.addEventListener("fullscreenchange", state.fullscreenListener);
    setState(canvas, state);
    resizeCanvas(canvas);
}

export function startSpawnPlayback(
    canvas: HTMLCanvasElement,
    currentGameloop: number,
    speedMultiplier: number): void {
    const state = getState(canvas);
    if (!state?.replay) {
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
    if (!state) {
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
    if (!state) {
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
    if (!state || !Number.isFinite(speedMultiplier) || speedMultiplier <= 0) {
        return;
    }

    state.speedMultiplier = speedMultiplier;
}

export async function setSpawnPlaybackFullscreen(
    canvas: HTMLCanvasElement,
    rootElement: Element | null,
    fullscreen: boolean): Promise<void> {
    const state = getState(canvas);
    if (!state) {
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

function animateSpawnPlayback(canvas: HTMLCanvasElement, timestamp: number): void {
    const state = getState(canvas);
    if (!state?.running) {
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

    state.animationFrameId = requestAnimationFrame(nextTimestamp => animateSpawnPlayback(canvas, nextTimestamp));
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

    state.running = false;
    cancelAnimation(state);
    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }

    if (state.fullscreenListener) {
        document.removeEventListener("fullscreenchange", state.fullscreenListener);
        state.fullscreenListener = null;
    }
}

function cancelAnimation(state: SpawnPlaybackState | undefined): void {
    if (state?.animationFrameId) {
        cancelAnimationFrame(state.animationFrameId);
        state.animationFrameId = 0;
    }
}

function handleFullscreenChange(canvas: HTMLCanvasElement): void {
    const state = getState(canvas);
    if (!state) {
        return;
    }

    notifyFullscreenChanged(state);
    requestAnimationFrame(() => drawSpawnPlayback(canvas, state.currentGameloop));
}

function notifyFullscreenChanged(state: SpawnPlaybackState): void {
    state.callbackRef?.invokeMethodAsync(
        "ReceiveSpawnPlaybackFullscreenChanged",
        document.fullscreenElement === state.rootElement).catch(() => { });
}
