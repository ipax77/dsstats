// dto.ts v1.0

export interface TableOrder {
    name: string;
    ascending: boolean;
}

export interface ReplayFilter {
    name?: string;
    commanders?: number[];
    linkCommanders?: boolean;
    tableOrders?: TableOrder[];
    skip?: number;
    take?: number;
}

export interface PwaConfig {
    configVersion: string;
    appGuid: string;
    cPuCores: number;
    uploadCredentials: boolean;
    ignoreReplays: string[];
    replayStartname: string;
    culture: string;
}

export interface ExportedReplays {
    hashes: string[];
    payload: string; // base64 string containing gzipped JSON of ReplayDto[]
}

export interface ExportResult {
    hashes: string[];
    payload: Uint8Array;
}

export interface UploadRequestDto {
    appGuid: string;
    appVersion: string;
    requestNames: string[];
    replays: ReplayDto[];
}

export interface ReplayMeta {
    replayHash: string;
    filePath: string;
    regionId: number;
    uploaded: number;
    skip: boolean;
}

export interface ReplayDto {
    fileName: string;
    compatHash: string;
    title: string;
    version: string;
    gameMode: number; // enum in C#, numeric in JS
    regionId: number;
    gametime: string; // stored as ISO string
    baseBuild: number;
    duration: number;
    cannon: number;
    bunker: number;
    winnerTeam: number;
    middleChanges: number[];
    players: ReplayPlayerDto[];
}

export interface ReplayPlayerDto {
    name: string;
    clan?: string;
    race: number;
    selectedRace: number;
    teamId: number;
    gamePos: number;
    result: number;
    duration: number;
    apm: number;
    messages: number;
    pings: number;
    isMvp: boolean;
    isUploader: boolean;
    spawns: SpawnDto[];
    upgrades: UpgradeDto[];
    tierUpgrades: number[];
    refineries: number[];
    player: PlayerDto;
}

export interface PlayerDto {
    playerId: number;
    name: string;
    toonId: ToonIdDto;
}

export interface ToonIdDto {
    region: number;
    realm: number;
    id: number;
}

export interface SpawnDto {
    breakpoint: number;
    income: number;
    gasCount: number;
    armyValue: number;
    killedValue: number;
    upgradeSpent: number;
    units: UnitDto[];
}

export interface UnitDto {
    name: string;
    count: number;
    positions: number[];
}

export interface UpgradeDto {
    name: string;
    gameloop: number;
}

export interface ReplayListDto {
    replayHash: string;
    gametime: string;
    gameMode: number;
    duration: number;
    winnerTeam: number;
    commandersTeam1: number[];
    commandersTeam2: number[];
    playerNames: string[];
    exp2Win?: number;
    avgRating?: number;
    leaverType: number;
    playerPos: number;
}

export interface FileInfoRecord {
    path: string;
    name: string;
    size: number;
    lastModified: number;
}

export interface FileContentRecord {
    contentBase64: string;
}