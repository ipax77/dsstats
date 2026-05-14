import { ReplayDetailFilter, ReplayDto, ReplayListDto, ReplayPlayerDto, ReplayPosDetailFilter, ReplayPosUnitDetailFilter, ToonIdDto } from "./dtos";

const NONE = 0;

export function hasDetailFilter(filter?: ReplayDetailFilter): boolean {
    if (!filter) {
        return false;
    }

    return !!filter.playercount
        || !!filter.tournamentEdition
        || hasGameModeFilter(filter)
        || ((filter.posFilters?.length ?? 0) > 0);
}

export function needsFullReplayForDetailFilter(filter?: ReplayDetailFilter): boolean {
    if (!hasDetailFilter(filter)) {
        return false;
    }

    return (filter?.posFilters?.length ?? 0) > 0;
}

export function replayListNeedsFullDetailCheck(replayList: ReplayListDto, filter?: ReplayDetailFilter): boolean {
    if (!filter) {
        return false;
    }

    return (filter.playercount !== undefined && filter.playercount !== 0 && replayList.playerCount === undefined && replayList.playerNames === undefined)
        || (!!filter.tournamentEdition && replayList.tournamentEdition === undefined)
        || needsFullReplayForDetailFilter(filter);
}

export function replayListMatchesDetailProjection(replayList: ReplayListDto, filter?: ReplayDetailFilter): boolean {
    if (!filter) {
        return true;
    }

    if (filter.playercount && filter.playercount !== 0) {
        const playerCount = replayList.playerCount ?? replayList.playerNames?.length;
        if (playerCount !== filter.playercount) {
            return false;
        }
    }

    if (filter.tournamentEdition && replayList.tournamentEdition === false) {
        return false;
    }

    if (hasGameModeFilter(filter) && !filter.gameModes!.includes(replayList.gameMode)) {
        return false;
    }

    return true;
}

export function replayMatchesDetailFilter(replay: ReplayDto, filter?: ReplayDetailFilter): boolean {
    if (!filter) {
        return true;
    }

    if (filter.playercount && filter.playercount !== 0 && replay.players.length !== filter.playercount) {
        return false;
    }

    if (filter.tournamentEdition && !replay.title.endsWith("TE")) {
        return false;
    }

    if (hasGameModeFilter(filter) && !filter.gameModes!.includes(replay.gameMode)) {
        return false;
    }

    for (const posFilter of filter.posFilters ?? []) {
        if (!replay.players.some(player => playerMatchesPositionFilter(player, replay.players, posFilter))) {
            return false;
        }

        for (const unitFilter of posFilter.unitFilters ?? []) {
            if (!isActiveUnitFilter(unitFilter)) {
                continue;
            }

            if (!replay.players.some(player => playerMatchesUnitFilter(player, posFilter, unitFilter))) {
                return false;
            }
        }
    }

    return true;
}

function hasGameModeFilter(filter: ReplayDetailFilter): boolean {
    return (filter.gameModes?.length ?? 0) > 0 && !filter.gameModes!.includes(NONE);
}

function playerMatchesPositionFilter(player: ReplayPlayerDto, players: ReplayPlayerDto[], filter: ReplayPosDetailFilter): boolean {
    if (filter.gamePos && filter.gamePos !== 0 && player.gamePos !== filter.gamePos) {
        return false;
    }

    if (filter.commander && filter.commander !== NONE && player.race !== filter.commander) {
        return false;
    }

    if (filter.oppCommander && filter.oppCommander !== NONE && getLaneOpponentRace(player, players) !== filter.oppCommander) {
        return false;
    }

    const nameOrId = filter.playerNameOrId?.trim();
    if (!nameOrId) {
        return true;
    }

    const toonIds = parseToonIds(nameOrId);
    if (toonIds.length > 0) {
        return toonIds.some(toonId => toonIdEquals(player.player?.toonId, toonId));
    }

    return player.name === nameOrId;
}

function playerMatchesUnitFilter(player: ReplayPlayerDto, posFilter: ReplayPosDetailFilter, unitFilter: ReplayPosUnitDetailFilter): boolean {
    if (posFilter.gamePos && posFilter.gamePos !== 0 && player.gamePos !== posFilter.gamePos) {
        return false;
    }

    const unitName = unitFilter.name!.trim();
    const count = unitFilter.count!;
    const breakpoint = unitFilter.breakpoint ?? 4;
    const isMinimum = unitFilter.min ?? true;

    return player.spawns.some(spawn => {
        if (spawn.breakpoint !== breakpoint) {
            return false;
        }

        return spawn.units.some(unit => unit.name === unitName && (isMinimum ? unit.count >= count : unit.count < count));
    });
}

function isActiveUnitFilter(unitFilter: ReplayPosUnitDetailFilter): boolean {
    return !!unitFilter.name?.trim() && (unitFilter.count ?? 0) > 0;
}

function getLaneOpponentRace(player: ReplayPlayerDto, players: ReplayPlayerDto[]): number {
    const opponentPos = player.gamePos <= 3 ? player.gamePos + 3 : player.gamePos - 3;
    return players.find(candidate => candidate.gamePos === opponentPos)?.race ?? NONE;
}

function parseToonIds(playerIdString: string): ToonIdDto[] {
    const decoded = playerIdString.replace(/%7C/gi, "|");
    return decoded
        .split("|")
        .map(part => part.trim())
        .filter(Boolean)
        .map(part => {
            const pieces = part.split("x").map(piece => Number.parseInt(piece, 10));
            if (pieces.length !== 3 || pieces.some(Number.isNaN)) {
                return undefined;
            }

            return {
                id: pieces[0],
                region: pieces[1],
                realm: pieces[2],
            };
        })
        .filter((toonId): toonId is ToonIdDto => !!toonId);
}

function toonIdEquals(left: ToonIdDto | undefined, right: ToonIdDto): boolean {
    return !!left
        && left.id === right.id
        && left.region === right.region
        && left.realm === right.realm;
}
