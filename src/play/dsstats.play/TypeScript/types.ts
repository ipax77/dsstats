export type RawObject = Record<string, unknown>;
export type LayerCanvas = HTMLCanvasElement | OffscreenCanvas;
export type CanvasContext = CanvasRenderingContext2D | OffscreenCanvasRenderingContext2D;

export interface DotNetCallbackRef {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface Point {
    x: number;
    y: number;
}

export interface Segment {
    start: Point;
    end: Point;
}

export interface Bounds {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
}

export interface Projection {
    minX: number;
    minY: number;
    scaleX: number;
    scaleY: number;
    left: number;
    bottom: number;
}

export interface MiddleControl {
    firstTeamId: number;
    changeGameloops: number[];
}

export interface UnitIconCircleLayer {
    type: "circle";
    cx: number;
    cy: number;
    r: number;
    fill?: string;
    stroke?: string;
    strokeWidth?: number;
    opacity?: number;
}

export type UnitIconPathCommand =
    | ["M", number, number]
    | ["L", number, number]
    | ["C", number, number, number, number, number, number]
    | ["Z"];

export interface UnitIconPathLayer {
    type: "path";
    commands: UnitIconPathCommand[];
    fill?: string;
    stroke?: string;
    strokeWidth?: number;
    opacity?: number;
    lineCap?: CanvasLineCap;
    lineJoin?: CanvasLineJoin;
}

export type UnitIconLayer = UnitIconCircleLayer | UnitIconPathLayer;

export interface UnitIconDefinition {
    id: string;
    commander: string;
    aliases: string[];
    viewBox: {
        width: number;
        height: number;
    };
    tokens: Record<string, string>;
    layers: UnitIconLayer[];
}

export interface UnitIconRenderOptions {
    x?: number;
    y?: number;
    size?: number;
    teamColor?: string;
}

export interface UnitRender {
    radius: number;
    sprite: LayerCanvas;
}

export interface NormalizedUnit {
    name: string;
    playerName: string;
    gamePos: number;
    commander: string;
    aliveUnitHighlightKey: string;
    spawnNumber: number;
    spawnGameloop: number;
    expiresGameloop: number;
    spawnX: number;
    spawnY: number;
    deltaX: number;
    deltaY: number;
    inverseLifetime: number;
    radius: number;
    color: string;
    teamId: number;
    iconDefinition: UnitIconDefinition | null;
    iconResolved: boolean;
    render: UnitRender | null;
}

export interface NormalizedPlayer {
    name: string;
    teamId: number;
    gamePos: number;
    commander: string;
    refineryGameloops: number[];
    tierUpgradeGameloops: number[];
    units: NormalizedUnit[];
}

export interface NormalizedReplay {
    durationGameloop: number;
    stepGameloops: number;
    bounds: Bounds;
    stats: unknown;
    summary: PlaybackSummary;
    middleControl: MiddleControl;
    landmarks: RawObject[];
    buildUnits: unknown[];
    snapshots: unknown[];
    players: NormalizedPlayer[];
    units: NormalizedUnit[];
}

export interface PlaybackPlayerSummary {
    playerName: string;
    teamId: number;
    gamePos: number;
    commander: string;
    kills: number;
}

export interface PlaybackTopUnitSummary {
    playerName: string;
    teamId: number;
    gamePos: number;
    unitName: string;
    kills: number;
}

export interface PlaybackSummary {
    totalKills: number;
    players: PlaybackPlayerSummary[];
    topUnits: PlaybackTopUnitSummary[];
}

export interface TeamSpawnAreaSource {
    teamId: number;
    label: string;
    color: string;
    labelSegment: [number, number];
    labelSide?: -1 | 1;
    points: Point[];
}

export interface SpawnAreaLabelGeometry {
    x: number;
    y: number;
    angle: number;
}

export interface SpawnAreaGeometry {
    teamId: number;
    label: string;
    color: string;
    points: Point[];
    labelGeometry: SpawnAreaLabelGeometry | null;
}

export interface LandmarkGeometry {
    x: number | null;
    y: number | null;
    kind: string;
    teamId: number;
    color: string;
    kills: number;
    label: string;
    radius: number;
    diedGameloop: number | null;
    projected: Point | null;
}

export interface PlayerGasBadge {
    x: number;
    y: number;
    gamePos: number;
    color: string;
    refineryGameloops: number[];
    tierUpgradeGameloops: number[];
}

export interface StaticGeometry {
    gridLines: Segment[];
    middleLine: Segment | null;
    middleControl: MiddleControl;
    spawnAreas: SpawnAreaGeometry[];
    playerGasBadges: PlayerGasBadge[];
    landmarks: LandmarkGeometry[];
}

export interface ObjectiveDeathAnnouncement {
    message: string;
    accentColor: string;
    anchorGameloop: number;
    startGameloop: number;
    holdEndGameloop: number;
    endGameloop: number;
}

export interface UnitLifeCost {
    cost: number;
    life: number;
}

export interface UnitLifeCostEntry {
    key: string;
    cost: number;
    life: number;
}

export interface SpawnWaveEvent {
    key: string;
    teamId: number;
    spawnNumber: number;
    playerName: string;
    gamePos: number;
    anchorGameloop: number;
    startGameloop: number;
    holdEndGameloop: number;
    endGameloop: number;
}

export interface SpawnWaveUnitRow {
    teamId: number;
    unitName: string;
    count: number;
    cost: number | null;
    life: number | null;
    totalCost: number | null;
    totalLife: number | null;
}

export interface SpawnWaveTeamTable {
    teamId: number;
    spawnNumber: number;
    playerName: string;
    gamePos: number;
    rows: SpawnWaveUnitRow[];
    totalCount: number;
    totalCost: number;
    totalLife: number;
}

export interface RenderCache {
    projection: Projection;
}

export interface SpawnPlaybackState {
    replay: NormalizedReplay;
    callbackRef: DotNetCallbackRef | null;
    gameloopsPerSecond: number;
    speedMultiplier: number;
    resizeObserver: ResizeObserver | null;
    isMounted: boolean;
    isDisposing: boolean;
    pendingResizeRaf: number | null;
    currentGameloop: number;
    running: boolean;
    animationFrameId: number;
    lastFrameTimestamp: number;
    lastProgressTimestamp: number;
    activeUnits: NormalizedUnit[];
    nextUnitIndex: number;
    lastActiveGameloop: number;
    staticGeometry: StaticGeometry | null;
    renderCache: RenderCache | null;
    staticBackgroundCanvas: LayerCanvas | null;
    staticCanvasWidth: number;
    staticCanvasHeight: number;
    objectiveDeathAnnouncements: ObjectiveDeathAnnouncement[];
    unitLifeCostByKey: Map<string, UnitLifeCost>;
    showSpawnWaveOverlay: boolean;
    spawnWaveEvents: SpawnWaveEvent[];
    spawnWaveTableCache: Map<string, SpawnWaveTeamTable>;
    unitSpriteCache: Map<string, LayerCanvas>;
    highlightedAliveUnitKey: string | null;
    rootElement: Element | null;
    modalElement: Element | null;
    modalHideListener: ((event: Event) => void) | null;
    fullscreenListener: (() => void) | null;
    aliveUnitClickListener: ((event: Event) => void) | null;
    aliveUnitKeydownListener: ((event: Event) => void) | null;
}
