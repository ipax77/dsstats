import type { Bounds, CanvasContext, LayerCanvas, Point, Projection, Segment } from "./types";

export function createLayerCanvas(width: number, height: number): LayerCanvas {
    if (typeof OffscreenCanvas !== "undefined") {
        return new OffscreenCanvas(width, height);
    }

    const layer = document.createElement("canvas");
    layer.width = width;
    layer.height = height;
    return layer;
}

export function getCanvasContext(canvas: LayerCanvas | HTMLCanvasElement): CanvasContext | null {
    return canvas.getContext("2d") as CanvasContext | null;
}

export function resizeCanvas(canvas: HTMLCanvasElement): boolean {
    const width = Math.max(320, Math.floor(canvas.clientWidth));
    const height = Math.max(240, Math.floor(canvas.clientHeight));
    const scale = window.devicePixelRatio || 1;
    const targetWidth = Math.floor(width * scale);
    const targetHeight = Math.floor(height * scale);

    if (canvas.width !== targetWidth || canvas.height !== targetHeight) {
        canvas.width = targetWidth;
        canvas.height = targetHeight;
        return true;
    }

    return false;
}

export function deviceScale(canvas: HTMLCanvasElement): number {
    return canvas.width / Math.max(1, canvas.clientWidth);
}

export function createProjection(bounds: Bounds, canvas: HTMLCanvasElement): Projection {
    const padding = 24 * deviceScale(canvas);
    const width = Math.max(1, bounds.maxX - bounds.minX);
    const height = Math.max(1, bounds.maxY - bounds.minY);
    return {
        minX: bounds.minX,
        minY: bounds.minY,
        scaleX: (canvas.width - padding * 2) / width,
        scaleY: (canvas.height - padding * 2) / height,
        left: padding,
        bottom: canvas.height - padding
    };
}

export function projectX(projection: Projection, x: number): number {
    return projection.left + (x - projection.minX) * projection.scaleX;
}

export function projectY(projection: Projection, y: number): number {
    return projection.bottom - (y - projection.minY) * projection.scaleY;
}

export function project(x: number, y: number, bounds: Bounds, canvas: HTMLCanvasElement): Point {
    const padding = 24 * deviceScale(canvas);
    const width = Math.max(1, bounds.maxX - bounds.minX);
    const height = Math.max(1, bounds.maxY - bounds.minY);

    return {
        x: padding + ((x - bounds.minX) / width) * (canvas.width - padding * 2),
        y: canvas.height - padding - ((y - bounds.minY) / height) * (canvas.height - padding * 2)
    };
}

export function projectSegment(segment: Segment, bounds: Bounds, canvas: HTMLCanvasElement): Segment {
    return {
        start: project(segment.start.x, segment.start.y, bounds, canvas),
        end: project(segment.end.x, segment.end.y, bounds, canvas)
    };
}

export function clipSumLine(bounds: Bounds, sum: number): Segment | null {
    return createSegmentFromIntersections([
        { x: bounds.minX, y: sum - bounds.minX },
        { x: bounds.maxX, y: sum - bounds.maxX },
        { x: sum - bounds.minY, y: bounds.minY },
        { x: sum - bounds.maxY, y: bounds.maxY }
    ], bounds);
}

export function clipDiffLine(bounds: Bounds, diff: number): Segment | null {
    return createSegmentFromIntersections([
        { x: bounds.minX, y: bounds.minX - diff },
        { x: bounds.maxX, y: bounds.maxX - diff },
        { x: bounds.minY + diff, y: bounds.minY },
        { x: bounds.maxY + diff, y: bounds.maxY }
    ], bounds);
}

export function isPointInBounds(point: Point, bounds: Bounds): boolean {
    const epsilon = 0.001;
    return point.x >= bounds.minX - epsilon
        && point.x <= bounds.maxX + epsilon
        && point.y >= bounds.minY - epsilon
        && point.y <= bounds.maxY + epsilon;
}

export function containsPoint(points: Point[], point: Point): boolean {
    return points.some(existing => Math.abs(existing.x - point.x) < 0.001 && Math.abs(existing.y - point.y) < 0.001);
}

export function distanceSquared(left: Point, right: Point): number {
    const x = left.x - right.x;
    const y = left.y - right.y;
    return (x * x) + (y * y);
}

export function roundUpToInterval(value: number, interval: number): number {
    return Math.ceil(value / interval) * interval;
}

export function drawRoundedRect(
    ctx: CanvasContext,
    x: number,
    y: number,
    width: number,
    height: number,
    radius: number): void {
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
}

export function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
}

export function withAlpha(color: string, alpha: string): string {
    if (color.startsWith("#") && color.length === 7) {
        return `${color}${alpha}`;
    }

    return color;
}

function createSegmentFromIntersections(candidates: Point[], bounds: Bounds): Segment | null {
    const points: Point[] = [];
    for (const point of candidates) {
        if (!isPointInBounds(point, bounds) || containsPoint(points, point)) {
            continue;
        }

        points.push(point);
    }

    if (points.length < 2) {
        return null;
    }

    let start = points[0];
    let end = points[1];
    let maxDistance = distanceSquared(start, end);
    for (let i = 0; i < points.length - 1; i++) {
        for (let j = i + 1; j < points.length; j++) {
            const distance = distanceSquared(points[i], points[j]);
            if (distance > maxDistance) {
                start = points[i];
                end = points[j];
                maxDistance = distance;
            }
        }
    }

    return { start, end };
}
