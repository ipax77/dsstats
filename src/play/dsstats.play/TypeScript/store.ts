import type { SpawnPlaybackState } from "./types";

export const states = new WeakMap<HTMLCanvasElement, SpawnPlaybackState>();

export function getState(canvas: HTMLCanvasElement): SpawnPlaybackState | undefined {
    return states.get(canvas);
}

export function setState(canvas: HTMLCanvasElement, state: SpawnPlaybackState): void {
    states.set(canvas, state);
}

export function deleteState(canvas: HTMLCanvasElement): void {
    states.delete(canvas);
}
