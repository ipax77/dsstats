const states = new WeakMap();

const MAP_WIDTH = 256;
const MAP_HEIGHT = 240;
const MAP_CENTER_SUM = (MAP_WIDTH / 2) + (MAP_HEIGHT / 2);
const GRID_INTERVAL = 16;
const NEUTRAL_MIDDLE_LINE_COLOR = "rgba(255, 193, 7, 0.70)";
const GAS_BADGE_WIDTH = 82;
const GAS_BADGE_HEIGHT = 24;
const GAS_BADGE_GAP = 8;
const GAS_BADGE_CORNER_PADDING = 20;
const TEAM_COLORS = {
    1: "#5DADEC",
    2: "#F87171"
};
const TEAM_SPAWN_AREAS = [
    {
        teamId: 1,
        label: "Team 1",
        color: "#5DADEC",
        labelSegment: [0, 1],
        points: [
            { x: 165, y: 174 },
            { x: 182, y: 157 },
            { x: 171, y: 146 },
            { x: 154, y: 163 }
        ]
    },
    {
        teamId: 2,
        label: "Team 2",
        color: "#F87171",
        labelSegment: [2, 3],
        points: [
            { x: 84, y: 93 },
            { x: 101, y: 76 },
            { x: 90, y: 65 },
            { x: 73, y: 82 }
        ]
    }
];

export function initializeSpawnPlayback(canvas, replay) {
    const state = {
        replay,
        resizeObserver: null,
        currentGameloop: 0,
        staticGeometry: null,
        staticCanvasWidth: 0,
        staticCanvasHeight: 0
    };

    const oldState = states.get(canvas);
    if (oldState?.resizeObserver) {
        oldState.resizeObserver.disconnect();
    }

    state.resizeObserver = new ResizeObserver(() => drawSpawnPlayback(canvas, state.currentGameloop));
    state.resizeObserver.observe(canvas);
    states.set(canvas, state);
    resizeCanvas(canvas);
}

export function drawSpawnPlayback(canvas, currentGameloop) {
    const state = states.get(canvas);
    if (!state?.replay) {
        return;
    }

    const resized = resizeCanvas(canvas);
    const ctx = canvas.getContext("2d");
    const replay = state.replay;
    const bounds = replay.bounds ?? replay.Bounds;
    if (!bounds) {
        return;
    }

    state.currentGameloop = currentGameloop;
    const stepGameloops = replay.stepGameloops ?? replay.StepGameloops ?? 112;
    const players = replay.players ?? replay.Players ?? [];

    if (resized
        || !state.staticGeometry
        || state.staticCanvasWidth !== canvas.width
        || state.staticCanvasHeight !== canvas.height) {
        state.staticGeometry = createStaticGeometry(replay, bounds, canvas);
        state.staticCanvasWidth = canvas.width;
        state.staticCanvasHeight = canvas.height;
    }

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawStaticMapLayer(ctx, canvas, state.staticGeometry, currentGameloop);
    const drawnUnits = drawUnitLayer(ctx, canvas, bounds, players, currentGameloop, stepGameloops);

    if (drawnUnits === 0) {
        drawEmptyState(ctx, canvas);
    }
}

function resizeCanvas(canvas) {
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

function createStaticGeometry(replay, bounds, canvas) {
    const landmarks = replay.landmarks ?? replay.Landmarks ?? [];
    const players = replay.players ?? replay.Players ?? [];
    const spawnAreas = createSpawnAreas(bounds, canvas);

    return {
        gridLines: createGridLines(bounds, canvas),
        middleLine: createMiddleLine(bounds, canvas),
        middleControl: normalizeMiddleControl(replay),
        spawnAreas,
        playerGasBadges: createPlayerGasBadges(players, spawnAreas, canvas),
        landmarks: landmarks.map(landmark => normalizeLandmark(landmark, bounds, canvas))
    };
}

function createGridLines(bounds, canvas) {
    const normalizedBounds = normalizeBounds(bounds);
    const lines = [];
    const sumMin = normalizedBounds.minX + normalizedBounds.minY;
    const sumMax = normalizedBounds.maxX + normalizedBounds.maxY;
    const diffMin = normalizedBounds.minX - normalizedBounds.maxY;
    const diffMax = normalizedBounds.maxX - normalizedBounds.minY;

    for (let sum = roundUpToInterval(sumMin, GRID_INTERVAL); sum <= sumMax; sum += GRID_INTERVAL) {
        if (Math.abs(sum - MAP_CENTER_SUM) < 0.001) {
            continue;
        }

        const segment = clipSumLine(normalizedBounds, sum);
        if (segment) {
            lines.push(projectSegment(segment, bounds, canvas));
        }
    }

    for (let diff = roundUpToInterval(diffMin, GRID_INTERVAL); diff <= diffMax; diff += GRID_INTERVAL) {
        const segment = clipDiffLine(normalizedBounds, diff);
        if (segment) {
            lines.push(projectSegment(segment, bounds, canvas));
        }
    }

    return lines;
}

function createMiddleLine(bounds, canvas) {
    const segment = clipSumLine(normalizeBounds(bounds), MAP_CENTER_SUM);
    return segment ? projectSegment(segment, bounds, canvas) : null;
}

function createSpawnAreas(bounds, canvas) {
    return TEAM_SPAWN_AREAS.map(area => {
        const points = area.points.map(point => project(point.x, point.y, bounds, canvas));
        return {
            teamId: area.teamId,
            label: area.label,
            color: area.color,
            points,
            labelGeometry: createSpawnAreaLabelGeometry(area, points, canvas)
        };
    });
}

function normalizeLandmark(landmark, bounds, canvas) {
    const x = landmark.x ?? landmark.X;
    const y = landmark.y ?? landmark.Y;
    const kind = landmark.kind ?? landmark.Kind ?? "Defense";
    const teamId = landmark.teamId ?? landmark.TeamId;
    const color = landmark.color ?? landmark.Color ?? (teamId === 1 ? "#5DADEC" : "#F87171");
    const kills = landmark.kills ?? landmark.Kills ?? 0;
    const name = landmark.name ?? landmark.Name ?? kind;
    const radius = Math.max(7, (landmark.radius ?? landmark.Radius ?? 10) * deviceScale(canvas) * 0.7);
    const diedGameloop = landmark.diedGameloop ?? landmark.DiedGameloop ?? null;

    return {
        x,
        y,
        kind,
        teamId,
        color,
        kills,
        label: name,
        radius,
        diedGameloop,
        projected: x == null || y == null ? null : project(x, y, bounds, canvas)
    };
}

function normalizeMiddleControl(replay) {
    const middleControl = replay.middleControl ?? replay.MiddleControl;
    const firstTeamId = middleControl?.firstTeamId ?? middleControl?.FirstTeamId ?? 0;
    const rawChangeGameloops = middleControl?.changeGameloops ?? middleControl?.ChangeGameloops ?? [];
    const changeGameloops = firstTeamId === 1 || firstTeamId === 2
        ? rawChangeGameloops.filter(gameloop => Number.isFinite(gameloop))
        : [];

    return {
        firstTeamId: changeGameloops.length > 0 ? firstTeamId : 0,
        changeGameloops
    };
}

function createSpawnAreaLabelGeometry(area, points, canvas) {
    if (points.length === 0) {
        return null;
    }

    const startIndex = area.labelSegment[0];
    const endIndex = area.labelSegment[1];
    const start = points[startIndex];
    const end = points[endIndex];
    if (!start || !end) {
        return null;
    }

    const midpoint = {
        x: (start.x + end.x) / 2,
        y: (start.y + end.y) / 2
    };
    const centroid = getCentroid(points);
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    const length = Math.sqrt((dx * dx) + (dy * dy));
    if (length <= 0) {
        return null;
    }

    let normalX = -dy / length;
    let normalY = dx / length;
    const outwardX = midpoint.x - centroid.x;
    const outwardY = midpoint.y - centroid.y;
    if ((normalX * outwardX) + (normalY * outwardY) < 0) {
        normalX = -normalX;
        normalY = -normalY;
    }
    if (area.labelSide === -1) {
        normalX = -normalX;
        normalY = -normalY;
    }

    let angle = Math.atan2(dy, dx);
    if (angle > Math.PI / 2 || angle < -Math.PI / 2) {
        angle += Math.PI;
    }

    const offset = 12 * deviceScale(canvas);
    return {
        x: midpoint.x + normalX * offset,
        y: midpoint.y + normalY * offset,
        angle
    };
}

function getCentroid(points) {
    let x = 0;
    let y = 0;
    for (const point of points) {
        x += point.x;
        y += point.y;
    }

    return {
        x: x / points.length,
        y: y / points.length
    };
}

function createPlayerGasBadges(players, spawnAreas, canvas) {
    const badges = [];
    const scale = deviceScale(canvas);
    const width = GAS_BADGE_WIDTH * scale;
    const height = GAS_BADGE_HEIGHT * scale;
    const gap = GAS_BADGE_GAP * scale;
    const padding = GAS_BADGE_CORNER_PADDING * scale;

    for (const spawnArea of spawnAreas) {
        const teamPlayers = players
            .filter(player => (player.teamId ?? player.TeamId) === spawnArea.teamId)
            .sort((left, right) => (left.gamePos ?? left.GamePos ?? 0) - (right.gamePos ?? right.GamePos ?? 0));
        if (teamPlayers.length === 0) {
            continue;
        }

        const totalWidth = teamPlayers.length * width + Math.max(0, teamPlayers.length - 1) * gap;
        const startX = spawnArea.teamId === 1
            ? canvas.width - padding - totalWidth + width / 2
            : padding + width / 2;
        const y = spawnArea.teamId === 1
            ? padding + height / 2
            : canvas.height - padding - height / 2;

        for (let i = 0; i < teamPlayers.length; i++) {
            const player = teamPlayers[i];
            badges.push({
                x: startX + i * (width + gap),
                y,
                gamePos: player.gamePos ?? player.GamePos ?? 0,
                color: spawnArea.color,
                refineryGameloops: normalizeRefineryGameloops(player)
            });
        }
    }

    return badges;
}

function normalizeRefineryGameloops(player) {
    const refineryGameloops = player.refineryGameloops ?? player.RefineryGameloops ?? [];
    return refineryGameloops
        .filter(gameloop => Number.isFinite(gameloop))
        .sort((left, right) => left - right);
}

function drawStaticMapLayer(ctx, canvas, geometry, currentGameloop) {
    ctx.save();
    ctx.fillStyle = "#071015";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.restore();

    drawGrid(ctx, canvas, geometry.gridLines);
    drawSpawnAreas(ctx, canvas, geometry.spawnAreas);
    drawPlayerGasBadges(ctx, canvas, geometry.playerGasBadges, currentGameloop);
    drawMiddleLine(ctx, canvas, geometry.middleLine, geometry.middleControl, currentGameloop);

    for (const landmark of geometry.landmarks) {
        if (landmark.diedGameloop != null && landmark.diedGameloop <= currentGameloop) {
            continue;
        }

        drawLandmark(ctx, canvas, landmark);
    }
}

function drawGrid(ctx, canvas, gridLines) {
    ctx.save();
    ctx.strokeStyle = "rgba(255, 255, 255, 0.10)";
    ctx.lineWidth = Math.max(1, deviceScale(canvas));

    for (const line of gridLines) {
        ctx.beginPath();
        ctx.moveTo(line.start.x, line.start.y);
        ctx.lineTo(line.end.x, line.end.y);
        ctx.stroke();
    }

    ctx.restore();
}

function drawSpawnAreas(ctx, canvas, spawnAreas) {
    ctx.save();
    ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
    ctx.setLineDash([6 * deviceScale(canvas), 4 * deviceScale(canvas)]);

    for (const area of spawnAreas) {
        if (area.points.length === 0) {
            continue;
        }

        ctx.fillStyle = withAlpha(area.color, "20");
        ctx.strokeStyle = withAlpha(area.color, "CC");
        ctx.beginPath();
        ctx.moveTo(area.points[0].x, area.points[0].y);
        for (let i = 1; i < area.points.length; i++) {
            ctx.lineTo(area.points[i].x, area.points[i].y);
        }
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    }

    ctx.restore();

    drawSpawnAreaLabels(ctx, canvas, spawnAreas);
}

function drawSpawnAreaLabels(ctx, canvas, spawnAreas) {
    ctx.save();
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.font = `${Math.max(11, 11 * deviceScale(canvas))}px sans-serif`;

    for (const area of spawnAreas) {
        const label = area.labelGeometry;
        if (!label) {
            continue;
        }

        ctx.save();
        ctx.translate(label.x, label.y);
        ctx.rotate(label.angle);
        ctx.lineWidth = Math.max(3, deviceScale(canvas) * 3);
        ctx.strokeStyle = "rgba(7, 16, 21, 0.92)";
        ctx.fillStyle = withAlpha(area.color, "EE");
        ctx.strokeText(area.label, 0, 0);
        ctx.fillText(area.label, 0, 0);
        ctx.restore();
    }

    ctx.restore();
}

function drawPlayerGasBadges(ctx, canvas, badges, currentGameloop) {
    if (badges.length === 0) {
        return;
    }

    const scale = deviceScale(canvas);
    const width = GAS_BADGE_WIDTH * scale;
    const height = GAS_BADGE_HEIGHT * scale;
    const radius = 6 * scale;
    const dotRadius = 3.5 * scale;
    const dotGap = 8 * scale;

    ctx.save();
    ctx.font = `${Math.max(10, 10 * scale)}px sans-serif`;
    ctx.textAlign = "left";
    ctx.textBaseline = "middle";

    for (const badge of badges) {
        const gasCount = getGasCountAtGameloop(badge.refineryGameloops, currentGameloop);
        const x = badge.x - width / 2;
        const y = badge.y - height / 2;

        ctx.fillStyle = "rgba(7, 16, 21, 0.82)";
        ctx.strokeStyle = withAlpha(badge.color, "DD");
        ctx.lineWidth = Math.max(1.25, 1.25 * scale);
        drawRoundedRect(ctx, x, y, width, height, radius);
        ctx.fill();
        ctx.stroke();

        ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
        ctx.fillText(`P${badge.gamePos}`, x + 7 * scale, badge.y);

        if (gasCount === 0) {
            ctx.textAlign = "right";
            ctx.fillStyle = "rgba(255, 255, 255, 0.66)";
            ctx.fillText("0", x + width - 8 * scale, badge.y);
            ctx.textAlign = "left";
            continue;
        }

        const firstDotX = x + width - 8 * scale - (gasCount - 1) * dotGap;
        for (let i = 0; i < gasCount; i++) {
            const dotX = firstDotX + i * dotGap;
            ctx.beginPath();
            ctx.arc(dotX, badge.y, dotRadius, 0, Math.PI * 2);
            ctx.fillStyle = "#1D72F3";
            ctx.fill();
            ctx.lineWidth = Math.max(1.25, 1.25 * scale);
            ctx.strokeStyle = "rgba(255, 255, 255, 0.82)";
            ctx.stroke();
        }
    }

    ctx.restore();
}

function getGasCountAtGameloop(refineryGameloops, currentGameloop) {
    let gasCount = 0;
    while (gasCount < refineryGameloops.length && refineryGameloops[gasCount] <= currentGameloop) {
        gasCount++;
    }

    return gasCount;
}

function drawMiddleLine(ctx, canvas, middleLine, middleControl, currentGameloop) {
    if (!middleLine) {
        return;
    }

    ctx.save();
    ctx.strokeStyle = getMiddleLineColor(middleControl, currentGameloop);
    ctx.lineWidth = Math.max(2, deviceScale(canvas) * 2);
    ctx.setLineDash([10 * deviceScale(canvas), 7 * deviceScale(canvas)]);
    ctx.beginPath();
    ctx.moveTo(middleLine.start.x, middleLine.start.y);
    ctx.lineTo(middleLine.end.x, middleLine.end.y);
    ctx.stroke();
    ctx.restore();
}

function getMiddleLineColor(middleControl, currentGameloop) {
    if (!middleControl || middleControl.firstTeamId !== 1 && middleControl.firstTeamId !== 2) {
        return NEUTRAL_MIDDLE_LINE_COLOR;
    }

    const changeGameloops = middleControl.changeGameloops;
    let reachedChanges = 0;
    while (reachedChanges < changeGameloops.length && currentGameloop >= changeGameloops[reachedChanges]) {
        reachedChanges++;
    }

    if (reachedChanges === 0) {
        return NEUTRAL_MIDDLE_LINE_COLOR;
    }

    const teamId = reachedChanges % 2 === 1
        ? middleControl.firstTeamId
        : getOtherTeamId(middleControl.firstTeamId);
    return withAlpha(TEAM_COLORS[teamId], "CC");
}

function getOtherTeamId(teamId) {
    return teamId === 1 ? 2 : 1;
}

function drawLandmark(ctx, canvas, landmark) {
    if (!landmark.projected) {
        return;
    }

    const projected = landmark.projected;
    const radius = landmark.radius;

    ctx.save();
    ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
    ctx.strokeStyle = withAlpha(landmark.color, "EE");
    ctx.fillStyle = withAlpha(landmark.color, landmark.kind === "Base" ? "44" : "55");

    if (landmark.kind === "Base") {
        ctx.beginPath();
        ctx.moveTo(projected.x, projected.y - radius);
        ctx.lineTo(projected.x + radius, projected.y);
        ctx.lineTo(projected.x, projected.y + radius);
        ctx.lineTo(projected.x - radius, projected.y);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    } else {
        ctx.beginPath();
        ctx.arc(projected.x, projected.y, radius, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(projected.x - radius * 0.75, projected.y);
        ctx.lineTo(projected.x + radius * 0.75, projected.y);
        ctx.moveTo(projected.x, projected.y - radius * 0.75);
        ctx.lineTo(projected.x, projected.y + radius * 0.75);
        ctx.stroke();
    }

    ctx.fillStyle = "rgba(255, 255, 255, 0.82)";
    ctx.font = `${Math.max(10, 10 * deviceScale(canvas))}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "top";
    ctx.fillText(landmark.label, projected.x, projected.y + radius + 3 * deviceScale(canvas));
    ctx.restore();
}

function drawUnitLayer(ctx, canvas, bounds, players, currentGameloop, stepGameloops) {
    let drawnUnits = 0;
    for (const player of players) {
        const units = player.units ?? player.Units ?? [];
        const teamId = player.teamId ?? player.TeamId;
        for (const unit of units) {
            if (drawUnit(ctx, canvas, bounds, unit, teamId, currentGameloop, stepGameloops)) {
                drawnUnits++;
            }
        }
    }

    return drawnUnits;
}

function drawUnit(ctx, canvas, bounds, unit, teamId, currentGameloop, stepGameloops) {
    const spawnGameloop = unit.spawnGameloop ?? unit.SpawnGameloop;
    if (currentGameloop < spawnGameloop) {
        return false;
    }

    const diedGameloop = unit.diedGameloop ?? unit.DiedGameloop;
    if (diedGameloop != null && diedGameloop <= currentGameloop) {
        return false;
    }

    const position = getUnitPosition(unit, currentGameloop, diedGameloop);
    const projected = project(position.x, position.y, bounds, canvas);
    const radius = Math.max(3, (unit.radius ?? unit.Radius ?? 8) * deviceScale(canvas) * 0.55);
    const color = unit.color ?? unit.Color ?? "#EC7063";

    ctx.save();
    ctx.globalAlpha = teamId === 1 ? 0.92 : 0.78;
    ctx.fillStyle = withAlpha(color, "99");
    ctx.strokeStyle = withAlpha(color, "EE");
    ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
    ctx.beginPath();
    ctx.arc(projected.x, projected.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    ctx.restore();
    return true;
}

function drawEmptyState(ctx, canvas) {
    ctx.save();
    ctx.fillStyle = "rgba(255, 255, 255, 0.72)";
    ctx.font = `${Math.max(14, 14 * deviceScale(canvas))}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText("No active units at this step", canvas.width / 2, canvas.height / 2);
    ctx.restore();
}

function getUnitPosition(unit, currentGameloop, diedGameloop) {
    const spawnGameloop = unit.spawnGameloop ?? unit.SpawnGameloop;
    const spawnX = unit.spawnX ?? unit.SpawnX;
    const spawnY = unit.spawnY ?? unit.SpawnY;
    const targetX = unit.targetX ?? unit.TargetX;
    const targetY = unit.targetY ?? unit.TargetY;
    const endGameloop = diedGameloop ?? Math.max(spawnGameloop + 1, currentGameloop + 224);
    const progress = clamp((currentGameloop - spawnGameloop) / Math.max(1, endGameloop - spawnGameloop), 0, 1);

    return {
        x: lerp(spawnX, targetX, progress),
        y: lerp(spawnY, targetY, progress)
    };
}

function normalizeBounds(bounds) {
    return {
        minX: bounds.minX ?? bounds.MinX,
        minY: bounds.minY ?? bounds.MinY,
        maxX: bounds.maxX ?? bounds.MaxX,
        maxY: bounds.maxY ?? bounds.MaxY
    };
}

function projectSegment(segment, bounds, canvas) {
    return {
        start: project(segment.start.x, segment.start.y, bounds, canvas),
        end: project(segment.end.x, segment.end.y, bounds, canvas)
    };
}

function clipSumLine(bounds, sum) {
    return createSegmentFromIntersections([
        { x: bounds.minX, y: sum - bounds.minX },
        { x: bounds.maxX, y: sum - bounds.maxX },
        { x: sum - bounds.minY, y: bounds.minY },
        { x: sum - bounds.maxY, y: bounds.maxY }
    ], bounds);
}

function clipDiffLine(bounds, diff) {
    return createSegmentFromIntersections([
        { x: bounds.minX, y: bounds.minX - diff },
        { x: bounds.maxX, y: bounds.maxX - diff },
        { x: bounds.minY + diff, y: bounds.minY },
        { x: bounds.maxY + diff, y: bounds.maxY }
    ], bounds);
}

function createSegmentFromIntersections(candidates, bounds) {
    const points = [];
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

function isPointInBounds(point, bounds) {
    const epsilon = 0.001;
    return point.x >= bounds.minX - epsilon
        && point.x <= bounds.maxX + epsilon
        && point.y >= bounds.minY - epsilon
        && point.y <= bounds.maxY + epsilon;
}

function containsPoint(points, point) {
    return points.some(existing => Math.abs(existing.x - point.x) < 0.001 && Math.abs(existing.y - point.y) < 0.001);
}

function distanceSquared(left, right) {
    const x = left.x - right.x;
    const y = left.y - right.y;
    return (x * x) + (y * y);
}

function roundUpToInterval(value, interval) {
    return Math.ceil(value / interval) * interval;
}

function drawRoundedRect(ctx, x, y, width, height, radius) {
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

function project(x, y, bounds, canvas) {
    const minX = bounds.minX ?? bounds.MinX;
    const minY = bounds.minY ?? bounds.MinY;
    const maxX = bounds.maxX ?? bounds.MaxX;
    const maxY = bounds.maxY ?? bounds.MaxY;
    const padding = 24 * deviceScale(canvas);
    const width = Math.max(1, maxX - minX);
    const height = Math.max(1, maxY - minY);

    return {
        x: padding + ((x - minX) / width) * (canvas.width - padding * 2),
        y: canvas.height - padding - ((y - minY) / height) * (canvas.height - padding * 2)
    };
}

function deviceScale(canvas) {
    return canvas.width / Math.max(1, canvas.clientWidth);
}

function lerp(start, end, amount) {
    return start + (end - start) * amount;
}

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}

function withAlpha(color, alpha) {
    if (typeof color === "string" && color.startsWith("#") && color.length === 7) {
        return `${color}${alpha}`;
    }

    return color;
}
