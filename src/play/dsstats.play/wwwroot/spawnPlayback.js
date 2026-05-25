// TypeScript/constants.ts
var MAP_WIDTH = 256;
var MAP_HEIGHT = 240;
var MAP_CENTER_SUM = MAP_WIDTH / 2 + MAP_HEIGHT / 2;
var GRID_INTERVAL = 16;
var NEUTRAL_MIDDLE_LINE_COLOR = "rgba(255, 193, 7, 0.70)";
var GAS_BADGE_WIDTH = 94;
var GAS_BADGE_HEIGHT = 24;
var GAS_BADGE_GAP = 8;
var GAS_BADGE_CORNER_PADDING = 20;
var MAX_UNIT_LIFETIME_GAMELOOPS = 2096;
var MIN_CATALOG_ICON_CSS_SIZE = 18;
var TEAM_COLORS = {
  1: "#5DADEC",
  2: "#F87171"
};
var TEAM_SPAWN_AREAS = [
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

// TypeScript/normalization.ts
function normalizeReplay(replayValue) {
  const replay = asObject(replayValue);
  const bounds = normalizeBounds(readObject(replay, "bounds", "Bounds"));
  const rawPlayers = readArray(replay, "players", "Players");
  const players = [];
  const units = [];
  for (const rawPlayerValue of rawPlayers) {
    const rawPlayer = asObject(rawPlayerValue);
    const player = {
      name: readString(rawPlayer, "name", "Name"),
      teamId: readNumber(rawPlayer, "teamId", "TeamId"),
      gamePos: readNumber(rawPlayer, "gamePos", "GamePos"),
      commander: readString(rawPlayer, "commander", "Commander"),
      refineryGameloops: normalizeRefineryGameloops(rawPlayer),
      tierUpgradeGameloops: normalizeTierUpgradeGameloops(rawPlayer),
      units: []
    };
    const rawUnits = readArray(rawPlayer, "units", "Units");
    for (const rawUnitValue of rawUnits) {
      const rawUnit = asObject(rawUnitValue);
      const name = readString(rawUnit, "name", "Name");
      const spawnGameloop = readNumber(rawUnit, "spawnGameloop", "SpawnGameloop");
      const expiresGameloop = readOptionalNumber(rawUnit, "expiresGameloop", "ExpiresGameloop") ?? spawnGameloop + MAX_UNIT_LIFETIME_GAMELOOPS;
      const spawnX = readNumber(rawUnit, "spawnX", "SpawnX");
      const spawnY = readNumber(rawUnit, "spawnY", "SpawnY");
      const targetX = readOptionalNumber(rawUnit, "targetX", "TargetX") ?? spawnX;
      const targetY = readOptionalNumber(rawUnit, "targetY", "TargetY") ?? spawnY;
      const unit = {
        name,
        commander: player.commander,
        aliveUnitHighlightKey: createAliveUnitHighlightKey(player.teamId, player.commander, name),
        spawnGameloop,
        expiresGameloop,
        spawnX,
        spawnY,
        deltaX: targetX - spawnX,
        deltaY: targetY - spawnY,
        inverseLifetime: 1 / Math.max(1, expiresGameloop - spawnGameloop),
        radius: readOptionalNumber(rawUnit, "radius", "Radius") ?? 8,
        color: readString(rawUnit, "color", "Color", "#EC7063"),
        teamId: player.teamId,
        iconDefinition: null,
        iconResolved: false,
        render: null
      };
      player.units.push(unit);
      units.push(unit);
    }
    players.push(player);
  }
  units.sort((left, right) => left.spawnGameloop - right.spawnGameloop || left.expiresGameloop - right.expiresGameloop);
  return {
    durationGameloop: readNumber(replay, "durationGameloop", "DurationGameloop"),
    stepGameloops: readOptionalNumber(replay, "stepGameloops", "StepGameloops") ?? 112,
    bounds,
    stats: replay.stats ?? replay.Stats,
    summary: normalizeSummary(replay),
    middleControl: normalizeMiddleControl(replay),
    landmarks: readArray(replay, "landmarks", "Landmarks").map(asObject),
    buildUnits: readArray(replay, "buildUnits", "BuildUnits"),
    snapshots: readArray(replay, "snapshots", "Snapshots"),
    players,
    units
  };
}
function createAliveUnitHighlightKey(teamId, commander, unitName) {
  return `${teamId}|${commander.length}:${commander}|${unitName.length}:${unitName}`;
}
function normalizeSummary(replayValue) {
  const replay = asObject(replayValue);
  const summary = asObject(replay.summary ?? replay.Summary);
  const players = readArray(summary, "players", "Players").map(normalizePlayerSummary);
  const topUnits = readArray(summary, "topUnits", "TopUnits").map(normalizeTopUnitSummary);
  return {
    totalKills: readNumber(summary, "totalKills", "TotalKills"),
    players,
    topUnits
  };
}
function normalizePlayerSummary(value) {
  const row = asObject(value);
  return {
    playerName: readString(row, "playerName", "PlayerName"),
    teamId: readNumber(row, "teamId", "TeamId"),
    gamePos: readNumber(row, "gamePos", "GamePos"),
    commander: readString(row, "commander", "Commander"),
    kills: readNumber(row, "kills", "Kills")
  };
}
function normalizeTopUnitSummary(value) {
  const row = asObject(value);
  return {
    playerName: readString(row, "playerName", "PlayerName"),
    teamId: readNumber(row, "teamId", "TeamId"),
    gamePos: readNumber(row, "gamePos", "GamePos"),
    unitName: readString(row, "unitName", "UnitName"),
    kills: readNumber(row, "kills", "Kills")
  };
}
function normalizeBounds(boundsValue) {
  const bounds = asObject(boundsValue);
  return {
    minX: readOptionalNumber(bounds, "minX", "MinX") ?? 0,
    minY: readOptionalNumber(bounds, "minY", "MinY") ?? 0,
    maxX: readOptionalNumber(bounds, "maxX", "MaxX") ?? MAP_WIDTH,
    maxY: readOptionalNumber(bounds, "maxY", "MaxY") ?? MAP_HEIGHT
  };
}
function normalizeMiddleControl(replayValue) {
  const replay = asObject(replayValue);
  const middleControl = asObject(replay.middleControl ?? replay.MiddleControl);
  const firstTeamId = readNumber(middleControl, "firstTeamId", "FirstTeamId");
  const rawChangeGameloops = readArray(middleControl, "changeGameloops", "ChangeGameloops");
  const changeGameloops = firstTeamId === 1 || firstTeamId === 2 ? rawChangeGameloops.filter(isFiniteNumber) : [];
  return {
    firstTeamId: changeGameloops.length > 0 ? firstTeamId : 0,
    changeGameloops
  };
}
function normalizeRefineryGameloops(playerValue) {
  const player = asObject(playerValue);
  return readArray(player, "refineryGameloops", "RefineryGameloops").filter(isFiniteNumber).sort(compareNumber);
}
function normalizeTierUpgradeGameloops(playerValue) {
  const player = asObject(playerValue);
  return readArray(player, "tierUpgradeGameloops", "TierUpgradeGameloops").filter(isFiniteNumber).sort(compareNumber);
}
function readString(record, camelName, pascalName, fallback = "") {
  const value = record[camelName] ?? record[pascalName];
  return typeof value === "string" ? value : fallback;
}
function readNumber(record, camelName, pascalName, fallback = 0) {
  const value = readOptionalNumber(record, camelName, pascalName);
  return value ?? fallback;
}
function readOptionalNumber(record, camelName, pascalName) {
  const value = record[camelName] ?? record[pascalName];
  return isFiniteNumber(value) ? value : null;
}
function readArray(record, camelName, pascalName) {
  const value = record[camelName] ?? record[pascalName];
  return Array.isArray(value) ? value : [];
}
function readObject(record, camelName, pascalName) {
  return asObject(record[camelName] ?? record[pascalName]);
}
function asObject(value) {
  return value !== null && typeof value === "object" ? value : {};
}
function isFiniteNumber(value) {
  return typeof value === "number" && Number.isFinite(value);
}
function compareNumber(left, right) {
  return left - right;
}

// TypeScript/canvasUtils.ts
function createLayerCanvas(width, height) {
  if (typeof OffscreenCanvas !== "undefined") {
    return new OffscreenCanvas(width, height);
  }
  const layer = document.createElement("canvas");
  layer.width = width;
  layer.height = height;
  return layer;
}
function getCanvasContext(canvas) {
  return canvas.getContext("2d");
}
function resizeCanvas(canvas, source = "unknown") {
  const width = Math.max(320, Math.floor(canvas.clientWidth));
  const height = Math.max(240, Math.floor(canvas.clientHeight));
  const scale = window.devicePixelRatio || 1;
  const targetWidth = Math.floor(width * scale);
  const targetHeight = Math.floor(height * scale);
  const oldWidth = canvas.width;
  const oldHeight = canvas.height;
  const resized = oldWidth !== targetWidth || oldHeight !== targetHeight;
  if (resized) {
    canvas.width = targetWidth;
    canvas.height = targetHeight;
  }
  console.log(
    `spawnPlayback resizeCanvas source=${source} resized=${resized} client=${canvas.clientWidth}x${canvas.clientHeight} target=${targetWidth}x${targetHeight} old=${oldWidth}x${oldHeight} scale=${scale.toFixed(2)} contains=${document.contains(canvas)} - ${Date.now()}`
  );
  return resized;
}
function deviceScale(canvas) {
  return canvas.width / Math.max(1, canvas.clientWidth);
}
function createProjection(bounds, canvas) {
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
function projectX(projection, x) {
  return projection.left + (x - projection.minX) * projection.scaleX;
}
function projectY(projection, y) {
  return projection.bottom - (y - projection.minY) * projection.scaleY;
}
function project(x, y, bounds, canvas) {
  const padding = 24 * deviceScale(canvas);
  const width = Math.max(1, bounds.maxX - bounds.minX);
  const height = Math.max(1, bounds.maxY - bounds.minY);
  return {
    x: padding + (x - bounds.minX) / width * (canvas.width - padding * 2),
    y: canvas.height - padding - (y - bounds.minY) / height * (canvas.height - padding * 2)
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
function isPointInBounds(point, bounds) {
  const epsilon = 1e-3;
  return point.x >= bounds.minX - epsilon && point.x <= bounds.maxX + epsilon && point.y >= bounds.minY - epsilon && point.y <= bounds.maxY + epsilon;
}
function containsPoint(points, point) {
  return points.some((existing) => Math.abs(existing.x - point.x) < 1e-3 && Math.abs(existing.y - point.y) < 1e-3);
}
function distanceSquared(left, right) {
  const x = left.x - right.x;
  const y = left.y - right.y;
  return x * x + y * y;
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
function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}
function withAlpha(color, alpha) {
  if (color.startsWith("#") && color.length === 7) {
    return `${color}${alpha}`;
  }
  return color;
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

// TypeScript/geometry.ts
function createRenderCache(bounds, canvas) {
  return {
    projection: createProjection(bounds, canvas)
  };
}
function createStaticGeometry(replay, bounds, canvas) {
  const spawnAreas = createSpawnAreas(bounds, canvas);
  return {
    gridLines: createGridLines(bounds, canvas),
    middleLine: createMiddleLine(bounds, canvas),
    middleControl: normalizeMiddleControl(replay),
    spawnAreas,
    playerGasBadges: createPlayerGasBadges(replay.players, spawnAreas, canvas),
    landmarks: replay.landmarks.map((landmark) => normalizeLandmark(landmark, bounds, canvas))
  };
}
function createGridLines(bounds, canvas) {
  const lines = [];
  const sumMin = bounds.minX + bounds.minY;
  const sumMax = bounds.maxX + bounds.maxY;
  const diffMin = bounds.minX - bounds.maxY;
  const diffMax = bounds.maxX - bounds.minY;
  for (let sum = roundUpToInterval(sumMin, GRID_INTERVAL); sum <= sumMax; sum += GRID_INTERVAL) {
    if (Math.abs(sum - MAP_CENTER_SUM) < 1e-3) {
      continue;
    }
    const segment = clipSumLine(bounds, sum);
    if (segment) {
      lines.push(projectSegment(segment, bounds, canvas));
    }
  }
  for (let diff = roundUpToInterval(diffMin, GRID_INTERVAL); diff <= diffMax; diff += GRID_INTERVAL) {
    const segment = clipDiffLine(bounds, diff);
    if (segment) {
      lines.push(projectSegment(segment, bounds, canvas));
    }
  }
  return lines;
}
function createMiddleLine(bounds, canvas) {
  const segment = clipSumLine(bounds, MAP_CENTER_SUM);
  return segment ? shortenSegment(projectSegment(segment, bounds, canvas), 1 / 3) : null;
}
function shortenSegment(segment, fraction) {
  const clampedFraction = Math.max(0, Math.min(1, fraction));
  const midX = (segment.start.x + segment.end.x) / 2;
  const midY = (segment.start.y + segment.end.y) / 2;
  const halfX = (segment.end.x - segment.start.x) * clampedFraction / 2;
  const halfY = (segment.end.y - segment.start.y) * clampedFraction / 2;
  return {
    start: {
      x: midX - halfX,
      y: midY - halfY
    },
    end: {
      x: midX + halfX,
      y: midY + halfY
    }
  };
}
function createSpawnAreas(bounds, canvas) {
  return TEAM_SPAWN_AREAS.map((area) => {
    const points = area.points.map((point) => project(point.x, point.y, bounds, canvas));
    return {
      teamId: area.teamId,
      label: area.label,
      color: area.color,
      points,
      labelGeometry: createSpawnAreaLabelGeometry(area, points, canvas)
    };
  });
}
function normalizeLandmark(landmarkValue, bounds, canvas) {
  const landmark = asObject(landmarkValue);
  const x = readOptionalNumber(landmark, "x", "X");
  const y = readOptionalNumber(landmark, "y", "Y");
  const kind = readString(landmark, "kind", "Kind", "Defense");
  const teamId = readNumber(landmark, "teamId", "TeamId");
  const color = readString(landmark, "color", "Color", teamId === 1 ? "#5DADEC" : "#F87171");
  const kills = readNumber(landmark, "kills", "Kills");
  const label = readString(landmark, "name", "Name", kind);
  const radius = Math.max(7, readNumber(landmark, "radius", "Radius", 10) * deviceScale(canvas) * 0.7);
  const diedGameloop = readOptionalNumber(landmark, "diedGameloop", "DiedGameloop");
  return {
    x,
    y,
    kind,
    teamId,
    color,
    kills,
    label,
    radius,
    diedGameloop,
    projected: x == null || y == null ? null : project(x, y, bounds, canvas)
  };
}
function createSpawnAreaLabelGeometry(area, points, canvas) {
  if (points.length === 0) {
    return null;
  }
  const start = points[area.labelSegment[0]];
  const end = points[area.labelSegment[1]];
  if (!start || !end) {
    return null;
  }
  const midpointX = (start.x + end.x) / 2;
  const midpointY = (start.y + end.y) / 2;
  const centroid = getCentroid(points);
  const dx = end.x - start.x;
  const dy = end.y - start.y;
  const length = Math.sqrt(dx * dx + dy * dy);
  if (length <= 0) {
    return null;
  }
  let normalX = -dy / length;
  let normalY = dx / length;
  const outwardX = midpointX - centroid.x;
  const outwardY = midpointY - centroid.y;
  if (normalX * outwardX + normalY * outwardY < 0) {
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
    x: midpointX + normalX * offset,
    y: midpointY + normalY * offset,
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
    const teamPlayers = players.filter((player) => player.teamId === spawnArea.teamId).sort((left, right) => left.gamePos - right.gamePos);
    if (teamPlayers.length === 0) {
      continue;
    }
    const totalWidth = teamPlayers.length * width + Math.max(0, teamPlayers.length - 1) * gap;
    const startX = spawnArea.teamId === 1 ? canvas.width - padding - totalWidth + width / 2 : padding + width / 2;
    const y = spawnArea.teamId === 1 ? padding + height / 2 : canvas.height - padding - height / 2;
    for (let i = 0; i < teamPlayers.length; i++) {
      const player = teamPlayers[i];
      badges.push({
        x: startX + i * (width + gap),
        y,
        gamePos: player.gamePos,
        color: spawnArea.color,
        refineryGameloops: normalizeRefineryGameloops(player),
        tierUpgradeGameloops: normalizeTierUpgradeGameloops(player)
      });
    }
  }
  return badges;
}

// TypeScript/objectiveIcons.ts
var spriteCache = /* @__PURE__ */ new Map();
var objectiveIconCatalog = {
  getSize(name, kind, radius, scale) {
    return isLargeObjective(name, kind) ? Math.max(30 * scale, radius * 3.2) : Math.max(20 * scale, radius * 2.5);
  },
  render(ctx, options) {
    const objectiveKind = resolveObjectiveKind(options.name, options.kind);
    if (!objectiveKind) {
      return false;
    }
    const sprite = getObjectiveSprite(objectiveKind, options.teamColor, options.size);
    ctx.drawImage(sprite, options.x - sprite.width / 2, options.y - sprite.height / 2);
    return true;
  }
};
function getObjectiveSprite(kind, teamColor, size) {
  const pixelSize = Math.max(1, Math.ceil(size));
  const key = `${kind}|${teamColor}|${pixelSize}`;
  const cached = spriteCache.get(key);
  if (cached) {
    return cached;
  }
  const padding = Math.ceil(pixelSize * 0.18);
  const spriteSize = pixelSize + padding * 2;
  const sprite = createLayerCanvas(spriteSize, spriteSize);
  const ctx = getCanvasContext(sprite);
  if (!ctx) {
    return sprite;
  }
  ctx.save();
  ctx.translate(spriteSize / 2, spriteSize / 2);
  switch (kind) {
    case "planetary":
      drawPlanetary(ctx, pixelSize, teamColor);
      break;
    case "nexus":
      drawNexus(ctx, pixelSize, teamColor);
      break;
    case "bunker":
      drawBunker(ctx, pixelSize, teamColor);
      break;
    case "cannon":
      drawCannon(ctx, pixelSize, teamColor);
      break;
  }
  ctx.restore();
  spriteCache.set(key, sprite);
  return sprite;
}
function drawPlanetary(ctx, size, teamColor) {
  const half = size / 2;
  const dark = "#20272d";
  const hull = "#3d474d";
  const plate = "#657078";
  const light = "#c4b482";
  const glow = withAlpha(teamColor, "E6");
  drawSoftShadow(ctx, size);
  ctx.fillStyle = "#2b343a";
  ctx.strokeStyle = dark;
  ctx.lineWidth = size * 0.06;
  drawOctagon(ctx, 0, 0, half * 0.78, half * 0.62);
  ctx.fill();
  ctx.stroke();
  drawPlanetaryPod(ctx, -half * 0.55, half * 0.28, size, hull, dark);
  drawPlanetaryPod(ctx, half * 0.48, half * 0.22, size, hull, dark);
  drawPlanetaryPod(ctx, -half * 0.45, -half * 0.28, size, hull, dark);
  drawPlanetaryPod(ctx, half * 0.43, -half * 0.28, size, hull, dark);
  ctx.fillStyle = hull;
  ctx.strokeStyle = dark;
  drawOctagon(ctx, 0, -half * 0.06, half * 0.5, half * 0.4);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = plate;
  ctx.beginPath();
  ctx.ellipse(0, -half * 0.1, half * 0.3, half * 0.18, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = light;
  drawRoundedBox(ctx, -half * 0.16, -half * 0.02, half * 0.32, half * 0.13, size * 0.025);
  ctx.fill();
  ctx.fillStyle = glow;
  ctx.strokeStyle = withAlpha(teamColor, "88");
  ctx.lineWidth = size * 0.035;
  ctx.beginPath();
  ctx.arc(-half * 0.36, half * 0.14, size * 0.07, 0, Math.PI * 2);
  ctx.arc(half * 0.34, half * 0.1, size * 0.06, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
}
function drawNexus(ctx, size, teamColor) {
  const half = size / 2;
  const gold = "#d7bd6a";
  const shade = "#806b33";
  const crystal = withAlpha(teamColor, "E8");
  drawSoftShadow(ctx, size);
  ctx.fillStyle = withAlpha(gold, "D8");
  ctx.strokeStyle = shade;
  ctx.lineWidth = size * 0.06;
  drawDiamond(ctx, 0, 0, half * 0.86, half * 0.66);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = "#efe3a5";
  drawDiamond(ctx, 0, -half * 0.05, half * 0.46, half * 0.36);
  ctx.fill();
  ctx.fillStyle = crystal;
  ctx.strokeStyle = "#f7fbff";
  ctx.lineWidth = size * 0.035;
  drawDiamond(ctx, 0, -half * 0.2, half * 0.22, half * 0.42);
  ctx.fill();
  ctx.stroke();
}
function drawBunker(ctx, size, teamColor) {
  const half = size / 2;
  const dark = "#232930";
  const panel = "#59636d";
  const roof = "#d8d4cf";
  const glow = withAlpha(teamColor, "E8");
  drawSoftShadow(ctx, size);
  ctx.fillStyle = "#36414a";
  ctx.strokeStyle = dark;
  ctx.lineWidth = size * 0.055;
  drawOctagon(ctx, 0, half * 0.03, half * 0.82, half * 0.54);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = panel;
  drawSlantedPanel(ctx, -half * 0.54, half * 0.06, half * 0.4, half * 0.28, -1);
  ctx.fill();
  drawSlantedPanel(ctx, half * 0.54, half * 0.06, half * 0.4, half * 0.28, 1);
  ctx.fill();
  ctx.strokeStyle = "#1f252b";
  ctx.lineWidth = size * 0.025;
  drawPanelGrid(ctx, -half * 0.54, half * 0.06, half * 0.32, 3, -1);
  drawPanelGrid(ctx, half * 0.54, half * 0.06, half * 0.32, 3, 1);
  ctx.fillStyle = roof;
  ctx.strokeStyle = "#59616a";
  ctx.lineWidth = size * 0.045;
  ctx.beginPath();
  ctx.ellipse(0, -half * 0.08, half * 0.46, half * 0.34, 0, Math.PI, Math.PI * 2);
  ctx.lineTo(half * 0.36, half * 0.08);
  ctx.quadraticCurveTo(0, half * 0.24, -half * 0.36, half * 0.08);
  ctx.closePath();
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = "#272d34";
  drawRoundedBox(ctx, -half * 0.18, -half * 0.02, half * 0.36, half * 0.14, size * 0.025);
  ctx.fill();
  ctx.fillStyle = glow;
  for (let i = -2; i <= 2; i++) {
    ctx.beginPath();
    ctx.arc(i * half * 0.13, -half * 0.16 + Math.abs(i) * half * 0.015, size * 0.028, 0, Math.PI * 2);
    ctx.fill();
  }
}
function drawCannon(ctx, size, teamColor) {
  const half = size / 2;
  drawSoftShadow(ctx, size);
  ctx.strokeStyle = withAlpha(teamColor, "F2");
  ctx.lineWidth = size * 0.09;
  ctx.beginPath();
  ctx.arc(0, 0, half * 0.46, 0, Math.PI * 2);
  ctx.stroke();
  ctx.fillStyle = "#76623a";
  ctx.strokeStyle = "#42351c";
  ctx.lineWidth = size * 0.06;
  drawDiamond(ctx, 0, 0, half * 0.58, half * 0.58);
  ctx.fill();
  ctx.stroke();
  ctx.strokeStyle = withAlpha(teamColor, "E8");
  ctx.lineWidth = size * 0.08;
  ctx.beginPath();
  ctx.moveTo(0, -half * 0.52);
  ctx.lineTo(0, half * 0.52);
  ctx.moveTo(-half * 0.52, 0);
  ctx.lineTo(half * 0.52, 0);
  ctx.stroke();
}
function drawSoftShadow(ctx, size) {
  ctx.fillStyle = "rgba(0, 0, 0, 0.24)";
  ctx.beginPath();
  ctx.ellipse(0, size * 0.16, size * 0.42, size * 0.22, 0, 0, Math.PI * 2);
  ctx.fill();
}
function drawRoundedBox(ctx, x, y, width, height, radius) {
  const right = x + width;
  const bottom = y + height;
  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.lineTo(right - radius, y);
  ctx.quadraticCurveTo(right, y, right, y + radius);
  ctx.lineTo(right, bottom - radius);
  ctx.quadraticCurveTo(right, bottom, right - radius, bottom);
  ctx.lineTo(x + radius, bottom);
  ctx.quadraticCurveTo(x, bottom, x, bottom - radius);
  ctx.lineTo(x, y + radius);
  ctx.quadraticCurveTo(x, y, x + radius, y);
  ctx.closePath();
}
function drawOctagon(ctx, x, y, radiusX, radiusY) {
  ctx.beginPath();
  ctx.moveTo(x - radiusX * 0.42, y - radiusY);
  ctx.lineTo(x + radiusX * 0.42, y - radiusY);
  ctx.lineTo(x + radiusX, y - radiusY * 0.38);
  ctx.lineTo(x + radiusX, y + radiusY * 0.36);
  ctx.lineTo(x + radiusX * 0.42, y + radiusY);
  ctx.lineTo(x - radiusX * 0.42, y + radiusY);
  ctx.lineTo(x - radiusX, y + radiusY * 0.36);
  ctx.lineTo(x - radiusX, y - radiusY * 0.38);
  ctx.closePath();
}
function drawPlanetaryPod(ctx, x, y, size, fill, stroke) {
  ctx.fillStyle = fill;
  ctx.strokeStyle = stroke;
  drawRoundedBox(ctx, x - size * 0.09, y - size * 0.1, size * 0.18, size * 0.2, size * 0.03);
  ctx.fill();
  ctx.stroke();
}
function drawSlantedPanel(ctx, x, y, width, height, side) {
  ctx.beginPath();
  ctx.moveTo(x - side * width * 0.5, y - height * 0.45);
  ctx.lineTo(x + side * width * 0.36, y - height * 0.32);
  ctx.lineTo(x + side * width * 0.5, y + height * 0.48);
  ctx.lineTo(x - side * width * 0.4, y + height * 0.34);
  ctx.closePath();
}
function drawPanelGrid(ctx, x, y, width, lines, side) {
  for (let i = 1; i <= lines; i++) {
    const offset = (i / (lines + 1) - 0.5) * width;
    ctx.beginPath();
    ctx.moveTo(x + side * offset, y - width * 0.22);
    ctx.lineTo(x + side * (offset + width * 0.1), y + width * 0.2);
    ctx.stroke();
  }
}
function drawDiamond(ctx, x, y, radiusX, radiusY) {
  ctx.beginPath();
  ctx.moveTo(x, y - radiusY);
  ctx.lineTo(x + radiusX, y);
  ctx.lineTo(x, y + radiusY);
  ctx.lineTo(x - radiusX, y);
  ctx.closePath();
}
function resolveObjectiveKind(name, kind) {
  const key = normalizeObjectiveKey(name || kind);
  if (key.includes("planetary")) {
    return "planetary";
  }
  if (key.includes("nexus")) {
    return "nexus";
  }
  if (key.includes("bunker")) {
    return "bunker";
  }
  if (key.includes("cannon")) {
    return "cannon";
  }
  return null;
}
function isLargeObjective(name, kind) {
  const key = normalizeObjectiveKey(name || kind);
  return key.includes("planetary") || key.includes("nexus") || normalizeObjectiveKey(kind) === "base";
}
function normalizeObjectiveKey(value) {
  return value.trim().toLowerCase().replaceAll(/[^a-z0-9]+/g, "");
}

// TypeScript/store.ts
var states = /* @__PURE__ */ new WeakMap();
function getState(canvas) {
  return states.get(canvas);
}
function setState(canvas, state) {
  states.set(canvas, state);
}
function deleteState(canvas) {
  states.delete(canvas);
}

// TypeScript/protossIcons.ts
var protossTokens = {
  armorFill: "#F4D372",
  armorMid: "#C99732",
  armorShade: "#76531C",
  armorDark: "#2D2417",
  ivoryFill: "#FFF1C2",
  psiFill: "#6FE7FF",
  psiMid: "#25A8E8",
  psiDark: "#0B4A76",
  bladeFill: "#A9F6FF",
  bladeCore: "#E8FFFF",
  shadowFill: "#171C28",
  darkStroke: "#18130C"
};
var protossZealot = {
  id: "protoss.zealot",
  commander: "protoss",
  aliases: ["Zealot"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 35, 34], ["C", 38, 22, 44, 15, 50, 13], ["C", 56, 15, 62, 22, 65, 34], ["L", 60, 48], ["L", 40, 48], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 40, 36], ["L", 60, 36], ["L", 57, 44], ["L", 43, 44], ["Z"]],
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 35, 48], ["L", 65, 48], ["L", 70, 73], ["L", 57, 88], ["L", 43, 88], ["L", 30, 73], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 39, 56], ["L", 61, 56], ["L", 56, 74], ["L", 50, 82], ["L", 44, 74], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 33, 55], ["L", 18, 65], ["L", 19, 75], ["L", 39, 66], ["Z"], ["M", 67, 55], ["L", 82, 65], ["L", 81, 75], ["L", 61, 66], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 18, 72], ["C", 10, 82, 9, 92, 16, 97], ["C", 21, 87, 28, 79, 27, 70], ["Z"], ["M", 82, 72], ["C", 90, 82, 91, 92, 84, 97], ["C", 79, 87, 72, 79, 73, 70], ["Z"]],
      fill: "bladeFill",
      stroke: "psiDark",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [["M", 15, 92], ["L", 25, 75], ["M", 85, 92], ["L", 75, 75]],
      stroke: "bladeCore",
      strokeWidth: 2.5,
      lineCap: "round",
      opacity: 0.8
    }
  ]
};
var protossSentry = {
  id: "protoss.sentry",
  commander: "protoss",
  aliases: ["Sentry"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 14], ["C", 68, 15, 83, 30, 84, 49], ["C", 83, 69, 68, 84, 50, 86], ["C", 32, 84, 17, 69, 16, 49], ["C", 17, 30, 32, 15, 50, 14], ["Z"]],
      fill: "psiDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round",
      opacity: 0.65
    },
    {
      type: "path",
      commands: [["M", 50, 20], ["L", 64, 40], ["L", 58, 68], ["L", 50, 82], ["L", 42, 68], ["L", 36, 40], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 28, 43], ["L", 41, 34], ["L", 43, 54], ["L", 31, 64], ["Z"], ["M", 72, 43], ["L", 59, 34], ["L", 57, 54], ["L", 69, 64], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 51,
      r: 14,
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 50,
      cy: 51,
      r: 7,
      fill: "bladeCore",
      stroke: "psiDark",
      strokeWidth: 1.5
    },
    {
      type: "path",
      commands: [["M", 33, 76], ["L", 43, 64], ["M", 67, 76], ["L", 57, 64], ["M", 50, 68], ["L", 50, 89]],
      stroke: "armorShade",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossStalker = {
  id: "protoss.stalker",
  commander: "protoss",
  aliases: ["Stalker"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 29, 45], ["L", 40, 29], ["L", 60, 29], ["L", 71, 45], ["L", 65, 67], ["L", 50, 76], ["L", 35, 67], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 37, 41], ["L", 63, 41], ["L", 60, 60], ["L", 50, 67], ["L", 40, 60], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 51,
      r: 8,
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [["M", 35, 62], ["L", 20, 79], ["L", 27, 85], ["L", 44, 68], ["Z"], ["M", 65, 62], ["L", 80, 79], ["L", 73, 85], ["L", 56, 68], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 35, 50], ["L", 12, 44], ["L", 9, 52], ["L", 32, 59], ["Z"], ["M", 65, 50], ["L", 88, 44], ["L", 91, 52], ["L", 68, 59], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 41, 33], ["L", 50, 15], ["L", 59, 33]],
      stroke: "psiMid",
      strokeWidth: 4,
      lineCap: "round",
      lineJoin: "round"
    }
  ]
};
var protossAdept = {
  id: "protoss.adept",
  commander: "protoss",
  aliases: ["Adept"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 38, 32], ["C", 40, 21, 45, 15, 50, 13], ["C", 55, 15, 60, 21, 62, 32], ["L", 58, 46], ["L", 42, 46], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 41, 35], ["L", 59, 35], ["L", 56, 43], ["L", 44, 43], ["Z"]],
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 34, 48], ["L", 66, 48], ["L", 63, 73], ["L", 50, 88], ["L", 37, 73], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 39, 55], ["L", 61, 55], ["L", 56, 72], ["L", 50, 78], ["L", 44, 72], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 33, 56], ["L", 14, 54], ["L", 8, 64], ["L", 31, 66], ["Z"], ["M", 67, 56], ["L", 86, 54], ["L", 92, 64], ["L", 69, 66], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 14, 60], ["L", 5, 48], ["M", 86, 60], ["L", 95, 48]],
      stroke: "bladeFill",
      strokeWidth: 5,
      lineCap: "round"
    }
  ]
};
var protossHighTemplar = {
  id: "protoss.highTemplar",
  commander: "protoss",
  aliases: ["High Templar", "HighTemplar"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 35, 37], ["C", 38, 22, 44, 14, 50, 12], ["C", 56, 14, 62, 22, 65, 37], ["L", 59, 51], ["L", 41, 51], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 39, 39], ["L", 61, 39], ["L", 57, 48], ["L", 43, 48], ["Z"]],
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 34, 50], ["L", 66, 50], ["L", 78, 92], ["L", 22, 92], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 43, 55], ["L", 57, 55], ["L", 61, 87], ["L", 39, 87], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 35, 59], ["L", 16, 68], ["L", 24, 79], ["L", 42, 65], ["Z"], ["M", 65, 59], ["L", 84, 68], ["L", 76, 79], ["L", 58, 65], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 70,
      r: 9,
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 2,
      opacity: 0.75
    }
  ]
};
var protossDarkTemplar = {
  id: "protoss.darkTemplar",
  commander: "protoss",
  aliases: ["Dark Templar", "DarkTemplar"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    ...protossTokens,
    armorFill: "#7462A8",
    armorMid: "#44336F",
    armorShade: "#271A44",
    bladeFill: "#8FF9FF",
    psiFill: "#45D7FF"
  },
  layers: [
    {
      type: "path",
      commands: [["M", 34, 35], ["C", 38, 20, 45, 13, 50, 11], ["C", 55, 13, 62, 20, 66, 35], ["L", 60, 50], ["L", 40, 50], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 40, 36], ["L", 60, 36], ["L", 56, 45], ["L", 44, 45], ["Z"]],
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 33, 49], ["L", 67, 49], ["L", 73, 82], ["L", 50, 93], ["L", 27, 82], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 38, 58], ["L", 62, 58], ["L", 56, 77], ["L", 50, 84], ["L", 44, 77], ["Z"]],
      fill: "shadowFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 31, 59], ["L", 13, 75], ["L", 20, 82], ["L", 40, 66], ["Z"], ["M", 69, 59], ["L", 87, 75], ["L", 80, 82], ["L", 60, 66], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 14, 80], ["C", 8, 88, 8, 96, 15, 99], ["C", 20, 91, 27, 84, 25, 77], ["Z"], ["M", 86, 80], ["C", 92, 88, 92, 96, 85, 99], ["C", 80, 91, 73, 84, 75, 77], ["Z"]],
      fill: "bladeFill",
      stroke: "psiDark",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.9
    }
  ]
};
var protossArchon = {
  id: "protoss.archon",
  commander: "protoss",
  aliases: ["Archon"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 9], ["C", 72, 15, 84, 34, 82, 58], ["C", 78, 82, 65, 95, 50, 96], ["C", 35, 95, 22, 82, 18, 58], ["C", 16, 34, 28, 15, 50, 9], ["Z"]],
      fill: "psiDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 50, 18], ["C", 65, 24, 74, 40, 72, 58], ["C", 69, 76, 60, 87, 50, 88], ["C", 40, 87, 31, 76, 28, 58], ["C", 26, 40, 35, 24, 50, 18], ["Z"]],
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 3,
      opacity: 0.9
    },
    {
      type: "path",
      commands: [["M", 40, 35], ["L", 60, 35], ["L", 57, 48], ["L", 43, 48], ["Z"]],
      fill: "bladeCore",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 28, 56], ["L", 11, 72], ["L", 23, 80], ["L", 39, 62], ["Z"], ["M", 72, 56], ["L", 89, 72], ["L", 77, 80], ["L", 61, 62], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 37, 72], ["L", 28, 92], ["M", 63, 72], ["L", 72, 92], ["M", 50, 73], ["L", 50, 96]],
      stroke: "bladeCore",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.8
    }
  ]
};
var protossImmortal = {
  id: "protoss.immortal",
  commander: "protoss",
  aliases: ["Immortal"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 25, 48], ["L", 36, 28], ["L", 64, 28], ["L", 75, 48], ["L", 70, 76], ["L", 50, 88], ["L", 30, 76], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 18, 37], ["L", 35, 27], ["L", 39, 43], ["L", 23, 56], ["Z"], ["M", 82, 37], ["L", 65, 27], ["L", 61, 43], ["L", 77, 56], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 36, 43], ["L", 64, 43], ["L", 61, 66], ["L", 50, 75], ["L", 39, 66], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 55,
      r: 8,
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [["M", 21, 47], ["L", 5, 43], ["L", 4, 52], ["L", 21, 57], ["Z"], ["M", 79, 47], ["L", 95, 43], ["L", 96, 52], ["L", 79, 57], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 36, 76], ["L", 25, 91], ["L", 43, 92], ["L", 50, 80], ["Z"], ["M", 64, 76], ["L", 75, 91], ["L", 57, 92], ["L", 50, 80], ["Z"]],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    }
  ]
};
var protossColossus = {
  id: "protoss.colossus",
  commander: "protoss",
  aliases: ["Colossus"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 34, 29], ["L", 44, 16], ["L", 56, 16], ["L", 66, 29], ["L", 62, 53], ["L", 50, 64], ["L", 38, 53], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 38,
      r: 8,
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [["M", 38, 51], ["L", 25, 79], ["L", 17, 94], ["M", 44, 57], ["L", 39, 84], ["L", 35, 96], ["M", 62, 51], ["L", 75, 79], ["L", 83, 94], ["M", 56, 57], ["L", 61, 84], ["L", 65, 96]],
      stroke: "armorShade",
      strokeWidth: 6,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 31, 28], ["L", 9, 19], ["L", 13, 27], ["L", 34, 35], ["Z"], ["M", 69, 28], ["L", 91, 19], ["L", 87, 27], ["L", 66, 35], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 14, 24], ["L", 5, 35], ["M", 86, 24], ["L", 95, 35]],
      stroke: "bladeFill",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossDisruptor = {
  id: "protoss.disruptor",
  commander: "protoss",
  aliases: ["Disruptor"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 18], ["L", 67, 33], ["L", 78, 50], ["L", 67, 67], ["L", 50, 82], ["L", 33, 67], ["L", 22, 50], ["L", 33, 33], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 24,
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 15,
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 7,
      fill: "bladeCore",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [["M", 50, 19], ["L", 50, 32], ["M", 50, 68], ["L", 50, 81], ["M", 19, 50], ["L", 32, 50], ["M", 68, 50], ["L", 81, 50]],
      stroke: "ivoryFill",
      strokeWidth: 5,
      lineCap: "round"
    }
  ]
};
var protossObserver = {
  id: "protoss.observer",
  commander: "protoss",
  aliases: ["Observer"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 18], ["L", 67, 39], ["L", 82, 50], ["L", 67, 61], ["L", 50, 82], ["L", 33, 61], ["L", 18, 50], ["L", 33, 39], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 18,
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 10,
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 2.5
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 4,
      fill: "bladeCore",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [["M", 32, 40], ["L", 20, 28], ["M", 68, 40], ["L", 80, 28], ["M", 32, 60], ["L", 20, 72], ["M", 68, 60], ["L", 80, 72]],
      stroke: "armorShade",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossOracle = {
  id: "protoss.oracle",
  commander: "protoss",
  aliases: ["Oracle"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 15], ["C", 70, 18, 84, 34, 85, 52], ["C", 72, 46, 59, 45, 50, 50], ["C", 41, 45, 28, 46, 15, 52], ["C", 16, 34, 30, 18, 50, 15], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 29, 53], ["C", 37, 68, 45, 80, 50, 90], ["C", 55, 80, 63, 68, 71, 53], ["C", 61, 58, 39, 58, 29, 53], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 51,
      r: 11,
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 2.5
    },
    {
      type: "path",
      commands: [["M", 23, 45], ["L", 7, 39], ["L", 18, 55], ["M", 77, 45], ["L", 93, 39], ["L", 82, 55]],
      stroke: "armorShade",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 42, 27], ["L", 50, 15], ["L", 58, 27]],
      stroke: "psiMid",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossPhoenix = {
  id: "protoss.phoenix",
  commander: "protoss",
  aliases: ["Phoenix"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 12], ["L", 62, 37], ["L", 91, 48], ["L", 80, 63], ["L", 61, 58], ["L", 57, 81], ["L", 50, 91], ["L", 43, 81], ["L", 39, 58], ["L", 20, 63], ["L", 9, 48], ["L", 38, 37], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 41, 34], ["L", 59, 34], ["L", 62, 54], ["L", 55, 70], ["L", 45, 70], ["L", 38, 54], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 48,
      r: 7,
      fill: "psiFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [["M", 20, 52], ["L", 4, 57], ["L", 10, 66], ["L", 27, 58], ["Z"], ["M", 80, 52], ["L", 96, 57], ["L", 90, 66], ["L", 73, 58], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 44, 76], ["L", 36, 92], ["M", 56, 76], ["L", 64, 92]],
      stroke: "psiMid",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossVoidRay = {
  id: "protoss.voidRay",
  commander: "protoss",
  aliases: ["Void Ray", "VoidRay"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 10], ["L", 62, 32], ["L", 83, 44], ["L", 72, 61], ["L", 60, 57], ["L", 57, 84], ["L", 50, 94], ["L", 43, 84], ["L", 40, 57], ["L", 28, 61], ["L", 17, 44], ["L", 38, 32], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 41, 31], ["L", 59, 31], ["L", 64, 57], ["L", 56, 75], ["L", 44, 75], ["L", 36, 57], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 46, 28], ["L", 54, 28], ["L", 54, 75], ["L", 46, 75], ["Z"]],
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.85
    },
    {
      type: "path",
      commands: [["M", 27, 47], ["L", 8, 41], ["L", 17, 55], ["M", 73, 47], ["L", 92, 41], ["L", 83, 55]],
      stroke: "armorMid",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 50, 75], ["L", 50, 96]],
      stroke: "bladeCore",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossCarrier = {
  id: "protoss.carrier",
  commander: "protoss",
  aliases: ["Carrier"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 8], ["L", 67, 32], ["L", 90, 45], ["L", 80, 67], ["L", 62, 63], ["L", 59, 84], ["L", 50, 96], ["L", 41, 84], ["L", 38, 63], ["L", 20, 67], ["L", 10, 45], ["L", 33, 32], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 38, 29], ["L", 62, 29], ["L", 68, 57], ["L", 58, 78], ["L", 42, 78], ["L", 32, 57], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 39, 41], ["L", 61, 41], ["L", 64, 53], ["L", 36, 53], ["Z"]],
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 27,
      cy: 56,
      r: 4,
      fill: "bladeCore",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 73,
      cy: 56,
      r: 4,
      fill: "bladeCore",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "path",
      commands: [["M", 41, 79], ["L", 35, 94], ["M", 50, 82], ["L", 50, 97], ["M", 59, 79], ["L", 65, 94]],
      stroke: "psiMid",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossTempest = {
  id: "protoss.tempest",
  commander: "protoss",
  aliases: ["Tempest"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 6], ["L", 60, 34], ["L", 83, 49], ["L", 73, 62], ["L", 59, 57], ["L", 55, 84], ["L", 50, 96], ["L", 45, 84], ["L", 41, 57], ["L", 27, 62], ["L", 17, 49], ["L", 40, 34], ["Z"]],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 43, 27], ["L", 57, 27], ["L", 60, 69], ["L", 53, 88], ["L", 47, 88], ["L", 40, 69], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 47, 16], ["L", 53, 16], ["L", 53, 87], ["L", 47, 87], ["Z"]],
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 1.5,
      opacity: 0.85
    },
    {
      type: "path",
      commands: [["M", 27, 50], ["L", 9, 48], ["L", 15, 58], ["L", 33, 56], ["Z"], ["M", 73, 50], ["L", 91, 48], ["L", 85, 58], ["L", 67, 56], ["Z"]],
      fill: "ivoryFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 43,
      r: 5,
      fill: "bladeCore",
      stroke: "darkStroke",
      strokeWidth: 1.5
    }
  ]
};
var protossMothership = {
  id: "protoss.mothership",
  commander: "protoss",
  aliases: ["Mothership"],
  viewBox: { width: 100, height: 100 },
  tokens: protossTokens,
  layers: [
    {
      type: "path",
      commands: [["M", 50, 6], ["L", 64, 25], ["L", 88, 30], ["L", 76, 49], ["L", 88, 70], ["L", 64, 75], ["L", 50, 94], ["L", 36, 75], ["L", 12, 70], ["L", 24, 49], ["L", 12, 30], ["L", 36, 25], ["Z"]],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [["M", 50, 18], ["L", 62, 35], ["L", 67, 58], ["L", 58, 78], ["L", 50, 86], ["L", 42, 78], ["L", 33, 58], ["L", 38, 35], ["Z"]],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 54,
      r: 15,
      fill: "psiFill",
      stroke: "psiDark",
      strokeWidth: 3,
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 50,
      cy: 54,
      r: 7,
      fill: "bladeCore",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [["M", 25, 34], ["L", 7, 21], ["M", 75, 34], ["L", 93, 21], ["M", 24, 68], ["L", 6, 81], ["M", 76, 68], ["L", 94, 81], ["M", 50, 20], ["L", 50, 3], ["M", 50, 84], ["L", 50, 98]],
      stroke: "armorMid",
      strokeWidth: 5,
      lineCap: "round"
    },
    {
      type: "path",
      commands: [["M", 18, 49], ["L", 5, 49], ["M", 82, 49], ["L", 95, 49]],
      stroke: "psiMid",
      strokeWidth: 4,
      lineCap: "round"
    }
  ]
};
var protossUnits = {
  zealot: protossZealot,
  sentry: protossSentry,
  stalker: protossStalker,
  adept: protossAdept,
  highTemplar: protossHighTemplar,
  darkTemplar: protossDarkTemplar,
  archon: protossArchon,
  immortal: protossImmortal,
  colossus: protossColossus,
  disruptor: protossDisruptor,
  observer: protossObserver,
  oracle: protossOracle,
  phoenix: protossPhoenix,
  voidRay: protossVoidRay,
  carrier: protossCarrier,
  tempest: protossTempest,
  mothership: protossMothership
};

// TypeScript/terranIcons.ts
var terranMarine = {
  id: "terran.marine",
  commander: "terran",
  aliases: ["Marine", "MarineLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    badgeFill: "#234A68",
    badgeGlow: "#5DADEC",
    badgeStroke: "#B9E1FF",
    armorFill: "#D8E7F0",
    armorMid: "#9FB3C0",
    armorShade: "#5E7280",
    armorDark: "#2E4756",
    visorFill: "#F5D35D",
    visorShade: "#D9962B",
    redLight: "#FF5A4F",
    blueLight: "#76D6FF",
    darkStroke: "#102838",
    rifleFill: "#243946"
  },
  layers: [
    // Badge base
    // {
    //     type: "circle",
    //     cx: 50,
    //     cy: 50,
    //     r: 39,
    //     fill: "badgeFill",
    //     opacity: 0.98
    // },
    // {
    //     type: "circle",
    //     cx: 50,
    //     cy: 50,
    //     r: 39,
    //     stroke: "badgeStroke",
    //     strokeWidth: 4,
    //     opacity: 0.9
    // },
    // {
    //     type: "circle",
    //     cx: 50,
    //     cy: 50,
    //     r: 33,
    //     stroke: "badgeGlow",
    //     strokeWidth: 2,
    //     opacity: 0.35
    // },
    // Back shoulder silhouette
    {
      type: "path",
      commands: [
        ["M", 19, 67],
        ["C", 21, 55, 29, 47, 38, 47],
        ["L", 44, 70],
        ["L", 31, 79],
        ["C", 24, 78, 20, 74, 19, 67],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 81, 67],
        ["C", 79, 55, 71, 47, 62, 47],
        ["L", 56, 70],
        ["L", 69, 79],
        ["C", 76, 78, 80, 74, 81, 67],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Helmet dome
    {
      type: "path",
      commands: [
        ["M", 28, 55],
        ["C", 28, 37, 37, 25, 50, 23],
        ["C", 63, 25, 72, 37, 72, 55],
        ["L", 66, 67],
        ["L", 34, 67],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Helmet side panels
    {
      type: "path",
      commands: [
        ["M", 29, 50],
        ["L", 21, 55],
        ["L", 24, 68],
        ["L", 34, 66],
        ["L", 36, 54],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 71, 50],
        ["L", 79, 55],
        ["L", 76, 68],
        ["L", 66, 66],
        ["L", 64, 54],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Brow plate
    {
      type: "path",
      commands: [
        ["M", 32, 43],
        ["C", 38, 36, 44, 33, 50, 33],
        ["C", 56, 33, 62, 36, 68, 43],
        ["L", 64, 50],
        ["C", 59, 46, 55, 44, 50, 44],
        ["C", 45, 44, 41, 46, 36, 50],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Visor
    {
      type: "path",
      commands: [
        ["M", 34, 50],
        ["C", 39, 45, 45, 42, 50, 42],
        ["C", 55, 42, 61, 45, 66, 50],
        ["L", 62, 59],
        ["L", 38, 59],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 55],
        ["C", 45, 52, 55, 52, 61, 55]
      ],
      stroke: "visorShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.65
    },
    // Respirator / mouth guard
    {
      type: "path",
      commands: [
        ["M", 39, 61],
        ["L", 61, 61],
        ["L", 58, 72],
        ["L", 42, 72],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 44, 64],
        ["L", 44, 70],
        ["M", 50, 64],
        ["L", 50, 71],
        ["M", 56, 64],
        ["L", 56, 70]
      ],
      stroke: "armorMid",
      strokeWidth: 2,
      lineCap: "round"
    },
    // Chest plate
    {
      type: "path",
      commands: [
        ["M", 36, 72],
        ["L", 64, 72],
        ["L", 70, 84],
        ["L", 30, 84],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 75],
        ["L", 57, 75],
        ["L", 54, 82],
        ["L", 46, 82],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Helmet lights
    {
      type: "circle",
      cx: 35,
      cy: 38,
      r: 3,
      fill: "redLight",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    {
      type: "circle",
      cx: 65,
      cy: 38,
      r: 3,
      fill: "blueLight",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    // Small rifle silhouette across lower badge
    {
      type: "path",
      commands: [
        ["M", 24, 77],
        ["L", 58, 66],
        ["L", 61, 70],
        ["L", 78, 65],
        ["L", 80, 70],
        ["L", 63, 75],
        ["L", 59, 72],
        ["L", 29, 82],
        ["Z"]
      ],
      fill: "rifleFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.9
    },
    // Decorative antenna / comms
    {
      type: "path",
      commands: [
        ["M", 31, 33],
        ["L", 22, 22],
        ["M", 69, 33],
        ["L", 78, 22],
        ["M", 45, 25],
        ["L", 50, 17],
        ["L", 55, 25]
      ],
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineCap: "round",
      lineJoin: "round"
    }
  ]
};
var terranReaper = {
  id: "terran.reaper",
  commander: "terran",
  aliases: ["Reaper", "ReaperLightweight", "JetpackInfantry"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    armorFill: "#D9E6EC",
    armorMid: "#93A9B5",
    armorShade: "#536C7A",
    armorDark: "#263E4C",
    visorFill: "#F2C84B",
    visorShade: "#C17A28",
    jetFill: "#405B69",
    jetGlow: "#70D9FF",
    flameFill: "#FF9B35",
    flameCore: "#FFE07A",
    pistolFill: "#253743",
    darkStroke: "#102634",
    redLight: "#FF5A4F"
  },
  layers: [
    // Left jetpack pod
    {
      type: "path",
      commands: [
        ["M", 29, 35],
        ["L", 20, 47],
        ["L", 24, 65],
        ["L", 34, 59],
        ["L", 37, 42],
        ["Z"]
      ],
      fill: "jetFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right jetpack pod
    {
      type: "path",
      commands: [
        ["M", 71, 35],
        ["L", 80, 47],
        ["L", 76, 65],
        ["L", 66, 59],
        ["L", 63, 42],
        ["Z"]
      ],
      fill: "jetFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Jetpack top bridge
    {
      type: "path",
      commands: [
        ["M", 36, 38],
        ["C", 42, 33, 58, 33, 64, 38],
        ["L", 62, 50],
        ["L", 38, 50],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left thruster flame
    {
      type: "path",
      commands: [
        ["M", 25, 63],
        ["C", 20, 70, 22, 77, 27, 84],
        ["C", 29, 76, 34, 72, 32, 64],
        ["Z"]
      ],
      fill: "flameFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [
        ["M", 28, 66],
        ["C", 25, 71, 26, 76, 29, 80],
        ["C", 30, 74, 33, 71, 31, 66],
        ["Z"]
      ],
      fill: "flameCore",
      opacity: 0.85
    },
    // Right thruster flame
    {
      type: "path",
      commands: [
        ["M", 75, 63],
        ["C", 80, 70, 78, 77, 73, 84],
        ["C", 71, 76, 66, 72, 68, 64],
        ["Z"]
      ],
      fill: "flameFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round",
      opacity: 0.9
    },
    {
      type: "path",
      commands: [
        ["M", 72, 66],
        ["C", 75, 71, 74, 76, 71, 80],
        ["C", 70, 74, 67, 71, 69, 66],
        ["Z"]
      ],
      fill: "flameCore",
      opacity: 0.85
    },
    // Legs, spread/agile pose
    {
      type: "path",
      commands: [
        ["M", 42, 67],
        ["L", 35, 83],
        ["L", 27, 85],
        ["L", 34, 93],
        ["L", 44, 88],
        ["L", 49, 70],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 58, 67],
        ["L", 65, 83],
        ["L", 73, 85],
        ["L", 66, 93],
        ["L", 56, 88],
        ["L", 51, 70],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Torso
    {
      type: "path",
      commands: [
        ["M", 36, 47],
        ["C", 39, 40, 44, 37, 50, 37],
        ["C", 56, 37, 61, 40, 64, 47],
        ["L", 61, 68],
        ["L", 50, 74],
        ["L", 39, 68],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Chest plate
    {
      type: "path",
      commands: [
        ["M", 41, 51],
        ["L", 59, 51],
        ["L", 57, 64],
        ["L", 50, 68],
        ["L", 43, 64],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Head / helmet
    {
      type: "path",
      commands: [
        ["M", 36, 34],
        ["C", 37, 23, 43, 17, 50, 17],
        ["C", 57, 17, 63, 23, 64, 34],
        ["L", 60, 45],
        ["L", 40, 45],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Helmet side caps
    {
      type: "path",
      commands: [
        ["M", 38, 31],
        ["L", 31, 36],
        ["L", 34, 45],
        ["L", 41, 42],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 62, 31],
        ["L", 69, 36],
        ["L", 66, 45],
        ["L", 59, 42],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Visor
    {
      type: "path",
      commands: [
        ["M", 39, 33],
        ["C", 43, 29, 47, 27, 50, 27],
        ["C", 53, 27, 57, 29, 61, 33],
        ["L", 58, 39],
        ["L", 42, 39],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Visor lower shadow
    {
      type: "path",
      commands: [
        ["M", 43, 37],
        ["C", 47, 35, 53, 35, 57, 37]
      ],
      stroke: "visorShade",
      strokeWidth: 2.5,
      lineCap: "round",
      opacity: 0.7
    },
    // Left arm
    {
      type: "path",
      commands: [
        ["M", 38, 51],
        ["L", 24, 55],
        ["L", 15, 50],
        ["L", 12, 57],
        ["L", 24, 65],
        ["L", 42, 61],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right arm
    {
      type: "path",
      commands: [
        ["M", 62, 51],
        ["L", 76, 55],
        ["L", 85, 50],
        ["L", 88, 57],
        ["L", 76, 65],
        ["L", 58, 61],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left pistol
    {
      type: "path",
      commands: [
        ["M", 13, 48],
        ["L", 5, 47],
        ["L", 4, 52],
        ["L", 13, 54],
        ["L", 17, 61],
        ["L", 21, 58],
        ["L", 18, 51],
        ["Z"]
      ],
      fill: "pistolFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Right pistol
    {
      type: "path",
      commands: [
        ["M", 87, 48],
        ["L", 95, 47],
        ["L", 96, 52],
        ["L", 87, 54],
        ["L", 83, 61],
        ["L", 79, 58],
        ["L", 82, 51],
        ["Z"]
      ],
      fill: "pistolFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Small armor lights
    {
      type: "circle",
      cx: 45,
      cy: 56,
      r: 2,
      fill: "redLight",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 55,
      cy: 56,
      r: 2,
      fill: "jetGlow",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    // Motion-readable boot soles
    {
      type: "path",
      commands: [
        ["M", 27, 85],
        ["L", 20, 91],
        ["L", 34, 93],
        ["Z"],
        ["M", 73, 85],
        ["L", 80, 91],
        ["L", 66, 93],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    }
  ]
};
var terranMarauder = {
  id: "terran.marauder",
  commander: "terran",
  aliases: ["Marauder", "MarauderLightweight", "HeavyInfantry"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    armorFill: "#D7E3EA",
    armorMid: "#9CAFB9",
    armorShade: "#5E7480",
    armorDark: "#263E4A",
    visorFill: "#F2C84B",
    visorShade: "#B97828",
    launcherFill: "#324955",
    launcherDark: "#1C303A",
    muzzleFill: "#101F27",
    shellLight: "#FFB64D",
    redLight: "#FF5A4F",
    blueLight: "#76D6FF",
    darkStroke: "#102634"
  },
  layers: [
    // Back silhouette / heavy backpack
    {
      type: "path",
      commands: [
        ["M", 27, 39],
        ["C", 32, 28, 42, 23, 50, 23],
        ["C", 58, 23, 68, 28, 73, 39],
        ["L", 72, 66],
        ["L", 62, 74],
        ["L", 38, 74],
        ["L", 28, 66],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Massive left shoulder
    {
      type: "path",
      commands: [
        ["M", 28, 42],
        ["C", 19, 43, 12, 50, 10, 61],
        ["C", 9, 70, 16, 77, 27, 77],
        ["L", 39, 68],
        ["L", 38, 50],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Massive right shoulder
    {
      type: "path",
      commands: [
        ["M", 72, 42],
        ["C", 81, 43, 88, 50, 90, 61],
        ["C", 91, 70, 84, 77, 73, 77],
        ["L", 61, 68],
        ["L", 62, 50],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Left shoulder front plate
    {
      type: "path",
      commands: [
        ["M", 18, 58],
        ["C", 21, 51, 27, 48, 34, 49],
        ["L", 37, 63],
        ["L", 29, 70],
        ["C", 22, 70, 18, 66, 18, 58],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Right shoulder front plate
    {
      type: "path",
      commands: [
        ["M", 82, 58],
        ["C", 79, 51, 73, 48, 66, 49],
        ["L", 63, 63],
        ["L", 71, 70],
        ["C", 78, 70, 82, 66, 82, 58],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Helmet
    {
      type: "path",
      commands: [
        ["M", 35, 43],
        ["C", 36, 31, 43, 25, 50, 25],
        ["C", 57, 25, 64, 31, 65, 43],
        ["L", 61, 54],
        ["L", 39, 54],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Helmet brow
    {
      type: "path",
      commands: [
        ["M", 37, 39],
        ["C", 41, 35, 46, 33, 50, 33],
        ["C", 54, 33, 59, 35, 63, 39],
        ["L", 60, 45],
        ["L", 40, 45],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Visor
    {
      type: "path",
      commands: [
        ["M", 39, 43],
        ["L", 61, 43],
        ["L", 58, 49],
        ["L", 42, 49],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Visor shadow
    {
      type: "path",
      commands: [
        ["M", 43, 47],
        ["L", 57, 47]
      ],
      stroke: "visorShade",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.7
    },
    // Thick torso
    {
      type: "path",
      commands: [
        ["M", 35, 53],
        ["L", 65, 53],
        ["L", 70, 75],
        ["L", 59, 87],
        ["L", 41, 87],
        ["L", 30, 75],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Central chest armor
    {
      type: "path",
      commands: [
        ["M", 40, 58],
        ["L", 60, 58],
        ["L", 62, 72],
        ["L", 55, 81],
        ["L", 45, 81],
        ["L", 38, 72],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Abdomen dark plate
    {
      type: "path",
      commands: [
        ["M", 43, 70],
        ["L", 57, 70],
        ["L", 55, 80],
        ["L", 45, 80],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Left arm cannon base
    {
      type: "path",
      commands: [
        ["M", 27, 66],
        ["L", 15, 70],
        ["L", 10, 83],
        ["L", 20, 88],
        ["L", 32, 77],
        ["Z"]
      ],
      fill: "launcherFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right arm cannon base
    {
      type: "path",
      commands: [
        ["M", 73, 66],
        ["L", 85, 70],
        ["L", 90, 83],
        ["L", 80, 88],
        ["L", 68, 77],
        ["Z"]
      ],
      fill: "launcherFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left grenade launcher barrel
    {
      type: "path",
      commands: [
        ["M", 11, 76],
        ["L", 3, 78],
        ["L", 5, 87],
        ["L", 18, 85],
        ["L", 20, 78],
        ["Z"]
      ],
      fill: "launcherDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Right grenade launcher barrel
    {
      type: "path",
      commands: [
        ["M", 89, 76],
        ["L", 97, 78],
        ["L", 95, 87],
        ["L", 82, 85],
        ["L", 80, 78],
        ["Z"]
      ],
      fill: "launcherDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Launcher muzzles
    {
      type: "circle",
      cx: 9,
      cy: 82,
      r: 4,
      fill: "muzzleFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "circle",
      cx: 91,
      cy: 82,
      r: 4,
      fill: "muzzleFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    // Heavy boots
    {
      type: "path",
      commands: [
        ["M", 40, 84],
        ["L", 33, 93],
        ["L", 47, 94],
        ["L", 50, 86],
        ["Z"],
        ["M", 60, 84],
        ["L", 67, 93],
        ["L", 53, 94],
        ["L", 50, 86],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Grenade/shell accents
    {
      type: "circle",
      cx: 28,
      cy: 54,
      r: 2.5,
      fill: "shellLight",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 72,
      cy: 54,
      r: 2.5,
      fill: "shellLight",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    // Armor lights
    {
      type: "circle",
      cx: 45,
      cy: 63,
      r: 2,
      fill: "redLight",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 55,
      cy: 63,
      r: 2,
      fill: "blueLight",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    // Panel seams
    {
      type: "path",
      commands: [
        ["M", 36, 65],
        ["L", 44, 69],
        ["M", 64, 65],
        ["L", 56, 69],
        ["M", 50, 58],
        ["L", 50, 68]
      ],
      stroke: "darkStroke",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    }
  ]
};
var terranDetailTokens = {
  armorFill: "#D7E3EA",
  armorMid: "#9CAFB9",
  armorShade: "#5E7480",
  armorDark: "#263E4A",
  visorFill: "#F2C84B",
  visorShade: "#B97828",
  weaponFill: "#324955",
  weaponDark: "#101F27",
  engineGlow: "#70D9FF",
  flameFill: "#FF9B35",
  flameCore: "#FFE07A",
  medicFill: "#F2F7FA",
  mineGlow: "#FF5A4F",
  redLight: "#FF5A4F",
  blueLight: "#76D6FF",
  darkStroke: "#102634"
};
var terranGhost = {
  id: "terran.ghost",
  commander: "terran",
  aliases: ["Ghost", "GhostAlternate", "GhostNova"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 36, 40],
        ["C", 38, 28, 43, 20, 50, 18],
        ["C", 57, 20, 62, 28, 64, 40],
        ["L", 59, 53],
        ["L", 41, 53],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 40],
        ["L", 61, 40],
        ["L", 57, 47],
        ["L", 43, 47],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 37, 52],
        ["L", 63, 52],
        ["L", 68, 78],
        ["L", 55, 90],
        ["L", 45, 90],
        ["L", 32, 78],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 41, 58],
        ["L", 59, 58],
        ["L", 56, 76],
        ["L", 50, 82],
        ["L", 44, 76],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 34, 57],
        ["L", 18, 64],
        ["L", 19, 72],
        ["L", 39, 66],
        ["Z"],
        ["M", 66, 57],
        ["L", 82, 64],
        ["L", 81, 72],
        ["L", 61, 66],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 15, 67],
        ["L", 76, 34],
        ["L", 82, 39],
        ["L", 22, 74],
        ["Z"],
        ["M", 75, 32],
        ["L", 94, 25],
        ["L", 91, 33],
        ["L", 80, 39],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 64,
      r: 3,
      fill: "blueLight",
      stroke: "darkStroke",
      strokeWidth: 1
    }
  ]
};
var terranHellbat = {
  id: "terran.hellbat",
  commander: "terran",
  aliases: ["Hellbat"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 29, 40],
        ["C", 33, 29, 41, 23, 50, 23],
        ["C", 59, 23, 67, 29, 71, 40],
        ["L", 68, 64],
        ["L", 58, 75],
        ["L", 42, 75],
        ["L", 32, 64],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 23, 48],
        ["C", 13, 52, 9, 62, 12, 72],
        ["L", 28, 73],
        ["L", 36, 60],
        ["L", 34, 49],
        ["Z"],
        ["M", 77, 48],
        ["C", 87, 52, 91, 62, 88, 72],
        ["L", 72, 73],
        ["L", 64, 60],
        ["L", 66, 49],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 37, 37],
        ["L", 63, 37],
        ["L", 61, 49],
        ["L", 39, 49],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 40, 44],
        ["L", 60, 44],
        ["L", 56, 50],
        ["L", 44, 50],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 36, 58],
        ["L", 64, 58],
        ["L", 61, 79],
        ["L", 50, 88],
        ["L", 39, 79],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 17, 71],
        ["L", 7, 76],
        ["L", 9, 87],
        ["L", 23, 80],
        ["Z"],
        ["M", 83, 71],
        ["L", 93, 76],
        ["L", 91, 87],
        ["L", 77, 80],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 7, 80],
        ["C", 1, 85, 2, 93, 10, 96],
        ["C", 9, 89, 15, 86, 17, 81],
        ["Z"],
        ["M", 93, 80],
        ["C", 99, 85, 98, 93, 90, 96],
        ["C", 91, 89, 85, 86, 83, 81],
        ["Z"]
      ],
      fill: "flameFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    }
  ]
};
var terranHellion = {
  id: "terran.hellion",
  commander: "terran",
  aliases: ["Hellion", "HellionTank"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 20, 49],
        ["L", 32, 34],
        ["L", 68, 34],
        ["L", 80, 49],
        ["L", 76, 68],
        ["L", 24, 68],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 36, 40],
        ["L", 64, 40],
        ["L", 70, 54],
        ["L", 30, 54],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 41, 44],
        ["L", 59, 44],
        ["L", 63, 52],
        ["L", 37, 52],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 26,
      cy: 70,
      r: 9,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 74,
      cy: 70,
      r: 9,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "path",
      commands: [
        ["M", 24, 57],
        ["L", 9, 58],
        ["L", 10, 65],
        ["L", 27, 64],
        ["Z"],
        ["M", 76, 57],
        ["L", 91, 58],
        ["L", 90, 65],
        ["L", 73, 64],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 9, 61],
        ["C", 2, 65, 2, 72, 9, 76],
        ["C", 9, 69, 15, 66, 16, 61],
        ["Z"],
        ["M", 91, 61],
        ["C", 98, 65, 98, 72, 91, 76],
        ["C", 91, 69, 85, 66, 84, 61],
        ["Z"]
      ],
      fill: "flameFill",
      opacity: 0.9
    }
  ]
};
var terranMedivac = {
  id: "terran.medivac",
  commander: "terran",
  aliases: ["Medivac"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 15],
        ["L", 66, 34],
        ["L", 86, 44],
        ["L", 78, 58],
        ["L", 63, 56],
        ["L", 59, 80],
        ["L", 50, 91],
        ["L", 41, 80],
        ["L", 37, 56],
        ["L", 22, 58],
        ["L", 14, 44],
        ["L", 34, 34],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 40, 33],
        ["L", 60, 33],
        ["L", 64, 62],
        ["L", 56, 78],
        ["L", 44, 78],
        ["L", 36, 62],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 39],
        ["L", 57, 39],
        ["L", 60, 48],
        ["L", 40, 48],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 46, 53],
        ["L", 54, 53],
        ["L", 54, 61],
        ["L", 62, 61],
        ["L", 62, 69],
        ["L", 54, 69],
        ["L", 54, 77],
        ["L", 46, 77],
        ["L", 46, 69],
        ["L", 38, 69],
        ["L", 38, 61],
        ["L", 46, 61],
        ["Z"]
      ],
      fill: "medicFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 22,
      cy: 51,
      r: 5,
      fill: "engineGlow",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "circle",
      cx: 78,
      cy: 51,
      r: 5,
      fill: "engineGlow",
      stroke: "darkStroke",
      strokeWidth: 2
    }
  ]
};
var terranBanshee = {
  id: "terran.banshee",
  commander: "terran",
  aliases: ["Banshee"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 12],
        ["L", 62, 35],
        ["L", 87, 43],
        ["L", 77, 59],
        ["L", 63, 55],
        ["L", 59, 79],
        ["L", 50, 90],
        ["L", 41, 79],
        ["L", 37, 55],
        ["L", 23, 59],
        ["L", 13, 43],
        ["L", 38, 35],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 40, 32],
        ["L", 60, 32],
        ["L", 64, 57],
        ["L", 56, 73],
        ["L", 44, 73],
        ["L", 36, 57],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 35],
        ["L", 57, 35],
        ["L", 60, 45],
        ["L", 40, 45],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 20, 39],
        ["L", 4, 35],
        ["L", 19, 31],
        ["M", 80, 39],
        ["L", 96, 35],
        ["L", 81, 31]
      ],
      stroke: "weaponDark",
      strokeWidth: 4,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 32, 61],
        ["L", 19, 75],
        ["L", 27, 79],
        ["L", 40, 66],
        ["Z"],
        ["M", 68, 61],
        ["L", 81, 75],
        ["L", 73, 79],
        ["L", 60, 66],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 84,
      r: 4,
      fill: "engineGlow",
      stroke: "darkStroke",
      strokeWidth: 1.5
    }
  ]
};
var terranViking = {
  id: "terran.viking",
  commander: "terran",
  aliases: ["Viking", "VikingFighter", "VikingAssault"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 10],
        ["L", 62, 36],
        ["L", 91, 52],
        ["L", 82, 67],
        ["L", 62, 61],
        ["L", 58, 83],
        ["L", 50, 92],
        ["L", 42, 83],
        ["L", 38, 61],
        ["L", 18, 67],
        ["L", 9, 52],
        ["L", 38, 36],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 41, 32],
        ["L", 59, 32],
        ["L", 63, 57],
        ["L", 55, 75],
        ["L", 45, 75],
        ["L", 37, 57],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 37],
        ["L", 57, 37],
        ["L", 60, 46],
        ["L", 40, 46],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 18, 55],
        ["L", 4, 58],
        ["L", 8, 66],
        ["L", 25, 62],
        ["Z"],
        ["M", 82, 55],
        ["L", 96, 58],
        ["L", 92, 66],
        ["L", 75, 62],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 78],
        ["L", 34, 91],
        ["L", 48, 88],
        ["Z"],
        ["M", 57, 78],
        ["L", 66, 91],
        ["L", 52, 88],
        ["Z"]
      ],
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 84,
      r: 3.5,
      fill: "engineGlow",
      stroke: "darkStroke",
      strokeWidth: 1.5
    }
  ]
};
var terranRaven = {
  id: "terran.raven",
  commander: "terran",
  aliases: ["Raven"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 18],
        ["L", 62, 37],
        ["L", 82, 43],
        ["L", 74, 60],
        ["L", 60, 60],
        ["L", 57, 78],
        ["L", 50, 88],
        ["L", 43, 78],
        ["L", 40, 60],
        ["L", 26, 60],
        ["L", 18, 43],
        ["L", 38, 37],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 39],
        ["L", 61, 39],
        ["L", 64, 56],
        ["L", 55, 67],
        ["L", 45, 67],
        ["L", 36, 56],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 52,
      r: 9,
      fill: "engineGlow",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 50,
      cy: 52,
      r: 4,
      fill: "blueLight",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    {
      type: "path",
      commands: [
        ["M", 35, 36],
        ["L", 27, 24],
        ["M", 65, 36],
        ["L", 73, 24],
        ["M", 50, 30],
        ["L", 50, 16]
      ],
      stroke: "engineGlow",
      strokeWidth: 3,
      lineCap: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 15,
      r: 3,
      fill: "redLight",
      stroke: "darkStroke",
      strokeWidth: 1
    }
  ]
};
var terranSiegeTank = {
  id: "terran.siegeTank",
  commander: "terran",
  aliases: ["Siege Tank", "SiegeTank"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 21, 50],
        ["L", 34, 36],
        ["L", 66, 36],
        ["L", 79, 50],
        ["L", 76, 76],
        ["L", 24, 76],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 26, 72],
        ["L", 17, 85],
        ["L", 83, 85],
        ["L", 74, 72],
        ["Z"]
      ],
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 36, 44],
        ["L", 64, 44],
        ["L", 68, 63],
        ["L", 59, 72],
        ["L", 41, 72],
        ["L", 32, 63],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 47, 45],
        ["L", 53, 45],
        ["L", 55, 15],
        ["L", 45, 15],
        ["Z"],
        ["M", 43, 12],
        ["L", 57, 12],
        ["L", 58, 18],
        ["L", 42, 18],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 23, 80],
        ["L", 34, 72],
        ["M", 41, 83],
        ["L", 47, 73],
        ["M", 59, 83],
        ["L", 53, 73],
        ["M", 77, 80],
        ["L", 66, 72]
      ],
      stroke: "armorMid",
      strokeWidth: 3,
      lineCap: "round"
    }
  ]
};
var terranCyclone = {
  id: "terran.cyclone",
  commander: "terran",
  aliases: ["Cyclone"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 23, 48],
        ["L", 37, 34],
        ["L", 63, 34],
        ["L", 77, 48],
        ["L", 74, 72],
        ["L", 26, 72],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 35, 42],
        ["L", 65, 42],
        ["L", 68, 61],
        ["L", 58, 70],
        ["L", 42, 70],
        ["L", 32, 61],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 50, 45],
        ["C", 62, 45, 70, 53, 70, 65],
        ["C", 70, 77, 61, 86, 50, 86],
        ["C", 39, 86, 30, 77, 30, 66],
        ["C", 30, 57, 36, 50, 44, 48]
      ],
      stroke: "engineGlow",
      strokeWidth: 6,
      lineCap: "round",
      lineJoin: "round",
      opacity: 0.8
    },
    {
      type: "path",
      commands: [
        ["M", 61, 37],
        ["L", 87, 25],
        ["L", 91, 34],
        ["L", 66, 48],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 24,
      cy: 74,
      r: 8,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 3
    },
    {
      type: "circle",
      cx: 76,
      cy: 74,
      r: 8,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 3
    }
  ]
};
var terranWidowMine = {
  id: "terran.widowMine",
  commander: "terran",
  aliases: ["Widow Mine", "WidowMine"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 20],
        ["L", 66, 34],
        ["L", 80, 50],
        ["L", 66, 66],
        ["L", 50, 80],
        ["L", 34, 66],
        ["L", 20, 50],
        ["L", 34, 34],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 36, 35],
        ["L", 64, 35],
        ["L", 70, 50],
        ["L", 64, 65],
        ["L", 36, 65],
        ["L", 30, 50],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 13,
      fill: "mineGlow",
      stroke: "darkStroke",
      strokeWidth: 3,
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 50,
      cy: 50,
      r: 6,
      fill: "flameCore",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    {
      type: "path",
      commands: [
        ["M", 31, 31],
        ["L", 18, 22],
        ["M", 69, 31],
        ["L", 82, 22],
        ["M", 31, 69],
        ["L", 18, 78],
        ["M", 69, 69],
        ["L", 82, 78]
      ],
      stroke: "weaponFill",
      strokeWidth: 5,
      lineCap: "round"
    }
  ]
};
var terranLiberator = {
  id: "terran.liberator",
  commander: "terran",
  aliases: ["Liberator"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 12],
        ["L", 62, 34],
        ["L", 90, 49],
        ["L", 82, 63],
        ["L", 62, 58],
        ["L", 58, 80],
        ["L", 50, 91],
        ["L", 42, 80],
        ["L", 38, 58],
        ["L", 18, 63],
        ["L", 10, 49],
        ["L", 38, 34],
        ["Z"]
      ],
      fill: "armorMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 41, 33],
        ["L", 59, 33],
        ["L", 63, 56],
        ["L", 55, 72],
        ["L", 45, 72],
        ["L", 37, 56],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 27,
      cy: 55,
      r: 8,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 2.5
    },
    {
      type: "circle",
      cx: 73,
      cy: 55,
      r: 8,
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 2.5
    },
    {
      type: "path",
      commands: [
        ["M", 45, 72],
        ["L", 50, 91],
        ["L", 55, 72]
      ],
      stroke: "engineGlow",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 38],
        ["L", 57, 38],
        ["L", 60, 47],
        ["L", 40, 47],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    }
  ]
};
var terranThor = {
  id: "terran.thor",
  commander: "terran",
  aliases: ["Thor", "ThorAP"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 29, 30],
        ["L", 41, 18],
        ["L", 59, 18],
        ["L", 71, 30],
        ["L", 75, 63],
        ["L", 63, 84],
        ["L", 37, 84],
        ["L", 25, 63],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 22, 42],
        ["L", 8, 48],
        ["L", 7, 66],
        ["L", 24, 68],
        ["L", 34, 55],
        ["L", 33, 42],
        ["Z"],
        ["M", 78, 42],
        ["L", 92, 48],
        ["L", 93, 66],
        ["L", 76, 68],
        ["L", 66, 55],
        ["L", 67, 42],
        ["Z"]
      ],
      fill: "armorShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 38, 33],
        ["L", 62, 33],
        ["L", 65, 55],
        ["L", 57, 70],
        ["L", 43, 70],
        ["L", 35, 55],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 41, 39],
        ["L", 59, 39],
        ["L", 57, 48],
        ["L", 43, 48],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 19, 58],
        ["L", 5, 75],
        ["L", 14, 82],
        ["L", 29, 65],
        ["Z"],
        ["M", 81, 58],
        ["L", 95, 75],
        ["L", 86, 82],
        ["L", 71, 65],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 82],
        ["L", 30, 94],
        ["L", 47, 94],
        ["L", 50, 84],
        ["Z"],
        ["M", 61, 82],
        ["L", 70, 94],
        ["L", 53, 94],
        ["L", 50, 84],
        ["Z"]
      ],
      fill: "weaponDark",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    }
  ]
};
var terranBattlecruiser = {
  id: "terran.battlecruiser",
  commander: "terran",
  aliases: ["Battlecruiser"],
  viewBox: { width: 100, height: 100 },
  tokens: terranDetailTokens,
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 8],
        ["L", 65, 31],
        ["L", 86, 45],
        ["L", 79, 64],
        ["L", 63, 62],
        ["L", 60, 84],
        ["L", 50, 95],
        ["L", 40, 84],
        ["L", 37, 62],
        ["L", 21, 64],
        ["L", 14, 45],
        ["L", 35, 31],
        ["Z"]
      ],
      fill: "armorDark",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 29],
        ["L", 61, 29],
        ["L", 67, 58],
        ["L", 58, 77],
        ["L", 42, 77],
        ["L", 33, 58],
        ["Z"]
      ],
      fill: "armorFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 43, 35],
        ["L", 57, 35],
        ["L", 61, 46],
        ["L", 39, 46],
        ["Z"]
      ],
      fill: "visorFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 32, 55],
        ["L", 13, 72],
        ["L", 22, 79],
        ["L", 41, 62],
        ["Z"],
        ["M", 68, 55],
        ["L", 87, 72],
        ["L", 78, 79],
        ["L", 59, 62],
        ["Z"]
      ],
      fill: "weaponFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 44, 77],
        ["L", 39, 92],
        ["M", 50, 80],
        ["L", 50, 96],
        ["M", 56, 77],
        ["L", 61, 92]
      ],
      stroke: "engineGlow",
      strokeWidth: 4,
      lineCap: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 59,
      r: 5,
      fill: "redLight",
      stroke: "darkStroke",
      strokeWidth: 1.5
    }
  ]
};
var terranUnits = {
  marine: terranMarine,
  reaper: terranReaper,
  marauder: terranMarauder,
  ghost: terranGhost,
  hellbat: terranHellbat,
  hellion: terranHellion,
  medivac: terranMedivac,
  banshee: terranBanshee,
  viking: terranViking,
  raven: terranRaven,
  siegeTank: terranSiegeTank,
  cyclone: terranCyclone,
  widowMine: terranWidowMine,
  liberator: terranLiberator,
  thor: terranThor,
  battlecruiser: terranBattlecruiser
};

// TypeScript/zergIcons.ts
var zergZergling = {
  id: "zerg.zergling",
  commander: "zerg",
  aliases: ["Zergling", "ZerglingLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#8C5A92",
    skinMid: "#6F4374",
    skinShade: "#4F2D56",
    carapaceFill: "#B78AC8",
    carapaceShade: "#7C5A8A",
    clawFill: "#E7D9B8",
    eyeFill: "#FF5A54",
    darkStroke: "#2A1630"
  },
  layers: [
    // Tail
    {
      type: "path",
      commands: [
        ["M", 18, 66],
        ["C", 10, 64, 8, 58, 14, 54],
        ["C", 20, 51, 27, 54, 31, 59],
        ["L", 29, 66],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Rear leg
    {
      type: "path",
      commands: [
        ["M", 30, 66],
        ["L", 24, 82],
        ["L", 31, 88],
        ["L", 41, 76],
        ["L", 39, 66],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Rear claw
    {
      type: "path",
      commands: [
        ["M", 23, 84],
        ["L", 18, 91],
        ["L", 27, 88],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Main body
    {
      type: "path",
      commands: [
        ["M", 27, 61],
        ["C", 32, 49, 44, 42, 58, 42],
        ["C", 68, 42, 76, 46, 82, 52],
        ["C", 84, 58, 81, 64, 74, 67],
        ["L", 52, 70],
        ["C", 41, 72, 32, 69, 27, 61],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Belly / lower body
    {
      type: "path",
      commands: [
        ["M", 33, 61],
        ["C", 40, 66, 49, 68, 60, 67],
        ["C", 67, 67, 72, 65, 75, 62],
        ["L", 70, 72],
        ["C", 59, 75, 46, 75, 36, 70],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Carapace top plate
    {
      type: "path",
      commands: [
        ["M", 34, 54],
        ["C", 43, 45, 55, 41, 67, 43],
        ["C", 73, 44, 78, 47, 81, 51],
        ["L", 72, 58],
        ["C", 62, 55, 50, 55, 39, 60],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Dorsal spikes
    {
      type: "path",
      commands: [
        ["M", 38, 49],
        ["L", 35, 36],
        ["L", 44, 46],
        ["Z"],
        ["M", 50, 44],
        ["L", 49, 29],
        ["L", 57, 42],
        ["Z"],
        ["M", 63, 45],
        ["L", 67, 31],
        ["L", 69, 46],
        ["Z"]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Head
    {
      type: "path",
      commands: [
        ["M", 61, 46],
        ["C", 72, 40, 82, 41, 89, 47],
        ["C", 92, 50, 92, 54, 88, 57],
        ["C", 82, 62, 72, 62, 63, 58],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Upper jaw highlight / face plate
    {
      type: "path",
      commands: [
        ["M", 68, 48],
        ["C", 75, 45, 82, 45, 87, 49],
        ["L", 81, 53],
        ["C", 77, 51, 72, 51, 68, 53],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 78,
      cy: 50,
      r: 2.8,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    // Front arm / main scythe
    {
      type: "path",
      commands: [
        ["M", 60, 61],
        ["C", 66, 62, 72, 66, 74, 72],
        ["L", 86, 78],
        ["L", 78, 83],
        ["L", 69, 75],
        ["C", 66, 73, 63, 69, 60, 65],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Front scythe claw tip
    {
      type: "path",
      commands: [
        ["M", 84, 77],
        ["L", 93, 79],
        ["L", 82, 84],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Lower forelimb / secondary claw
    {
      type: "path",
      commands: [
        ["M", 53, 63],
        ["C", 58, 66, 61, 71, 60, 76],
        ["L", 52, 80],
        ["L", 49, 73],
        ["L", 49, 65],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Secondary claw tip
    {
      type: "path",
      commands: [
        ["M", 51, 79],
        ["L", 47, 87],
        ["L", 56, 82],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Little leg separation accents
    {
      type: "path",
      commands: [
        ["M", 35, 67],
        ["L", 40, 79],
        ["M", 44, 66],
        ["L", 48, 76]
      ],
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineCap: "round"
    }
  ]
};
var zergRoach = {
  id: "zerg.roach",
  commander: "zerg",
  aliases: ["Roach", "RoachLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7B4B83",
    skinMid: "#633769",
    skinShade: "#43234A",
    carapaceFill: "#A77CB7",
    carapaceMid: "#7C5A8A",
    carapaceShade: "#4E355B",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    eyeFill: "#FF5A54",
    clawFill: "#E7D9B8",
    darkStroke: "#26142D"
  },
  layers: [
    // Rear tail / abdomen taper
    {
      type: "path",
      commands: [
        ["M", 17, 59],
        ["C", 9, 58, 7, 51, 13, 47],
        ["C", 20, 43, 29, 47, 34, 53],
        ["L", 31, 64],
        ["C", 26, 63, 21, 61, 17, 59],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Back left leg
    {
      type: "path",
      commands: [
        ["M", 31, 63],
        ["L", 22, 76],
        ["L", 14, 78],
        ["L", 21, 84],
        ["L", 33, 77],
        ["L", 39, 65],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Back right leg
    {
      type: "path",
      commands: [
        ["M", 67, 62],
        ["L", 78, 75],
        ["L", 87, 77],
        ["L", 80, 84],
        ["L", 67, 77],
        ["L", 61, 65],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Main low body silhouette
    {
      type: "path",
      commands: [
        ["M", 25, 58],
        ["C", 28, 43, 42, 34, 58, 35],
        ["C", 73, 36, 85, 45, 88, 57],
        ["C", 87, 68, 77, 75, 60, 77],
        ["C", 43, 79, 29, 72, 25, 58],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Large armored shell / roach back
    {
      type: "path",
      commands: [
        ["M", 31, 55],
        ["C", 36, 41, 47, 34, 59, 36],
        ["C", 72, 38, 82, 46, 85, 56],
        ["C", 78, 61, 68, 64, 56, 64],
        ["C", 45, 64, 36, 61, 31, 55],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Shell segmented plates
    {
      type: "path",
      commands: [
        ["M", 43, 39],
        ["C", 39, 46, 38, 55, 42, 62],
        ["M", 56, 36],
        ["C", 52, 45, 52, 56, 57, 64],
        ["M", 70, 41],
        ["C", 66, 48, 66, 57, 72, 62]
      ],
      stroke: "carapaceShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Front head
    {
      type: "path",
      commands: [
        ["M", 72, 54],
        ["C", 79, 48, 89, 49, 94, 56],
        ["C", 94, 63, 87, 68, 77, 67],
        ["L", 68, 62],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Head carapace / brow
    {
      type: "path",
      commands: [
        ["M", 77, 54],
        ["C", 83, 52, 89, 54, 92, 58],
        ["L", 86, 61],
        ["C", 82, 59, 78, 59, 75, 61],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 85,
      cy: 57,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    // Acid mouth sac
    {
      type: "path",
      commands: [
        ["M", 82, 63],
        ["C", 86, 62, 90, 64, 91, 67],
        ["C", 88, 71, 82, 71, 78, 67],
        ["Z"]
      ],
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 86,
      cy: 66,
      r: 2,
      fill: "acidCore",
      opacity: 0.85
    },
    // Front left leg
    {
      type: "path",
      commands: [
        ["M", 63, 67],
        ["L", 55, 82],
        ["L", 46, 86],
        ["L", 55, 91],
        ["L", 67, 82],
        ["L", 73, 69],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Front right leg / raised claw
    {
      type: "path",
      commands: [
        ["M", 75, 63],
        ["L", 86, 72],
        ["L", 94, 72],
        ["L", 90, 80],
        ["L", 79, 76],
        ["L", 70, 68],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Claw tips
    {
      type: "path",
      commands: [
        ["M", 14, 78],
        ["L", 7, 82],
        ["L", 20, 84],
        ["Z"],
        ["M", 87, 77],
        ["L", 96, 80],
        ["L", 80, 84],
        ["Z"],
        ["M", 46, 86],
        ["L", 39, 92],
        ["L", 55, 91],
        ["Z"],
        ["M", 94, 72],
        ["L", 99, 75],
        ["L", 90, 80],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Acid sacs on back
    {
      type: "circle",
      cx: 46,
      cy: 52,
      r: 4,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 60,
      cy: 50,
      r: 4.5,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 72,
      cy: 53,
      r: 3.5,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.95
    },
    // Acid sac highlights
    {
      type: "path",
      commands: [
        ["M", 45, 50],
        ["L", 47, 49],
        ["M", 59, 48],
        ["L", 62, 47],
        ["M", 71, 51],
        ["L", 73, 50]
      ],
      stroke: "acidCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Lower body shadow accent
    {
      type: "path",
      commands: [
        ["M", 31, 65],
        ["C", 42, 71, 62, 72, 78, 66]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.65
    }
  ]
};
var zergQueen = {
  id: "zerg.queen",
  commander: "zerg",
  aliases: ["Queen", "QueenLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#8A4F8F",
    skinMid: "#6D3B73",
    skinShade: "#47224F",
    carapaceFill: "#B487C8",
    carapaceMid: "#7D5A8F",
    carapaceShade: "#4A315A",
    clawFill: "#E8D7B2",
    spineFill: "#C6A0D8",
    eyeFill: "#FF5A54",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    darkStroke: "#24122C"
  },
  layers: [
    // Rear abdomen / organic base
    {
      type: "path",
      commands: [
        ["M", 32, 69],
        ["C", 32, 55, 40, 47, 50, 47],
        ["C", 60, 47, 68, 55, 68, 69],
        ["C", 68, 84, 60, 93, 50, 94],
        ["C", 40, 93, 32, 84, 32, 69],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Abdomen front plate
    {
      type: "path",
      commands: [
        ["M", 39, 67],
        ["C", 40, 58, 44, 53, 50, 53],
        ["C", 56, 53, 60, 58, 61, 67],
        ["C", 61, 80, 56, 88, 50, 90],
        ["C", 44, 88, 39, 80, 39, 67],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Back carapace / upper shell
    {
      type: "path",
      commands: [
        ["M", 26, 50],
        ["C", 30, 36, 39, 28, 50, 28],
        ["C", 61, 28, 70, 36, 74, 50],
        ["L", 65, 63],
        ["L", 35, 63],
        ["Z"]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Main torso
    {
      type: "path",
      commands: [
        ["M", 34, 48],
        ["C", 36, 35, 43, 28, 50, 28],
        ["C", 57, 28, 64, 35, 66, 48],
        ["L", 62, 68],
        ["L", 50, 74],
        ["L", 38, 68],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Chest carapace
    {
      type: "path",
      commands: [
        ["M", 38, 49],
        ["C", 41, 41, 45, 37, 50, 37],
        ["C", 55, 37, 59, 41, 62, 49],
        ["L", 58, 61],
        ["L", 50, 66],
        ["L", 42, 61],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Head
    {
      type: "path",
      commands: [
        ["M", 38, 32],
        ["C", 39, 21, 44, 14, 50, 14],
        ["C", 56, 14, 61, 21, 62, 32],
        ["L", 59, 43],
        ["L", 50, 47],
        ["L", 41, 43],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Crown / queen crest
    {
      type: "path",
      commands: [
        ["M", 41, 24],
        ["L", 35, 9],
        ["L", 46, 20],
        ["M", 50, 18],
        ["L", 50, 4],
        ["L", 56, 20],
        ["M", 59, 24],
        ["L", 65, 9],
        ["L", 54, 20]
      ],
      fill: "spineFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round",
      lineCap: "round"
    },
    // Face plate / brow
    {
      type: "path",
      commands: [
        ["M", 41, 31],
        ["C", 44, 26, 47, 24, 50, 24],
        ["C", 53, 24, 56, 26, 59, 31],
        ["L", 56, 37],
        ["L", 44, 37],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Eyes
    {
      type: "circle",
      cx: 46,
      cy: 33,
      r: 2.2,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 54,
      cy: 33,
      r: 2.2,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    // Mouth / mandibles
    {
      type: "path",
      commands: [
        ["M", 45, 40],
        ["L", 39, 48],
        ["L", 47, 44],
        ["M", 55, 40],
        ["L", 61, 48],
        ["L", 53, 44]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2.2,
      lineJoin: "round",
      lineCap: "round"
    },
    // Left upper arm
    {
      type: "path",
      commands: [
        ["M", 35, 52],
        ["L", 22, 42],
        ["L", 13, 47],
        ["L", 20, 55],
        ["L", 34, 61],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right upper arm
    {
      type: "path",
      commands: [
        ["M", 65, 52],
        ["L", 78, 42],
        ["L", 87, 47],
        ["L", 80, 55],
        ["L", 66, 61],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Upper claw tips
    {
      type: "path",
      commands: [
        ["M", 13, 47],
        ["L", 5, 45],
        ["L", 13, 54],
        ["Z"],
        ["M", 87, 47],
        ["L", 95, 45],
        ["L", 87, 54],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Left lower arm
    {
      type: "path",
      commands: [
        ["M", 39, 65],
        ["L", 28, 72],
        ["L", 23, 84],
        ["L", 32, 83],
        ["L", 43, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right lower arm
    {
      type: "path",
      commands: [
        ["M", 61, 65],
        ["L", 72, 72],
        ["L", 77, 84],
        ["L", 68, 83],
        ["L", 57, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Lower claw tips
    {
      type: "path",
      commands: [
        ["M", 23, 84],
        ["L", 16, 91],
        ["L", 32, 83],
        ["Z"],
        ["M", 77, 84],
        ["L", 84, 91],
        ["L", 68, 83],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Small side legs / stabilizers
    {
      type: "path",
      commands: [
        ["M", 35, 76],
        ["L", 25, 92],
        ["M", 65, 76],
        ["L", 75, 92]
      ],
      stroke: "skinShade",
      strokeWidth: 5,
      lineCap: "round"
    },
    // Spawn/acid sacs on abdomen
    {
      type: "circle",
      cx: 44,
      cy: 74,
      r: 3.5,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.5,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 56,
      cy: 74,
      r: 3.5,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.5,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 50,
      cy: 83,
      r: 3.8,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.5,
      opacity: 0.95
    },
    // Carapace seams
    {
      type: "path",
      commands: [
        ["M", 42, 52],
        ["L", 50, 58],
        ["L", 58, 52],
        ["M", 40, 68],
        ["C", 45, 71, 55, 71, 60, 68],
        ["M", 43, 78],
        ["C", 47, 81, 53, 81, 57, 78]
      ],
      stroke: "carapaceShade",
      strokeWidth: 2.3,
      lineCap: "round",
      lineJoin: "round",
      opacity: 0.85
    },
    // Acid highlights
    {
      type: "path",
      commands: [
        ["M", 43, 72],
        ["L", 45, 71],
        ["M", 55, 72],
        ["L", 57, 71],
        ["M", 49, 81],
        ["L", 52, 80]
      ],
      stroke: "acidCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    }
  ]
};
var zergHydralisk = {
  id: "zerg.hydralisk",
  commander: "zerg",
  aliases: ["Hydralisk", "HydraliskLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#83508B",
    skinMid: "#67396F",
    skinShade: "#43234A",
    carapaceFill: "#B98BCC",
    carapaceMid: "#835E92",
    carapaceShade: "#4D355C",
    clawFill: "#E8D7B2",
    spineFill: "#D6B4E5",
    eyeFill: "#FF5A54",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    darkStroke: "#24122C"
  },
  layers: [
    // Coiled lower tail shadow
    {
      type: "path",
      commands: [
        ["M", 29, 82],
        ["C", 35, 72, 47, 69, 58, 73],
        ["C", 70, 77, 75, 87, 67, 93],
        ["C", 58, 99, 39, 97, 29, 89],
        ["C", 24, 86, 24, 84, 29, 82],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Main serpent body
    {
      type: "path",
      commands: [
        ["M", 47, 88],
        ["C", 43, 76, 43, 66, 47, 55],
        ["C", 51, 44, 52, 34, 49, 24],
        ["C", 46, 16, 50, 9, 57, 9],
        ["C", 67, 10, 74, 22, 72, 35],
        ["C", 70, 50, 63, 60, 62, 73],
        ["C", 62, 83, 67, 89, 75, 92],
        ["C", 65, 97, 53, 96, 47, 88],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Chest / belly plate
    {
      type: "path",
      commands: [
        ["M", 53, 35],
        ["C", 58, 43, 58, 56, 55, 70],
        ["C", 53, 80, 55, 87, 61, 92],
        ["C", 55, 92, 51, 89, 50, 83],
        ["C", 48, 70, 51, 55, 50, 44],
        ["C", 50, 39, 50, 36, 53, 35],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Back carapace ridge
    {
      type: "path",
      commands: [
        ["M", 58, 12],
        ["C", 65, 16, 68, 25, 67, 36],
        ["C", 66, 50, 59, 61, 59, 74],
        ["C", 59, 82, 63, 88, 69, 91],
        ["C", 63, 92, 58, 90, 56, 84],
        ["C", 53, 72, 57, 57, 59, 44],
        ["C", 61, 31, 58, 21, 54, 14],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round",
      opacity: 0.95
    },
    // Hood / head crest
    {
      type: "path",
      commands: [
        ["M", 39, 23],
        ["C", 41, 11, 49, 4, 59, 5],
        ["C", 72, 6, 83, 17, 84, 31],
        ["C", 75, 25, 66, 23, 58, 26],
        ["C", 51, 28, 45, 27, 39, 23],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Face / snout
    {
      type: "path",
      commands: [
        ["M", 50, 25],
        ["C", 58, 20, 70, 23, 76, 32],
        ["C", 79, 38, 75, 45, 67, 47],
        ["C", 58, 48, 50, 42, 47, 34],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Face plate / brow
    {
      type: "path",
      commands: [
        ["M", 56, 28],
        ["C", 62, 26, 69, 29, 73, 34],
        ["L", 67, 38],
        ["C", 62, 35, 57, 35, 53, 37],
        ["Z"]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.3,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 65,
      cy: 32,
      r: 2.6,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.4
    },
    // Mouth acid glow
    {
      type: "path",
      commands: [
        ["M", 67, 42],
        ["C", 72, 41, 76, 44, 77, 48],
        ["C", 73, 51, 67, 50, 63, 46],
        ["Z"]
      ],
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 71,
      cy: 46,
      r: 2,
      fill: "acidCore",
      opacity: 0.9
    },
    // Left scythe arm
    {
      type: "path",
      commands: [
        ["M", 48, 47],
        ["C", 39, 49, 31, 57, 27, 68],
        ["L", 17, 73],
        ["L", 24, 80],
        ["L", 35, 72],
        ["C", 40, 65, 45, 58, 51, 54],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left blade
    {
      type: "path",
      commands: [
        ["M", 17, 73],
        ["C", 10, 74, 6, 78, 4, 86],
        ["C", 12, 84, 19, 82, 24, 80],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Right scythe arm
    {
      type: "path",
      commands: [
        ["M", 67, 48],
        ["C", 76, 51, 83, 59, 86, 70],
        ["L", 96, 75],
        ["L", 89, 82],
        ["L", 78, 74],
        ["C", 73, 66, 68, 60, 62, 55],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right blade
    {
      type: "path",
      commands: [
        ["M", 96, 75],
        ["C", 101, 78, 104, 84, 104, 91],
        ["C", 97, 86, 92, 84, 89, 82],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Dorsal quills / spines
    {
      type: "path",
      commands: [
        ["M", 45, 22],
        ["L", 35, 10],
        ["L", 50, 19],
        ["Z"],
        ["M", 53, 15],
        ["L", 51, 1],
        ["L", 60, 14],
        ["Z"],
        ["M", 66, 19],
        ["L", 76, 8],
        ["L", 72, 23],
        ["Z"],
        ["M", 58, 47],
        ["L", 70, 42],
        ["L", 61, 54],
        ["Z"],
        ["M", 56, 60],
        ["L", 68, 59],
        ["L", 58, 68],
        ["Z"]
      ],
      fill: "spineFill",
      stroke: "darkStroke",
      strokeWidth: 2.3,
      lineJoin: "round"
    },
    // Chest / body segmentation
    {
      type: "path",
      commands: [
        ["M", 51, 43],
        ["C", 56, 46, 61, 46, 66, 43],
        ["M", 50, 55],
        ["C", 54, 58, 59, 58, 63, 55],
        ["M", 49, 67],
        ["C", 53, 70, 58, 70, 62, 67],
        ["M", 50, 79],
        ["C", 54, 82, 60, 82, 65, 79]
      ],
      stroke: "carapaceShade",
      strokeWidth: 2.5,
      lineCap: "round",
      opacity: 0.85
    },
    // Lower tail highlight
    {
      type: "path",
      commands: [
        ["M", 34, 84],
        ["C", 43, 89, 57, 90, 68, 86]
      ],
      stroke: "skinMid",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.75
    }
  ]
};
var zergInfestor = {
  id: "zerg.infestor",
  commander: "zerg",
  aliases: ["Infestor", "InfestorLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7E4B87",
    skinMid: "#613568",
    skinShade: "#402247",
    carapaceFill: "#AA7BBC",
    carapaceMid: "#7D5A8F",
    carapaceShade: "#4B3158",
    fungalFill: "#7DFF5A",
    fungalCore: "#D7FF75",
    fungalDark: "#3B9E43",
    eyeFill: "#FF5A54",
    clawFill: "#E8D7B2",
    darkStroke: "#24122C"
  },
  layers: [
    // Rear bloated body shadow
    {
      type: "path",
      commands: [
        ["M", 19, 62],
        ["C", 19, 43, 34, 31, 53, 31],
        ["C", 73, 31, 87, 44, 87, 62],
        ["C", 87, 78, 72, 89, 52, 89],
        ["C", 32, 89, 19, 78, 19, 62],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Main bloated body
    {
      type: "path",
      commands: [
        ["M", 24, 60],
        ["C", 25, 45, 38, 35, 54, 35],
        ["C", 70, 35, 82, 46, 82, 61],
        ["C", 82, 74, 69, 83, 52, 83],
        ["C", 35, 83, 24, 73, 24, 60],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Upper shell plate
    {
      type: "path",
      commands: [
        ["M", 30, 55],
        ["C", 34, 42, 44, 36, 56, 37],
        ["C", 68, 38, 77, 47, 79, 58],
        ["C", 69, 61, 53, 62, 39, 60],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Shell plate segmentation
    {
      type: "path",
      commands: [
        ["M", 42, 39],
        ["C", 39, 45, 39, 54, 43, 60],
        ["M", 55, 37],
        ["C", 52, 45, 52, 54, 56, 61],
        ["M", 68, 43],
        ["C", 64, 49, 64, 56, 69, 60]
      ],
      stroke: "carapaceShade",
      strokeWidth: 2.8,
      lineCap: "round",
      opacity: 0.9
    },
    // Front head/mouth cluster
    {
      type: "path",
      commands: [
        ["M", 67, 54],
        ["C", 76, 49, 88, 52, 94, 62],
        ["C", 94, 70, 87, 75, 77, 73],
        ["L", 66, 67],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Head brow / face plate
    {
      type: "path",
      commands: [
        ["M", 73, 56],
        ["C", 80, 54, 87, 57, 91, 62],
        ["L", 85, 66],
        ["C", 81, 63, 76, 63, 72, 65],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.3,
      lineJoin: "round"
    },
    // Eyes
    {
      type: "circle",
      cx: 82,
      cy: 60,
      r: 2.3,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.3
    },
    {
      type: "circle",
      cx: 88,
      cy: 64,
      r: 1.8,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    // Mouth / mandibles
    {
      type: "path",
      commands: [
        ["M", 80, 69],
        ["L", 75, 78],
        ["L", 84, 72],
        ["M", 88, 69],
        ["L", 94, 76],
        ["L", 85, 73]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineCap: "round",
      lineJoin: "round"
    },
    // Left front tentacle
    {
      type: "path",
      commands: [
        ["M", 43, 74],
        ["C", 35, 80, 28, 88, 18, 90],
        ["C", 25, 82, 30, 75, 39, 68],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right front tentacle
    {
      type: "path",
      commands: [
        ["M", 59, 75],
        ["C", 68, 80, 77, 87, 88, 88],
        ["C", 80, 82, 72, 75, 63, 68],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left side tentacle
    {
      type: "path",
      commands: [
        ["M", 29, 65],
        ["C", 18, 66, 10, 72, 7, 82],
        ["C", 17, 79, 25, 75, 34, 68],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 2.8,
      lineJoin: "round"
    },
    // Rear right tentacle
    {
      type: "path",
      commands: [
        ["M", 70, 66],
        ["C", 80, 66, 90, 70, 97, 78],
        ["C", 86, 78, 76, 75, 67, 70],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 2.8,
      lineJoin: "round"
    },
    // Tentacle claw/tips
    {
      type: "path",
      commands: [
        ["M", 18, 90],
        ["L", 10, 94],
        ["L", 23, 93],
        ["Z"],
        ["M", 88, 88],
        ["L", 96, 92],
        ["L", 83, 92],
        ["Z"],
        ["M", 7, 82],
        ["L", 0, 86],
        ["L", 12, 85],
        ["Z"],
        ["M", 97, 78],
        ["L", 104, 81],
        ["L", 92, 82],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Large fungal sacs
    {
      type: "circle",
      cx: 39,
      cy: 56,
      r: 5.2,
      fill: "fungalFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 53,
      cy: 52,
      r: 6,
      fill: "fungalFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 67,
      cy: 57,
      r: 5,
      fill: "fungalFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 46,
      cy: 70,
      r: 4.5,
      fill: "fungalFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 61,
      cy: 69,
      r: 4.2,
      fill: "fungalFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    // Fungal sac cores / highlights
    {
      type: "path",
      commands: [
        ["M", 37, 54],
        ["L", 41, 53],
        ["M", 51, 49],
        ["L", 56, 48],
        ["M", 65, 55],
        ["L", 69, 54],
        ["M", 44, 68],
        ["L", 48, 67],
        ["M", 59, 67],
        ["L", 63, 66]
      ],
      stroke: "fungalCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.85
    },
    // Belly shadow curve
    {
      type: "path",
      commands: [
        ["M", 30, 68],
        ["C", 42, 76, 63, 77, 77, 69]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.6
    },
    // Small dark pores
    {
      type: "circle",
      cx: 33,
      cy: 62,
      r: 1.5,
      fill: "fungalDark",
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 57,
      cy: 61,
      r: 1.4,
      fill: "fungalDark",
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 72,
      cy: 64,
      r: 1.5,
      fill: "fungalDark",
      opacity: 0.9
    }
  ]
};
var zergCorruptor = {
  id: "zerg.corruptor",
  commander: "zerg",
  aliases: ["Corruptor", "CorruptorLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7E4B87",
    skinMid: "#633769",
    skinShade: "#402247",
    carapaceFill: "#AD7FC0",
    carapaceMid: "#7C5A8A",
    carapaceShade: "#4B3158",
    wingFill: "#8D5A98",
    wingShade: "#56315F",
    corruptionFill: "#8EFF5A",
    corruptionCore: "#D7FF75",
    corruptionDark: "#3B9E43",
    eyeFill: "#FF5A54",
    clawFill: "#E8D7B2",
    darkStroke: "#24122C"
  },
  layers: [
    // Left wing membrane
    {
      type: "path",
      commands: [
        ["M", 43, 42],
        ["C", 31, 30, 17, 27, 6, 34],
        ["C", 14, 42, 21, 50, 25, 61],
        ["C", 31, 56, 38, 51, 47, 49],
        ["Z"]
      ],
      fill: "wingFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Right wing membrane
    {
      type: "path",
      commands: [
        ["M", 57, 42],
        ["C", 69, 30, 83, 27, 94, 34],
        ["C", 86, 42, 79, 50, 75, 61],
        ["C", 69, 56, 62, 51, 53, 49],
        ["Z"]
      ],
      fill: "wingFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Left wing dark inner fold
    {
      type: "path",
      commands: [
        ["M", 18, 38],
        ["C", 24, 43, 30, 49, 34, 56],
        ["M", 31, 33],
        ["C", 35, 39, 40, 44, 45, 48]
      ],
      stroke: "wingShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Right wing dark inner fold
    {
      type: "path",
      commands: [
        ["M", 82, 38],
        ["C", 76, 43, 70, 49, 66, 56],
        ["M", 69, 33],
        ["C", 65, 39, 60, 44, 55, 48]
      ],
      stroke: "wingShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Rear floating tail / stinger
    {
      type: "path",
      commands: [
        ["M", 43, 66],
        ["C", 43, 77, 47, 86, 50, 94],
        ["C", 54, 86, 57, 77, 57, 66],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Main manta body
    {
      type: "path",
      commands: [
        ["M", 30, 48],
        ["C", 33, 33, 42, 24, 50, 23],
        ["C", 58, 24, 67, 33, 70, 48],
        ["C", 72, 63, 63, 74, 50, 76],
        ["C", 37, 74, 28, 63, 30, 48],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Upper carapace
    {
      type: "path",
      commands: [
        ["M", 35, 45],
        ["C", 38, 32, 44, 26, 50, 26],
        ["C", 56, 26, 62, 32, 65, 45],
        ["C", 60, 51, 54, 54, 50, 55],
        ["C", 46, 54, 40, 51, 35, 45],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Hooked head / beak
    {
      type: "path",
      commands: [
        ["M", 41, 29],
        ["C", 44, 18, 50, 10, 57, 9],
        ["C", 66, 9, 72, 18, 69, 28],
        ["C", 67, 36, 59, 41, 50, 40],
        ["C", 45, 39, 42, 35, 41, 29],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Face plate
    {
      type: "path",
      commands: [
        ["M", 50, 24],
        ["C", 55, 20, 63, 22, 66, 28],
        ["L", 61, 33],
        ["C", 57, 30, 52, 30, 48, 33],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 59,
      cy: 27,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.4
    },
    // Corruption mouth sac
    {
      type: "path",
      commands: [
        ["M", 58, 36],
        ["C", 65, 35, 70, 39, 70, 45],
        ["C", 65, 48, 57, 46, 53, 41],
        ["Z"]
      ],
      fill: "corruptionFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 63,
      cy: 42,
      r: 2.2,
      fill: "corruptionCore",
      opacity: 0.9
    },
    // Underside body plate
    {
      type: "path",
      commands: [
        ["M", 38, 57],
        ["C", 45, 62, 55, 62, 62, 57],
        ["L", 58, 70],
        ["L", 50, 74],
        ["L", 42, 70],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Back corruption sacs
    {
      type: "circle",
      cx: 43,
      cy: 49,
      r: 4.2,
      fill: "corruptionFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 54,
      cy: 48,
      r: 4.8,
      fill: "corruptionFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 61,
      cy: 57,
      r: 3.7,
      fill: "corruptionFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    // Sac highlights
    {
      type: "path",
      commands: [
        ["M", 42, 47],
        ["L", 45, 46],
        ["M", 53, 45],
        ["L", 57, 44],
        ["M", 60, 55],
        ["L", 63, 54]
      ],
      stroke: "corruptionCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Lower tentacles
    {
      type: "path",
      commands: [
        ["M", 44, 70],
        ["C", 39, 76, 36, 84, 32, 91],
        ["M", 50, 73],
        ["C", 49, 81, 49, 88, 47, 96],
        ["M", 56, 70],
        ["C", 62, 77, 65, 84, 69, 91]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.95
    },
    // Tentacle tips
    {
      type: "path",
      commands: [
        ["M", 32, 91],
        ["L", 26, 96],
        ["L", 36, 95],
        ["Z"],
        ["M", 47, 96],
        ["L", 43, 101],
        ["L", 52, 100],
        ["Z"],
        ["M", 69, 91],
        ["L", 75, 96],
        ["L", 65, 95],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Outer wing claws/hooks
    {
      type: "path",
      commands: [
        ["M", 6, 34],
        ["L", 0, 29],
        ["L", 3, 42],
        ["Z"],
        ["M", 94, 34],
        ["L", 100, 29],
        ["L", 97, 42],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Carapace seams
    {
      type: "path",
      commands: [
        ["M", 40, 41],
        ["C", 46, 44, 54, 44, 60, 41],
        ["M", 37, 53],
        ["C", 45, 57, 56, 57, 64, 53],
        ["M", 50, 29],
        ["L", 50, 53]
      ],
      stroke: "carapaceShade",
      strokeWidth: 2.4,
      lineCap: "round",
      opacity: 0.85
    }
  ]
};
var zergLurker = {
  id: "zerg.lurker",
  commander: "zerg",
  aliases: ["Lurker", "LurkerLightweight", "LurkerMP"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7B4B83",
    skinMid: "#633769",
    skinShade: "#402247",
    carapaceFill: "#A77CB7",
    carapaceMid: "#7C5A8A",
    carapaceShade: "#4E355B",
    spineFill: "#D6B4E5",
    spikeFill: "#E8D7B2",
    eyeFill: "#FF5A54",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    darkStroke: "#24122C"
  },
  layers: [
    // Underground shadow / burrow mound
    {
      type: "path",
      commands: [
        ["M", 15, 73],
        ["C", 25, 63, 43, 58, 61, 60],
        ["C", 78, 61, 91, 68, 96, 78],
        ["C", 83, 86, 60, 90, 39, 87],
        ["C", 24, 85, 14, 80, 15, 73],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round",
      opacity: 0.95
    },
    // Rear abdomen taper
    {
      type: "path",
      commands: [
        ["M", 18, 59],
        ["C", 10, 57, 8, 50, 14, 46],
        ["C", 22, 41, 33, 47, 37, 55],
        ["L", 33, 67],
        ["C", 27, 65, 22, 62, 18, 59],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Main low body
    {
      type: "path",
      commands: [
        ["M", 25, 59],
        ["C", 30, 43, 44, 34, 60, 36],
        ["C", 75, 38, 87, 48, 90, 61],
        ["C", 88, 72, 76, 78, 59, 78],
        ["C", 41, 79, 29, 72, 25, 59],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Heavy armored shell
    {
      type: "path",
      commands: [
        ["M", 30, 55],
        ["C", 35, 41, 47, 34, 61, 37],
        ["C", 74, 39, 84, 48, 87, 59],
        ["C", 77, 64, 64, 66, 50, 64],
        ["C", 40, 63, 34, 60, 30, 55],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Shell side plates
    {
      type: "path",
      commands: [
        ["M", 38, 47],
        ["C", 36, 53, 38, 59, 43, 63],
        ["M", 51, 39],
        ["C", 48, 47, 49, 57, 55, 65],
        ["M", 66, 41],
        ["C", 63, 49, 65, 58, 72, 63],
        ["M", 78, 49],
        ["C", 75, 54, 77, 59, 82, 61]
      ],
      stroke: "carapaceShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Long dorsal lurker spines
    {
      type: "path",
      commands: [
        ["M", 34, 49],
        ["L", 27, 26],
        ["L", 43, 45],
        ["Z"],
        ["M", 45, 42],
        ["L", 43, 13],
        ["L", 55, 41],
        ["Z"],
        ["M", 58, 39],
        ["L", 64, 10],
        ["L", 67, 43],
        ["Z"],
        ["M", 71, 44],
        ["L", 84, 21],
        ["L", 80, 50],
        ["Z"]
      ],
      fill: "spineFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Front head
    {
      type: "path",
      commands: [
        ["M", 72, 55],
        ["C", 80, 50, 90, 52, 95, 60],
        ["C", 95, 67, 87, 72, 77, 70],
        ["L", 67, 64],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Head armor brow
    {
      type: "path",
      commands: [
        ["M", 77, 56],
        ["C", 83, 54, 90, 56, 93, 61],
        ["L", 87, 65],
        ["C", 83, 62, 78, 62, 74, 64],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 86,
      cy: 59,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.4
    },
    // Front digging claw left
    {
      type: "path",
      commands: [
        ["M", 69, 68],
        ["L", 80, 78],
        ["L", 94, 80],
        ["L", 88, 88],
        ["L", 75, 83],
        ["L", 63, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Front digging claw right
    {
      type: "path",
      commands: [
        ["M", 58, 70],
        ["L", 50, 84],
        ["L", 39, 90],
        ["L", 52, 93],
        ["L", 65, 82],
        ["L", 70, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Rear stabilizing leg
    {
      type: "path",
      commands: [
        ["M", 34, 67],
        ["L", 23, 80],
        ["L", 12, 82],
        ["L", 21, 88],
        ["L", 35, 80],
        ["L", 42, 69],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Claw tips
    {
      type: "path",
      commands: [
        ["M", 94, 80],
        ["L", 101, 84],
        ["L", 88, 88],
        ["Z"],
        ["M", 39, 90],
        ["L", 31, 96],
        ["L", 52, 93],
        ["Z"],
        ["M", 12, 82],
        ["L", 5, 86],
        ["L", 21, 88],
        ["Z"]
      ],
      fill: "spikeFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Burrow attack spikes emerging from ground
    {
      type: "path",
      commands: [
        ["M", 24, 82],
        ["L", 29, 63],
        ["L", 34, 83],
        ["Z"],
        ["M", 48, 87],
        ["L", 54, 67],
        ["L", 59, 88],
        ["Z"],
        ["M", 76, 83],
        ["L", 82, 65],
        ["L", 87, 84],
        ["Z"]
      ],
      fill: "spikeFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round",
      opacity: 0.95
    },
    // Acid / sensory sacs
    {
      type: "circle",
      cx: 47,
      cy: 55,
      r: 3.6,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.8,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 62,
      cy: 53,
      r: 4.2,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.8,
      opacity: 0.95
    },
    // Sac highlights
    {
      type: "path",
      commands: [
        ["M", 46, 53],
        ["L", 49, 52],
        ["M", 61, 51],
        ["L", 64, 50]
      ],
      stroke: "acidCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Lower body shadow accent
    {
      type: "path",
      commands: [
        ["M", 31, 67],
        ["C", 43, 73, 64, 74, 80, 67]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.65
    }
  ]
};
var zergMutalisk = {
  id: "zerg.mutalisk",
  commander: "zerg",
  aliases: ["Mutalisk", "MutaliskLightweight", "ZergAir"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7E4B87",
    skinMid: "#633769",
    skinShade: "#402247",
    wingFill: "#9A63A6",
    wingMid: "#7B4D87",
    wingShade: "#4D2B56",
    carapaceFill: "#B98BCC",
    carapaceMid: "#805A91",
    carapaceShade: "#4A3158",
    clawFill: "#E8D7B2",
    spineFill: "#D6B4E5",
    eyeFill: "#FF5A54",
    glaiveFill: "#8EFF5A",
    glaiveCore: "#D7FF75",
    darkStroke: "#24122C"
  },
  layers: [
    // Rear tail
    {
      type: "path",
      commands: [
        ["M", 42, 66],
        ["C", 36, 78, 32, 88, 25, 97],
        ["C", 38, 94, 48, 86, 53, 72],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left wing outer crescent
    {
      type: "path",
      commands: [
        ["M", 44, 44],
        ["C", 31, 25, 15, 17, 2, 22],
        ["C", 8, 32, 14, 44, 18, 58],
        ["C", 26, 53, 35, 49, 48, 50],
        ["Z"]
      ],
      fill: "wingFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Right wing outer crescent
    {
      type: "path",
      commands: [
        ["M", 56, 44],
        ["C", 69, 25, 85, 17, 98, 22],
        ["C", 92, 32, 86, 44, 82, 58],
        ["C", 74, 53, 65, 49, 52, 50],
        ["Z"]
      ],
      fill: "wingFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Left inner wing fold
    {
      type: "path",
      commands: [
        ["M", 12, 27],
        ["C", 20, 35, 27, 43, 33, 53],
        ["M", 27, 24],
        ["C", 31, 33, 38, 41, 46, 48]
      ],
      stroke: "wingShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Right inner wing fold
    {
      type: "path",
      commands: [
        ["M", 88, 27],
        ["C", 80, 35, 73, 43, 67, 53],
        ["M", 73, 24],
        ["C", 69, 33, 62, 41, 54, 48]
      ],
      stroke: "wingShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Wing lower hooks
    {
      type: "path",
      commands: [
        ["M", 18, 58],
        ["L", 9, 66],
        ["L", 24, 64],
        ["Z"],
        ["M", 82, 58],
        ["L", 91, 66],
        ["L", 76, 64],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Main body
    {
      type: "path",
      commands: [
        ["M", 34, 48],
        ["C", 36, 35, 44, 27, 53, 27],
        ["C", 63, 28, 70, 38, 69, 51],
        ["C", 68, 64, 59, 73, 48, 73],
        ["C", 39, 71, 33, 61, 34, 48],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Upper carapace
    {
      type: "path",
      commands: [
        ["M", 39, 46],
        ["C", 42, 34, 48, 29, 54, 30],
        ["C", 61, 31, 66, 39, 65, 49],
        ["C", 60, 53, 52, 54, 44, 52],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Head / snout
    {
      type: "path",
      commands: [
        ["M", 48, 30],
        ["C", 51, 18, 59, 10, 68, 11],
        ["C", 76, 13, 80, 22, 77, 31],
        ["C", 74, 40, 64, 43, 55, 39],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Head plate
    {
      type: "path",
      commands: [
        ["M", 58, 24],
        ["C", 63, 21, 70, 23, 74, 29],
        ["L", 69, 34],
        ["C", 65, 31, 60, 31, 56, 34],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.4,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 67,
      cy: 28,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.4
    },
    // Mouth / projectile sac
    {
      type: "path",
      commands: [
        ["M", 67, 38],
        ["C", 73, 37, 78, 41, 78, 46],
        ["C", 73, 49, 66, 47, 62, 43],
        ["Z"]
      ],
      fill: "glaiveFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 72,
      cy: 43,
      r: 2,
      fill: "glaiveCore",
      opacity: 0.9
    },
    // Lower body / abdomen
    {
      type: "path",
      commands: [
        ["M", 40, 58],
        ["C", 46, 63, 56, 64, 63, 59],
        ["L", 58, 70],
        ["L", 49, 75],
        ["L", 42, 70],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Body spines
    {
      type: "path",
      commands: [
        ["M", 42, 42],
        ["L", 34, 30],
        ["L", 47, 39],
        ["Z"],
        ["M", 52, 31],
        ["L", 52, 16],
        ["L", 60, 31],
        ["Z"],
        ["M", 62, 38],
        ["L", 74, 27],
        ["L", 68, 42],
        ["Z"]
      ],
      fill: "spineFill",
      stroke: "darkStroke",
      strokeWidth: 2.3,
      lineJoin: "round"
    },
    // Wing claws / tips
    {
      type: "path",
      commands: [
        ["M", 2, 22],
        ["L", -4, 17],
        ["L", -1, 31],
        ["Z"],
        ["M", 98, 22],
        ["L", 104, 17],
        ["L", 101, 31],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Small underside talons
    {
      type: "path",
      commands: [
        ["M", 45, 70],
        ["L", 39, 80],
        ["L", 48, 74],
        ["M", 56, 70],
        ["L", 62, 80],
        ["L", 53, 74]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2.2,
      lineCap: "round",
      lineJoin: "round"
    },
    // Glaive sacs on body
    {
      type: "circle",
      cx: 47,
      cy: 52,
      r: 3.5,
      fill: "glaiveFill",
      stroke: "darkStroke",
      strokeWidth: 1.7,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 59,
      cy: 51,
      r: 3.8,
      fill: "glaiveFill",
      stroke: "darkStroke",
      strokeWidth: 1.7,
      opacity: 0.95
    },
    // Glaive sac highlights
    {
      type: "path",
      commands: [
        ["M", 46, 50],
        ["L", 49, 49],
        ["M", 58, 49],
        ["L", 61, 48]
      ],
      stroke: "glaiveCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Body seams
    {
      type: "path",
      commands: [
        ["M", 43, 47],
        ["C", 49, 50, 58, 50, 63, 47],
        ["M", 42, 59],
        ["C", 48, 63, 57, 63, 63, 59],
        ["M", 51, 32],
        ["L", 51, 57]
      ],
      stroke: "carapaceShade",
      strokeWidth: 2.3,
      lineCap: "round",
      opacity: 0.85
    }
  ]
};
var zergSwarmHost = {
  id: "zerg.swarm_host",
  commander: "zerg",
  aliases: ["Swarm Host", "SwarmHost", "SwarmHostLightweight", "SwarmHostMP"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7E4B87",
    skinMid: "#633769",
    skinShade: "#402247",
    carapaceFill: "#AA7BBC",
    carapaceMid: "#7D5A8F",
    carapaceShade: "#4B3158",
    spawnFill: "#8EFF5A",
    spawnCore: "#D7FF75",
    spawnDark: "#3B9E43",
    locustFill: "#9A63A6",
    clawFill: "#E8D7B2",
    eyeFill: "#FF5A54",
    darkStroke: "#24122C"
  },
  layers: [
    // Rear abdomen shadow / brood sac mass
    {
      type: "path",
      commands: [
        ["M", 15, 63],
        ["C", 15, 43, 31, 30, 53, 31],
        ["C", 77, 32, 92, 47, 91, 66],
        ["C", 90, 82, 74, 91, 52, 91],
        ["C", 30, 91, 15, 81, 15, 63],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Main swollen body
    {
      type: "path",
      commands: [
        ["M", 22, 61],
        ["C", 24, 45, 37, 35, 55, 36],
        ["C", 73, 37, 85, 49, 85, 64],
        ["C", 84, 77, 70, 85, 52, 85],
        ["C", 34, 85, 22, 75, 22, 61],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Heavy upper carapace shell
    {
      type: "path",
      commands: [
        ["M", 27, 55],
        ["C", 32, 40, 44, 32, 59, 35],
        ["C", 73, 38, 82, 48, 84, 60],
        ["C", 74, 64, 59, 66, 44, 64],
        ["C", 35, 63, 30, 60, 27, 55],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Shell segmentation
    {
      type: "path",
      commands: [
        ["M", 38, 40],
        ["C", 35, 47, 36, 57, 41, 63],
        ["M", 52, 34],
        ["C", 49, 44, 50, 56, 56, 65],
        ["M", 67, 40],
        ["C", 63, 48, 65, 57, 72, 62]
      ],
      stroke: "carapaceShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Head / front cluster
    {
      type: "path",
      commands: [
        ["M", 69, 55],
        ["C", 78, 50, 90, 53, 95, 62],
        ["C", 95, 70, 87, 76, 76, 74],
        ["L", 66, 67],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Head brow
    {
      type: "path",
      commands: [
        ["M", 75, 57],
        ["C", 82, 55, 90, 58, 93, 63],
        ["L", 86, 67],
        ["C", 82, 64, 77, 64, 73, 66],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.3,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 85,
      cy: 61,
      r: 2.4,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.3
    },
    // Large spawning sacs
    {
      type: "circle",
      cx: 38,
      cy: 60,
      r: 5,
      fill: "spawnFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 52,
      cy: 56,
      r: 6,
      fill: "spawnFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 67,
      cy: 61,
      r: 5,
      fill: "spawnFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 45,
      cy: 73,
      r: 4.8,
      fill: "spawnFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    {
      type: "circle",
      cx: 60,
      cy: 73,
      r: 4.5,
      fill: "spawnFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      opacity: 0.96
    },
    // Spawn sac highlights
    {
      type: "path",
      commands: [
        ["M", 36, 58],
        ["L", 40, 57],
        ["M", 50, 53],
        ["L", 55, 52],
        ["M", 65, 59],
        ["L", 69, 58],
        ["M", 43, 71],
        ["L", 47, 70],
        ["M", 58, 71],
        ["L", 62, 70]
      ],
      stroke: "spawnCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.85
    },
    // Left rear leg
    {
      type: "path",
      commands: [
        ["M", 31, 70],
        ["L", 20, 81],
        ["L", 9, 83],
        ["L", 18, 90],
        ["L", 32, 82],
        ["L", 40, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right rear leg
    {
      type: "path",
      commands: [
        ["M", 70, 70],
        ["L", 81, 81],
        ["L", 92, 83],
        ["L", 83, 90],
        ["L", 69, 82],
        ["L", 61, 72],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Left front digging leg
    {
      type: "path",
      commands: [
        ["M", 61, 73],
        ["L", 50, 86],
        ["L", 38, 91],
        ["L", 51, 95],
        ["L", 65, 84],
        ["L", 72, 74],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right front digging leg
    {
      type: "path",
      commands: [
        ["M", 75, 70],
        ["L", 87, 78],
        ["L", 98, 79],
        ["L", 91, 87],
        ["L", 78, 83],
        ["L", 68, 74],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Claw tips
    {
      type: "path",
      commands: [
        ["M", 9, 83],
        ["L", 2, 88],
        ["L", 18, 90],
        ["Z"],
        ["M", 92, 83],
        ["L", 99, 88],
        ["L", 83, 90],
        ["Z"],
        ["M", 38, 91],
        ["L", 30, 97],
        ["L", 51, 95],
        ["Z"],
        ["M", 98, 79],
        ["L", 105, 83],
        ["L", 91, 87],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Dorsal spawn vents / small spines
    {
      type: "path",
      commands: [
        ["M", 34, 50],
        ["L", 30, 35],
        ["L", 41, 48],
        ["Z"],
        ["M", 49, 43],
        ["L", 50, 25],
        ["L", 58, 44],
        ["Z"],
        ["M", 65, 47],
        ["L", 75, 32],
        ["L", 72, 51],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Tiny locust silhouette emerging from back
    {
      type: "path",
      commands: [
        ["M", 47, 30],
        ["C", 48, 22, 53, 18, 59, 19],
        ["C", 64, 20, 67, 25, 65, 31],
        ["L", 59, 36],
        ["L", 51, 34],
        ["Z"]
      ],
      fill: "locustFill",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round",
      opacity: 0.95
    },
    {
      type: "path",
      commands: [
        ["M", 52, 25],
        ["L", 43, 18],
        ["L", 49, 29],
        ["M", 62, 26],
        ["L", 73, 21],
        ["L", 64, 31]
      ],
      stroke: "darkStroke",
      strokeWidth: 2.2,
      lineCap: "round",
      lineJoin: "round"
    },
    // Belly shadow
    {
      type: "path",
      commands: [
        ["M", 28, 69],
        ["C", 41, 78, 64, 79, 80, 70]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.6
    },
    // Small dark pores
    {
      type: "circle",
      cx: 33,
      cy: 65,
      r: 1.4,
      fill: "spawnDark",
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 56,
      cy: 64,
      r: 1.4,
      fill: "spawnDark",
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 72,
      cy: 67,
      r: 1.4,
      fill: "spawnDark",
      opacity: 0.9
    }
  ]
};
var zergUltralisk = {
  id: "zerg.ultralisk",
  commander: "zerg",
  aliases: ["Ultralisk", "UltraliskLightweight"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7E4B87",
    skinMid: "#633769",
    skinShade: "#402247",
    carapaceFill: "#AD7FC0",
    carapaceMid: "#7C5A8A",
    carapaceShade: "#4B3158",
    tuskFill: "#E8D7B2",
    tuskShade: "#BBA47F",
    eyeFill: "#FF5A54",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    darkStroke: "#24122C"
  },
  layers: [
    // Rear massive body shadow
    {
      type: "path",
      commands: [
        ["M", 13, 61],
        ["C", 16, 41, 34, 28, 56, 29],
        ["C", 76, 30, 91, 44, 93, 62],
        ["C", 95, 78, 80, 90, 57, 91],
        ["C", 34, 92, 12, 80, 13, 61],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Main huge body
    {
      type: "path",
      commands: [
        ["M", 19, 60],
        ["C", 22, 43, 37, 33, 56, 34],
        ["C", 74, 35, 86, 47, 88, 62],
        ["C", 89, 75, 76, 84, 56, 85],
        ["C", 36, 86, 19, 75, 19, 60],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Heavy top carapace shell
    {
      type: "path",
      commands: [
        ["M", 25, 55],
        ["C", 31, 39, 44, 31, 59, 34],
        ["C", 74, 37, 84, 48, 86, 60],
        ["C", 75, 65, 60, 67, 44, 64],
        ["C", 35, 63, 29, 60, 25, 55],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Carapace plates / segmentation
    {
      type: "path",
      commands: [
        ["M", 36, 41],
        ["C", 33, 48, 35, 57, 41, 63],
        ["M", 51, 34],
        ["C", 47, 44, 49, 57, 56, 65],
        ["M", 67, 40],
        ["C", 63, 48, 65, 57, 73, 62],
        ["M", 79, 50],
        ["C", 76, 55, 78, 59, 83, 61]
      ],
      stroke: "carapaceShade",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    },
    // Head / front skull mass
    {
      type: "path",
      commands: [
        ["M", 69, 51],
        ["C", 79, 45, 92, 49, 98, 60],
        ["C", 99, 70, 90, 77, 77, 75],
        ["L", 65, 68],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    // Head armor brow
    {
      type: "path",
      commands: [
        ["M", 74, 53],
        ["C", 82, 50, 91, 54, 95, 61],
        ["L", 88, 66],
        ["C", 83, 62, 78, 62, 73, 65],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    // Eye
    {
      type: "circle",
      cx: 86,
      cy: 58,
      r: 2.6,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1.4
    },
    // Left giant tusk blade
    {
      type: "path",
      commands: [
        ["M", 74, 66],
        ["C", 62, 69, 45, 78, 31, 92],
        ["C", 51, 89, 68, 82, 82, 71],
        ["Z"]
      ],
      fill: "tuskFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Right giant tusk blade
    {
      type: "path",
      commands: [
        ["M", 84, 66],
        ["C", 93, 69, 103, 76, 109, 89],
        ["C", 96, 87, 85, 80, 78, 71],
        ["Z"]
      ],
      fill: "tuskFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Tusk shade lines
    {
      type: "path",
      commands: [
        ["M", 50, 85],
        ["C", 61, 80, 71, 75, 80, 69],
        ["M", 93, 82],
        ["C", 88, 77, 84, 72, 80, 68]
      ],
      stroke: "tuskShade",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Front left massive leg
    {
      type: "path",
      commands: [
        ["M", 63, 74],
        ["L", 52, 88],
        ["L", 39, 93],
        ["L", 53, 97],
        ["L", 68, 86],
        ["L", 74, 76],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Front right massive leg
    {
      type: "path",
      commands: [
        ["M", 76, 71],
        ["L", 89, 80],
        ["L", 100, 80],
        ["L", 94, 89],
        ["L", 80, 85],
        ["L", 68, 75],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Rear left leg
    {
      type: "path",
      commands: [
        ["M", 32, 72],
        ["L", 21, 84],
        ["L", 9, 86],
        ["L", 19, 93],
        ["L", 34, 84],
        ["L", 42, 74],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    // Rear right support leg
    {
      type: "path",
      commands: [
        ["M", 48, 77],
        ["L", 42, 91],
        ["L", 30, 96],
        ["L", 45, 98],
        ["L", 56, 87],
        ["L", 58, 78],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    // Claw tips
    {
      type: "path",
      commands: [
        ["M", 39, 93],
        ["L", 31, 99],
        ["L", 53, 97],
        ["Z"],
        ["M", 100, 80],
        ["L", 107, 85],
        ["L", 94, 89],
        ["Z"],
        ["M", 9, 86],
        ["L", 2, 91],
        ["L", 19, 93],
        ["Z"],
        ["M", 30, 96],
        ["L", 22, 101],
        ["L", 45, 98],
        ["Z"]
      ],
      fill: "tuskFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    },
    // Dorsal armor spikes
    {
      type: "path",
      commands: [
        ["M", 31, 50],
        ["L", 26, 31],
        ["L", 40, 48],
        ["Z"],
        ["M", 45, 39],
        ["L", 47, 17],
        ["L", 55, 40],
        ["Z"],
        ["M", 62, 39],
        ["L", 72, 20],
        ["L", 70, 44],
        ["Z"],
        ["M", 76, 48],
        ["L", 90, 34],
        ["L", 82, 54],
        ["Z"]
      ],
      fill: "carapaceMid",
      stroke: "darkStroke",
      strokeWidth: 2.7,
      lineJoin: "round"
    },
    // Small acid/bio sacs for Zerg color identity
    {
      type: "circle",
      cx: 47,
      cy: 58,
      r: 3.8,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.8,
      opacity: 0.95
    },
    {
      type: "circle",
      cx: 63,
      cy: 56,
      r: 4.2,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 1.8,
      opacity: 0.95
    },
    // Acid highlights
    {
      type: "path",
      commands: [
        ["M", 46, 56],
        ["L", 49, 55],
        ["M", 62, 54],
        ["L", 65, 53]
      ],
      stroke: "acidCore",
      strokeWidth: 2,
      lineCap: "round",
      opacity: 0.8
    },
    // Lower body shadow curve
    {
      type: "path",
      commands: [
        ["M", 28, 68],
        ["C", 43, 77, 67, 78, 82, 68]
      ],
      stroke: "skinShade",
      strokeWidth: 4,
      lineCap: "round",
      opacity: 0.65
    }
  ]
};
var zergOverseer = {
  id: "zerg.overseer",
  commander: "zerg",
  aliases: ["Overseer"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#87518F",
    skinMid: "#6B3A73",
    skinShade: "#43234A",
    carapaceFill: "#B487C8",
    carapaceShade: "#5A3A66",
    eyeFill: "#FF5A54",
    bioGlow: "#8EFF5A",
    bioCore: "#D7FF75",
    clawFill: "#E8D7B2",
    darkStroke: "#24122C"
  },
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 15],
        ["C", 69, 17, 83, 31, 85, 50],
        ["C", 83, 69, 68, 83, 50, 86],
        ["C", 32, 83, 17, 69, 15, 50],
        ["C", 17, 31, 31, 17, 50, 15],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 26, 48],
        ["C", 29, 34, 39, 26, 50, 26],
        ["C", 61, 26, 71, 34, 74, 48],
        ["C", 68, 56, 59, 60, 50, 60],
        ["C", 41, 60, 32, 56, 26, 48],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 52,
      r: 15,
      fill: "bioGlow",
      stroke: "darkStroke",
      strokeWidth: 3,
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 50,
      cy: 52,
      r: 7,
      fill: "bioCore",
      stroke: "darkStroke",
      strokeWidth: 1.5
    },
    {
      type: "path",
      commands: [
        ["M", 23, 49],
        ["L", 8, 37],
        ["L", 15, 55],
        ["M", 77, 49],
        ["L", 92, 37],
        ["L", 85, 55],
        ["M", 32, 70],
        ["L", 20, 87],
        ["M", 68, 70],
        ["L", 80, 87]
      ],
      stroke: "skinMid",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 36, 26],
        ["L", 31, 10],
        ["L", 45, 24],
        ["M", 64, 26],
        ["L", 69, 10],
        ["L", 55, 24]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 43,
      cy: 45,
      r: 2.6,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 57,
      cy: 45,
      r: 2.6,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    }
  ]
};
var zergRavager = {
  id: "zerg.ravager",
  commander: "zerg",
  aliases: ["Ravager"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7B4B83",
    skinMid: "#633769",
    skinShade: "#43234A",
    carapaceFill: "#AA7BBC",
    carapaceShade: "#4E355B",
    bileFill: "#8EFF5A",
    bileCore: "#D7FF75",
    clawFill: "#E8D7B2",
    eyeFill: "#FF5A54",
    darkStroke: "#24122C"
  },
  layers: [
    {
      type: "path",
      commands: [
        ["M", 18, 61],
        ["C", 21, 43, 37, 32, 58, 34],
        ["C", 76, 36, 90, 48, 92, 63],
        ["C", 91, 78, 76, 88, 55, 88],
        ["C", 34, 88, 18, 77, 18, 61],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 27, 55],
        ["C", 33, 39, 45, 31, 60, 35],
        ["C", 75, 39, 86, 50, 88, 62],
        ["C", 77, 66, 61, 67, 44, 64],
        ["C", 35, 62, 30, 59, 27, 55],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 40, 42],
        ["L", 45, 21],
        ["L", 52, 43],
        ["M", 57, 39],
        ["L", 68, 20],
        ["L", 66, 45],
        ["M", 72, 47],
        ["L", 88, 34],
        ["L", 80, 54]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 52,
      cy: 53,
      r: 6,
      fill: "bileFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "circle",
      cx: 68,
      cy: 56,
      r: 5,
      fill: "bileFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [
        ["M", 70, 58],
        ["C", 79, 52, 91, 55, 96, 64],
        ["C", 94, 72, 84, 77, 73, 73],
        ["L", 64, 66],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 84,
      cy: 62,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "path",
      commands: [
        ["M", 32, 70],
        ["L", 20, 83],
        ["L", 8, 85],
        ["L", 18, 91],
        ["L", 34, 82],
        ["Z"],
        ["M", 68, 72],
        ["L", 81, 82],
        ["L", 94, 82],
        ["L", 85, 90],
        ["L", 70, 83],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    }
  ]
};
var zergBaneling = {
  id: "zerg.baneling",
  commander: "zerg",
  aliases: ["Baneling"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#7B4B83",
    skinShade: "#43234A",
    carapaceFill: "#A77CB7",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    eyeFill: "#FF5A54",
    darkStroke: "#24122C"
  },
  layers: [
    {
      type: "circle",
      cx: 50,
      cy: 57,
      r: 31,
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4
    },
    {
      type: "circle",
      cx: 50,
      cy: 54,
      r: 25,
      fill: "acidFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      opacity: 0.92
    },
    {
      type: "path",
      commands: [
        ["M", 28, 44],
        ["C", 36, 29, 56, 24, 72, 35],
        ["C", 65, 41, 56, 44, 45, 44],
        ["C", 38, 44, 32, 44, 28, 44],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 45,
      cy: 51,
      r: 7,
      fill: "acidCore",
      opacity: 0.9
    },
    {
      type: "circle",
      cx: 60,
      cy: 61,
      r: 6,
      fill: "acidCore",
      opacity: 0.75
    },
    {
      type: "path",
      commands: [
        ["M", 28, 70],
        ["L", 14, 82],
        ["L", 24, 86],
        ["L", 37, 75],
        ["M", 72, 70],
        ["L", 86, 82],
        ["L", 76, 86],
        ["L", 63, 75]
      ],
      stroke: "skinFill",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 66,
      cy: 42,
      r: 2.6,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    }
  ]
};
var zergViper = {
  id: "zerg.viper",
  commander: "zerg",
  aliases: ["Viper"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#86508E",
    skinMid: "#6B3A73",
    skinShade: "#43234A",
    carapaceFill: "#B487C8",
    carapaceShade: "#5A3A66",
    acidFill: "#8EFF5A",
    acidCore: "#D7FF75",
    eyeFill: "#FF5A54",
    darkStroke: "#24122C"
  },
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 13],
        ["C", 62, 25, 70, 42, 69, 61],
        ["C", 64, 77, 56, 88, 50, 94],
        ["C", 44, 88, 36, 77, 31, 61],
        ["C", 30, 42, 38, 25, 50, 13],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 30],
        ["C", 43, 21, 47, 16, 50, 13],
        ["C", 53, 16, 57, 21, 61, 30],
        ["L", 58, 56],
        ["L", 50, 68],
        ["L", 42, 56],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 37, 45],
        ["C", 24, 38, 13, 34, 4, 36],
        ["C", 13, 49, 25, 58, 39, 60],
        ["Z"],
        ["M", 63, 45],
        ["C", 76, 38, 87, 34, 96, 36],
        ["C", 87, 49, 75, 58, 61, 60],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 35, 62],
        ["L", 22, 75],
        ["L", 27, 83],
        ["L", 44, 69],
        ["M", 65, 62],
        ["L", 78, 75],
        ["L", 73, 83],
        ["L", 56, 69]
      ],
      stroke: "skinMid",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 44,
      cy: 35,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "circle",
      cx: 56,
      cy: 35,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "path",
      commands: [
        ["M", 46, 56],
        ["C", 48, 64, 52, 64, 54, 56],
        ["M", 40, 72],
        ["C", 46, 80, 54, 80, 60, 72]
      ],
      stroke: "acidFill",
      strokeWidth: 3,
      lineCap: "round",
      opacity: 0.9
    }
  ]
};
var zergBroodLord = {
  id: "zerg.broodLord",
  commander: "zerg",
  aliases: ["Brood Lord", "BroodLord"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#80508A",
    skinMid: "#65376E",
    skinShade: "#402247",
    carapaceFill: "#AD7FC0",
    carapaceShade: "#4B3158",
    broodFill: "#8EFF5A",
    broodCore: "#D7FF75",
    clawFill: "#E8D7B2",
    eyeFill: "#FF5A54",
    darkStroke: "#24122C"
  },
  layers: [
    {
      type: "path",
      commands: [
        ["M", 50, 16],
        ["C", 68, 19, 83, 35, 85, 55],
        ["C", 82, 76, 67, 89, 50, 91],
        ["C", 33, 89, 18, 76, 15, 55],
        ["C", 17, 35, 32, 19, 50, 16],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 25, 51],
        ["C", 30, 34, 41, 25, 50, 25],
        ["C", 59, 25, 70, 34, 75, 51],
        ["C", 66, 58, 56, 61, 50, 61],
        ["C", 44, 61, 34, 58, 25, 51],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 31, 48],
        ["C", 18, 42, 8, 38, 1, 42],
        ["C", 10, 55, 23, 65, 38, 66],
        ["Z"],
        ["M", 69, 48],
        ["C", 82, 42, 92, 38, 99, 42],
        ["C", 90, 55, 77, 65, 62, 66],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 42,
      cy: 63,
      r: 6,
      fill: "broodFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "circle",
      cx: 58,
      cy: 63,
      r: 6,
      fill: "broodFill",
      stroke: "darkStroke",
      strokeWidth: 2
    },
    {
      type: "path",
      commands: [
        ["M", 36, 76],
        ["L", 20, 90],
        ["L", 32, 93],
        ["L", 47, 79],
        ["M", 64, 76],
        ["L", 80, 90],
        ["L", 68, 93],
        ["L", 53, 79]
      ],
      stroke: "skinMid",
      strokeWidth: 5,
      lineCap: "round",
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 33],
        ["L", 34, 15],
        ["L", 47, 31],
        ["M", 61, 33],
        ["L", 66, 15],
        ["L", 53, 31]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 50,
      cy: 48,
      r: 3,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    }
  ]
};
var zergLocust = {
  id: "zerg.locust",
  commander: "zerg",
  aliases: ["Locust", "LocustMPPrecursor"],
  viewBox: { width: 100, height: 100 },
  tokens: {
    skinFill: "#8C5A92",
    skinMid: "#6F4374",
    skinShade: "#4F2D56",
    carapaceFill: "#B78AC8",
    carapaceShade: "#7C5A8A",
    clawFill: "#E7D9B8",
    eyeFill: "#FF5A54",
    darkStroke: "#2A1630"
  },
  layers: [
    {
      type: "path",
      commands: [
        ["M", 25, 63],
        ["C", 29, 49, 43, 39, 59, 40],
        ["C", 74, 41, 86, 50, 88, 62],
        ["C", 86, 75, 72, 82, 54, 82],
        ["C", 38, 82, 27, 74, 25, 63],
        ["Z"]
      ],
      fill: "skinFill",
      stroke: "darkStroke",
      strokeWidth: 4,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 33, 56],
        ["C", 41, 43, 55, 38, 70, 44],
        ["C", 76, 47, 82, 54, 85, 61],
        ["C", 72, 61, 58, 59, 43, 64],
        ["Z"]
      ],
      fill: "carapaceFill",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 67, 53],
        ["C", 76, 48, 88, 51, 94, 59],
        ["C", 93, 66, 84, 70, 73, 68],
        ["L", 63, 62],
        ["Z"]
      ],
      fill: "skinShade",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "circle",
      cx: 84,
      cy: 58,
      r: 2.5,
      fill: "eyeFill",
      stroke: "darkStroke",
      strokeWidth: 1
    },
    {
      type: "path",
      commands: [
        ["M", 35, 66],
        ["L", 22, 80],
        ["L", 12, 82],
        ["L", 20, 89],
        ["L", 36, 78],
        ["Z"],
        ["M", 62, 68],
        ["L", 76, 81],
        ["L", 88, 82],
        ["L", 80, 90],
        ["L", 64, 79],
        ["Z"]
      ],
      fill: "skinMid",
      stroke: "darkStroke",
      strokeWidth: 3,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 39, 51],
        ["L", 36, 33],
        ["L", 48, 49],
        ["M", 56, 44],
        ["L", 60, 25],
        ["L", 65, 47],
        ["M", 72, 49],
        ["L", 84, 36],
        ["L", 78, 56]
      ],
      fill: "carapaceShade",
      stroke: "darkStroke",
      strokeWidth: 2.5,
      lineJoin: "round"
    },
    {
      type: "path",
      commands: [
        ["M", 12, 82],
        ["L", 5, 87],
        ["L", 20, 89],
        ["Z"],
        ["M", 88, 82],
        ["L", 96, 87],
        ["L", 80, 90],
        ["Z"]
      ],
      fill: "clawFill",
      stroke: "darkStroke",
      strokeWidth: 2,
      lineJoin: "round"
    }
  ]
};
var zergUnits = {
  zergling: zergZergling,
  roach: zergRoach,
  queen: zergQueen,
  hydralisk: zergHydralisk,
  infestor: zergInfestor,
  corruptor: zergCorruptor,
  lurker: zergLurker,
  mutalisk: zergMutalisk,
  swarmhost: zergSwarmHost,
  ultralisk: zergUltralisk,
  overseer: zergOverseer,
  ravager: zergRavager,
  baneling: zergBaneling,
  viper: zergViper,
  broodLord: zergBroodLord,
  locust: zergLocust
};

// TypeScript/unitIcons.ts
var definitions = [
  ...Object.values(protossUnits),
  ...Object.values(terranUnits),
  ...Object.values(zergUnits)
];
var aliases = /* @__PURE__ */ new Map();
var svgCache = /* @__PURE__ */ new Map();
var tokenCache = /* @__PURE__ */ new Map();
var OBJECTIVE_COMMANDER = "objective";
for (const definition of definitions) {
  for (const alias of definition.aliases) {
    aliases.set(getAliasKey(definition.commander, alias), definition);
  }
}
var unitIconCatalog = {
  resolve(commander, unitName) {
    return aliases.get(getAliasKey(commander, unitName)) ?? null;
  },
  render(ctx, definition, options) {
    renderIcon(ctx, definition, options);
  },
  toSvg(definition, options) {
    return toSvg(definition, options);
  },
  hydrateUnitIcons(root = document) {
    hydrateUnitIcons(root);
  }
};
function hydrateUnitIcons(root = document) {
  const hosts = root.querySelectorAll("[data-unit-icon]");
  for (const host of hosts) {
    const commander = host.dataset.unitCommander ?? "";
    const unitName = host.dataset.unitIcon ?? "";
    const size = normalizeSize(Number(host.dataset.unitSize ?? 20));
    const teamId = Number(host.dataset.teamId ?? 0);
    const teamColor = host.dataset.teamColor || colorForTeam(teamId);
    const unitColor = host.dataset.unitColor || void 0;
    const definition = unitIconCatalog.resolve(commander, unitName);
    const isObjective = isObjectiveIcon(commander);
    const renderKey = `${commander}|${unitName}|${size}|${teamColor ?? ""}|${unitColor ?? ""}|${definition?.id ?? ""}|${isObjective ? "objective" : ""}`;
    if (host.dataset.renderedIconKey === renderKey) {
      continue;
    }
    host.dataset.renderedIconKey = renderKey;
    if (definition) {
      host.innerHTML = toSvg(definition, { size, teamColor });
    } else if (isObjective && hydrateObjectiveIcon(host, unitName, size, teamColor ?? "#8a949e")) {
      continue;
    } else {
      host.innerHTML = fallbackSvg(size, unitColor ?? teamColor ?? "#8a949e");
    }
  }
}
function hydrateObjectiveIcon(host, unitName, size, teamColor) {
  const pixelSize = Math.ceil(size);
  const canvas = document.createElement("canvas");
  canvas.width = pixelSize;
  canvas.height = pixelSize;
  const ctx = getCanvasContext(canvas);
  if (!ctx) {
    return false;
  }
  const rendered = objectiveIconCatalog.render(ctx, {
    name: unitName,
    kind: unitName,
    teamColor,
    x: pixelSize / 2,
    y: pixelSize / 2,
    size: pixelSize * 0.72
  });
  if (!rendered) {
    return false;
  }
  canvas.style.width = `${pixelSize}px`;
  canvas.style.height = `${pixelSize}px`;
  host.replaceChildren(canvas);
  return true;
}
function renderIcon(ctx, definition, options) {
  const size = normalizeSize(options.size ?? 24);
  const x = options.x ?? size / 2;
  const y = options.y ?? size / 2;
  const scaleX = size / definition.viewBox.width;
  const scaleY = size / definition.viewBox.height;
  const tokens = resolveTokens(definition, options.teamColor);
  ctx.save();
  ctx.translate(x - size / 2, y - size / 2);
  ctx.scale(scaleX, scaleY);
  for (const layer of definition.layers) {
    drawLayer(ctx, layer, tokens, scaleX, scaleY);
  }
  ctx.restore();
}
function drawLayer(ctx, layer, tokens, scaleX, scaleY) {
  const inheritedAlpha = ctx.globalAlpha;
  ctx.save();
  ctx.globalAlpha = inheritedAlpha * (layer.opacity ?? 1);
  if (layer.type === "circle") {
    ctx.beginPath();
    ctx.arc(layer.cx, layer.cy, layer.r, 0, Math.PI * 2);
  } else {
    applyPath(ctx, layer.commands);
    ctx.lineCap = layer.lineCap ?? "butt";
    ctx.lineJoin = layer.lineJoin ?? "miter";
  }
  if (layer.fill) {
    ctx.fillStyle = resolvePaint(layer.fill, tokens);
    ctx.fill();
  }
  if (layer.stroke && layer.strokeWidth) {
    ctx.strokeStyle = resolvePaint(layer.stroke, tokens);
    const inverseScale = 1 / Math.max(1e-3, Math.min(scaleX, scaleY));
    ctx.lineWidth = Math.max(inverseScale, layer.strokeWidth);
    ctx.stroke();
  }
  ctx.restore();
}
function applyPath(ctx, commands) {
  ctx.beginPath();
  for (const command of commands) {
    switch (command[0]) {
      case "M":
        ctx.moveTo(command[1], command[2]);
        break;
      case "L":
        ctx.lineTo(command[1], command[2]);
        break;
      case "C":
        ctx.bezierCurveTo(command[1], command[2], command[3], command[4], command[5], command[6]);
        break;
      case "Z":
        ctx.closePath();
        break;
    }
  }
}
function toSvg(definition, options) {
  const size = normalizeSize(options.size ?? 20);
  const teamColor = options.teamColor ?? "";
  const cacheKey = `${definition.id}|${size}|${teamColor}`;
  const cached = svgCache.get(cacheKey);
  if (cached !== void 0) {
    return cached;
  }
  const tokens = resolveTokens(definition, options.teamColor);
  const layers = definition.layers.map((layer) => toSvgLayer(layer, tokens)).join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${definition.viewBox.width} ${definition.viewBox.height}" role="img" aria-hidden="true" focusable="false">${layers}</svg>`;
  svgCache.set(cacheKey, svg);
  return svg;
}
function toSvgLayer(layer, tokens) {
  const common = [
    attr("fill", layer.fill ? resolvePaint(layer.fill, tokens) : void 0),
    attr("stroke", layer.stroke ? resolvePaint(layer.stroke, tokens) : void 0),
    attr("stroke-width", layer.strokeWidth),
    attr("opacity", layer.opacity === void 0 || layer.opacity === 1 ? void 0 : layer.opacity),
    layer.type === "path" ? attr("stroke-linecap", layer.lineCap) : "",
    layer.type === "path" ? attr("stroke-linejoin", layer.lineJoin) : ""
  ].join("");
  if (layer.type === "circle") {
    return `<circle cx="${layer.cx}" cy="${layer.cy}" r="${layer.r}"${common}/>`;
  }
  return `<path d="${commandsToPath(layer.commands)}"${common}/>`;
}
function commandsToPath(commands) {
  return commands.map((command) => command.join(" ")).join(" ");
}
function fallbackSvg(size, color) {
  const safeColor = escapeAttribute(color);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 20 20" role="img" aria-hidden="true" focusable="false"><circle cx="10" cy="10" r="7" fill="${safeColor}99" stroke="${safeColor}" stroke-width="2"/></svg>`;
}
function resolveTokens(definition, teamColor) {
  if (!teamColor) {
    return definition.tokens;
  }
  const cacheKey = `${definition.id}|${teamColor}`;
  const cached = tokenCache.get(cacheKey);
  if (cached !== void 0) {
    return cached;
  }
  const palette = createTeamPalette(teamColor);
  const tokens = {
    ...definition.tokens,
    badgeFill: teamColor,
    badgeGlow: palette.light,
    badgeStroke: palette.light,
    armorFill: palette.light,
    armorMid: teamColor,
    armorShade: palette.dark,
    armorDark: palette.deeper,
    rifleFill: palette.deeper,
    skinFill: teamColor,
    skinMid: palette.dark,
    skinShade: palette.deeper,
    carapaceFill: palette.light,
    carapaceShade: palette.dark
  };
  tokenCache.set(cacheKey, tokens);
  return tokens;
}
function createTeamPalette(color) {
  return {
    light: mixHex(color, "#ffffff", 0.55),
    mid: mixHex(color, "#ffffff", 0.18),
    dark: mixHex(color, "#000000", 0.24),
    deeper: mixHex(color, "#000000", 0.48)
  };
}
function mixHex(left, right, weightRight) {
  const leftRgb = parseHexColor(left);
  const rightRgb = parseHexColor(right);
  if (!leftRgb || !rightRgb) {
    return left;
  }
  const weightLeft = 1 - weightRight;
  return toHex(
    Math.round(leftRgb.r * weightLeft + rightRgb.r * weightRight),
    Math.round(leftRgb.g * weightLeft + rightRgb.g * weightRight),
    Math.round(leftRgb.b * weightLeft + rightRgb.b * weightRight)
  );
}
function parseHexColor(color) {
  const match = /^#?([0-9a-f]{6})$/i.exec(color.trim());
  if (!match) {
    return null;
  }
  const value = Number.parseInt(match[1], 16);
  return {
    r: value >> 16 & 255,
    g: value >> 8 & 255,
    b: value & 255
  };
}
function toHex(r, g, b) {
  return `#${hexByte(r)}${hexByte(g)}${hexByte(b)}`;
}
function hexByte(value) {
  return Math.max(0, Math.min(255, value)).toString(16).padStart(2, "0").toUpperCase();
}
function resolvePaint(paint, tokens) {
  return tokens[paint] ?? paint;
}
function colorForTeam(teamId) {
  return TEAM_COLORS[teamId];
}
function attr(name, value) {
  return value === void 0 ? "" : ` ${name}="${escapeAttribute(String(value))}"`;
}
function escapeAttribute(value) {
  return value.replaceAll("&", "&amp;").replaceAll('"', "&quot;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
}
function normalizeSize(value) {
  return Number.isFinite(value) && value > 0 ? value : 20;
}
function getAliasKey(commander, unitName) {
  return `${normalize(commander)}|${normalize(unitName)}`;
}
function isObjectiveIcon(commander) {
  return normalize(commander) === OBJECTIVE_COMMANDER;
}
function normalize(value) {
  return String(value ?? "").trim().toLowerCase().replace(/ /g, "");
}

// TypeScript/rendering.ts
var MAP_BACKGROUND_BASE_COLOR = "#c1bda4";
var WATER_BACKGROUND_BASE_COLOR = "#56aeca";
var BACKGROUND_BASELINE_AREA = 960 * 560;
var OBJECTIVE_DEATH_ANNOUNCEMENT_SECONDS = 28;
var OBJECTIVE_DEATH_ANNOUNCEMENT_FADE_SECONDS = 7;
var OBJECTIVE_DEATH_ANNOUNCEMENT_HOLD_SECONDS = 14;
var OBJECTIVE_DEATH_LABELS = /* @__PURE__ */ new Set(["Bunker", "Cannon"]);
var ALIVE_UNIT_HIGHLIGHT_COLOR = "#F8D34A";
var FOREST_CLUSTERS = [
  { x: 0.21, y: 0.19, width: 0.18, height: 0.12, trees: 18 },
  { x: 0.27, y: 0.36, width: 0.15, height: 0.12, trees: 16 },
  { x: 0.48, y: 0.17, width: 0.13, height: 0.1, trees: 12 },
  { x: 0.13, y: 0.61, width: 0.14, height: 0.17, trees: 18 },
  { x: 0.48, y: 0.84, width: 0.24, height: 0.13, trees: 28 },
  { x: 0.72, y: 0.62, width: 0.13, height: 0.17, trees: 18 },
  { x: 0.87, y: 0.41, width: 0.09, height: 0.12, trees: 10 }
];
var DRAW_DIAGNOSTIC_FIRST_DRAWS = 5;
var DRAW_DIAGNOSTIC_SLOW_MS = 16;
var drawDiagnosticCounts = /* @__PURE__ */ new WeakMap();
function drawSpawnPlayback(canvas, currentGameloop, source = "unknown") {
  const drawStarted = performance.now();
  const stages = [];
  let stageStarted = drawStarted;
  let rebuiltStaticCache = false;
  const markStage = (name) => {
    const now = performance.now();
    stages.push(`${name}=${(now - stageStarted).toFixed(1)}ms`);
    stageStarted = now;
  };
  const state = getState(canvas);
  if (!state?.replay) {
    return;
  }
  const resized = resizeCanvas(canvas, source);
  markStage("resizeCanvas");
  const ctx = getCanvasContext(canvas);
  markStage("getContext");
  const replay = state.replay;
  const bounds = replay.bounds;
  if (!ctx || !bounds) {
    logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, 0, "missing context/bounds");
    return;
  }
  state.currentGameloop = clampGameloop(state, currentGameloop);
  markStage("clampGameloop");
  if (resized || !state.staticGeometry || !state.renderCache || state.staticCanvasWidth !== canvas.width || state.staticCanvasHeight !== canvas.height) {
    rebuiltStaticCache = true;
    state.renderCache = createRenderCache(bounds, canvas);
    markStage("createRenderCache");
    state.staticGeometry = createStaticGeometry(replay, bounds, canvas);
    markStage("createStaticGeometry");
    state.staticBackgroundCanvas = createStaticBackgroundCanvas(canvas, state.staticGeometry);
    markStage("createStaticBackgroundCanvas");
    state.objectiveDeathAnnouncements = createObjectiveDeathAnnouncements(
      state.staticGeometry.landmarks,
      replay.stepGameloops,
      state.gameloopsPerSecond
    );
    markStage("createObjectiveAnnouncements");
    state.staticCanvasWidth = canvas.width;
    state.staticCanvasHeight = canvas.height;
    state.activeUnits.length = 0;
    state.nextUnitIndex = 0;
    state.lastActiveGameloop = Number.NEGATIVE_INFINITY;
    prepareUnitSprites(state, canvas);
    markStage("prepareUnitSprites");
  }
  if (!state.staticBackgroundCanvas || !state.staticGeometry || !state.renderCache) {
    logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, 0, "missing cache");
    return;
  }
  ctx.drawImage(state.staticBackgroundCanvas, 0, 0);
  markStage("drawStaticBackground");
  drawDynamicMapLayer(ctx, canvas, state.staticGeometry, state.currentGameloop);
  markStage("drawDynamicMapLayer");
  const activeUnits = getActiveUnits(state, state.currentGameloop);
  markStage("getActiveUnits");
  const drawnUnits = drawUnitLayer(ctx, state.renderCache.projection, activeUnits, state.currentGameloop);
  markStage("drawUnitLayer");
  if (state.highlightedAliveUnitKey !== null) {
    drawAliveUnitHighlightLayer(
      ctx,
      state.renderCache.projection,
      activeUnits,
      state.currentGameloop,
      state.highlightedAliveUnitKey
    );
    markStage("drawAliveUnitHighlightLayer");
  }
  if (drawnUnits === 0) {
    drawEmptyState(ctx, canvas);
    markStage("drawEmptyState");
  }
  drawObjectiveDeathAnnouncements(ctx, canvas, state.objectiveDeathAnnouncements, state.currentGameloop);
  markStage("drawObjectiveDeathAnnouncements");
  drawEndOfReplaySummary(ctx, canvas, replay.summary, state.currentGameloop, replay.durationGameloop);
  markStage("drawEndOfReplaySummary");
  logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, activeUnits.length);
}
function clampGameloop(state, gameloop) {
  const duration = state.replay.durationGameloop ?? 0;
  return clamp(Number.isFinite(gameloop) ? gameloop : 0, 0, duration);
}
function prepareUnitSprites(state, canvas) {
  const scale = deviceScale(canvas);
  state.unitSpriteCache.clear();
  for (const unit of state.replay.units) {
    const radius = Math.max(3, unit.radius * scale * 0.55);
    if (!unit.iconResolved) {
      unit.iconDefinition = unitIconCatalog.resolve(unit.commander, unit.name);
      unit.iconResolved = true;
    }
    unit.render = {
      radius,
      sprite: getUnitSprite(state, unit, radius, scale)
    };
  }
}
function logDrawDiagnostic(canvas, source, startedAt, stages, resized, rebuiltStaticCache, activeUnitCount, note = "") {
  const elapsed = performance.now() - startedAt;
  const drawCount = (drawDiagnosticCounts.get(canvas) ?? 0) + 1;
  drawDiagnosticCounts.set(canvas, drawCount);
  if (!rebuiltStaticCache && drawCount > DRAW_DIAGNOSTIC_FIRST_DRAWS && elapsed < DRAW_DIAGNOSTIC_SLOW_MS) {
    return;
  }
  const suffix = note ? ` ${note}` : "";
  console.log(
    `spawnPlayback draw #${drawCount} source=${source} elapsed=${elapsed.toFixed(1)}ms resized=${resized} rebuilt=${rebuiltStaticCache} active=${activeUnitCount}${suffix} stages=[${stages.join(", ")}] - ${Date.now()}`
  );
}
function createStaticBackgroundCanvas(canvas, geometry) {
  const backgroundCanvas = createLayerCanvas(canvas.width, canvas.height);
  const ctx = getCanvasContext(backgroundCanvas);
  if (!ctx) {
    return null;
  }
  drawStaticBackgroundLayer(ctx, canvas, geometry);
  return backgroundCanvas;
}
function getUnitSprite(state, unit, radius, canvasScale) {
  const color = unit.color;
  const teamId = unit.teamId;
  const iconDefinition = unit.iconDefinition;
  const iconColor = iconDefinition ? TEAM_COLORS[teamId] ?? color : color;
  const key = iconDefinition ? `${iconDefinition.id}|${unit.commander}|${unit.name}|${teamId}|${iconColor}|${Math.round(radius * 10)}` : `${teamId}|${color}|${Math.round(radius * 10)}`;
  const cached = state.unitSpriteCache.get(key);
  if (cached) {
    return cached;
  }
  const scale = Math.max(1, radius / 3);
  const padding = Math.ceil(3 * scale);
  const iconSize = iconDefinition ? Math.max(radius * 2.6, MIN_CATALOG_ICON_CSS_SIZE * canvasScale) : radius * 2;
  const size = Math.ceil(iconSize + padding * 2);
  const sprite = createLayerCanvas(size, size);
  const ctx = getCanvasContext(sprite);
  if (!ctx) {
    return sprite;
  }
  const center = size / 2;
  ctx.save();
  ctx.globalAlpha = teamId === 1 ? 0.92 : 0.78;
  if (iconDefinition) {
    unitIconCatalog.render(ctx, iconDefinition, {
      x: center,
      y: center,
      size: iconSize,
      teamColor: iconColor
    });
  } else {
    ctx.fillStyle = withAlpha(color, "99");
    ctx.strokeStyle = withAlpha(color, "EE");
    ctx.lineWidth = Math.max(1.5, 1.5 * scale);
    ctx.beginPath();
    ctx.arc(center, center, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
  }
  ctx.restore();
  state.unitSpriteCache.set(key, sprite);
  return sprite;
}
function getActiveUnits(state, currentGameloop) {
  if (currentGameloop < state.lastActiveGameloop) {
    rebuildActiveUnits(state, currentGameloop);
    return state.activeUnits;
  }
  const units = state.replay.units;
  while (state.nextUnitIndex < units.length && units[state.nextUnitIndex].spawnGameloop <= currentGameloop) {
    state.activeUnits.push(units[state.nextUnitIndex]);
    state.nextUnitIndex++;
  }
  compactActiveUnits(state, currentGameloop);
  state.lastActiveGameloop = currentGameloop;
  return state.activeUnits;
}
function rebuildActiveUnits(state, currentGameloop) {
  state.activeUnits.length = 0;
  const units = state.replay.units;
  let index = 0;
  while (index < units.length && units[index].spawnGameloop <= currentGameloop) {
    const unit = units[index];
    if (unit.expiresGameloop > currentGameloop) {
      state.activeUnits.push(unit);
    }
    index++;
  }
  state.nextUnitIndex = index;
  state.lastActiveGameloop = currentGameloop;
}
function compactActiveUnits(state, currentGameloop) {
  const activeUnits = state.activeUnits;
  let writeIndex = 0;
  for (let readIndex = 0; readIndex < activeUnits.length; readIndex++) {
    const unit = activeUnits[readIndex];
    if (unit.expiresGameloop > currentGameloop) {
      activeUnits[writeIndex] = unit;
      writeIndex++;
    }
  }
  activeUnits.length = writeIndex;
}
function drawStaticBackgroundLayer(ctx, canvas, geometry) {
  ctx.save();
  drawCloudyBackground(ctx, canvas);
  drawForestAssets(ctx, canvas);
  ctx.restore();
  drawGrid(ctx, canvas, geometry.gridLines);
  drawSpawnAreas(ctx, canvas, geometry.spawnAreas);
}
function drawCloudyBackground(ctx, canvas) {
  const scale = deviceScale(canvas);
  const random = createBackgroundRandom(canvas.width, canvas.height);
  const patchScale = getBackgroundAreaScale(canvas, scale);
  drawWaterBackground(ctx, canvas, random, scale, patchScale);
  drawShoreBands(ctx, canvas, scale);
  ctx.save();
  traceIslandPath(ctx, canvas, 0);
  ctx.clip();
  ctx.fillStyle = MAP_BACKGROUND_BASE_COLOR;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  drawCloudPatches(ctx, canvas, random, Math.round(90 * patchScale), scale, {
    offset: 100,
    minRadius: 80,
    maxRadius: 260,
    centerColor: "255,255,245",
    minAlpha: 0.025,
    maxAlpha: 0.08
  });
  drawCloudPatches(ctx, canvas, random, Math.round(70 * patchScale), scale, {
    offset: 100,
    minRadius: 60,
    maxRadius: 220,
    centerColor: "80,90,80",
    minAlpha: 0.018,
    maxAlpha: 0.055
  });
  drawWispyBackgroundStrokes(ctx, canvas, random, Math.round(140 * patchScale), scale);
  drawFineGrain(ctx, canvas, random, scale);
  ctx.restore();
}
function drawWaterBackground(ctx, canvas, random, scale, patchScale) {
  const gradient = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
  gradient.addColorStop(0, "#58b9d8");
  gradient.addColorStop(0.55, WATER_BACKGROUND_BASE_COLOR);
  gradient.addColorStop(1, "#3287b8");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.save();
  ctx.globalAlpha = 0.25;
  ctx.lineCap = "round";
  for (let i = 0; i < Math.round(90 * patchScale); i++) {
    const x = rand(random, -40 * scale, canvas.width);
    const y = rand(random, 0, canvas.height);
    const length = rand(random, 36 * scale, 130 * scale);
    ctx.strokeStyle = random() > 0.45 ? "rgba(210, 244, 240, 0.30)" : "rgba(24, 111, 158, 0.26)";
    ctx.lineWidth = rand(random, 1 * scale, 2.25 * scale);
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.bezierCurveTo(
      x + length * 0.35,
      y + rand(random, -10 * scale, 10 * scale),
      x + length * 0.65,
      y + rand(random, -10 * scale, 10 * scale),
      x + length,
      y + rand(random, -8 * scale, 8 * scale)
    );
    ctx.stroke();
  }
  ctx.restore();
}
function drawShoreBands(ctx, canvas, scale) {
  ctx.save();
  traceIslandPath(ctx, canvas, 24 * scale);
  ctx.fillStyle = "rgba(218, 213, 169, 0.74)";
  ctx.fill();
  traceIslandPath(ctx, canvas, 12 * scale);
  ctx.fillStyle = "rgba(196, 190, 143, 0.70)";
  ctx.fill();
  traceIslandPath(ctx, canvas, 0);
  ctx.fillStyle = MAP_BACKGROUND_BASE_COLOR;
  ctx.fill();
  ctx.lineWidth = Math.max(2, 2 * scale);
  ctx.strokeStyle = "rgba(245, 238, 190, 0.42)";
  traceIslandPath(ctx, canvas, 24 * scale);
  ctx.stroke();
  ctx.lineWidth = Math.max(1, 1.25 * scale);
  ctx.strokeStyle = "rgba(65, 125, 136, 0.30)";
  traceIslandPath(ctx, canvas, 36 * scale);
  ctx.stroke();
  ctx.restore();
}
function traceIslandPath(ctx, canvas, expand) {
  const width = canvas.width;
  const height = canvas.height;
  const left = 0.075 * width - expand;
  const right = 0.925 * width + expand;
  const top = 0.105 * height - expand;
  const bottom = 0.895 * height + expand;
  const upperRight = 0.985 * width + expand;
  const upperTop = 0.055 * height - expand;
  const lowerLeft = 0.02 * width - expand;
  const lowerBottom = 0.95 * height + expand;
  ctx.beginPath();
  ctx.moveTo(0.19 * width, top + 0.03 * height);
  ctx.bezierCurveTo(0.32 * width, top - 0.045 * height, 0.47 * width, top - 0.01 * height, 0.58 * width, top + 0.02 * height);
  ctx.bezierCurveTo(0.72 * width, upperTop - 0.035 * height, upperRight - 0.015 * width, upperTop + 0.02 * height, upperRight, 0.21 * height);
  ctx.bezierCurveTo(upperRight + 0.01 * width, 0.35 * height, 0.84 * width, 0.45 * height, 0.9 * width, 0.58 * height);
  ctx.bezierCurveTo(0.95 * width, 0.73 * height, 0.79 * width, bottom + 0.03 * height, 0.63 * width, bottom);
  ctx.bezierCurveTo(0.51 * width, lowerBottom + 0.035 * height, 0.39 * width, 0.93 * height, 0.28 * width, lowerBottom - 0.01 * height);
  ctx.bezierCurveTo(0.12 * width, lowerBottom + 0.03 * height, lowerLeft - 0.015 * width, 0.77 * height, lowerLeft, 0.61 * height);
  ctx.bezierCurveTo(left - 0.035 * width, 0.48 * height, 0.15 * width, 0.39 * height, left + 0.01 * width, 0.28 * height);
  ctx.bezierCurveTo(left + 0.035 * width, 0.18 * height, 0.1 * width, 0.15 * height, 0.19 * width, top + 0.03 * height);
  ctx.closePath();
}
function getBackgroundAreaScale(canvas, scale) {
  const cssWidth = canvas.width / Math.max(1, scale);
  const cssHeight = canvas.height / Math.max(1, scale);
  return clamp(cssWidth * cssHeight / BACKGROUND_BASELINE_AREA, 0.65, 1.8);
}
function drawCloudPatches(ctx, canvas, random, count, scale, options) {
  const offset = options.offset * scale;
  for (let i = 0; i < count; i++) {
    const x = rand(random, -offset, canvas.width + offset);
    const y = rand(random, -offset, canvas.height + offset);
    const radius = rand(random, options.minRadius * scale, options.maxRadius * scale);
    const alpha = rand(random, options.minAlpha, options.maxAlpha);
    const gradient = ctx.createRadialGradient(x, y, 0, x, y, radius);
    gradient.addColorStop(0, `rgba(${options.centerColor},${alpha})`);
    gradient.addColorStop(1, `rgba(${options.centerColor},0)`);
    ctx.fillStyle = gradient;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fill();
  }
}
function drawWispyBackgroundStrokes(ctx, canvas, random, count, scale) {
  ctx.save();
  ctx.globalAlpha = 0.22;
  ctx.lineCap = "round";
  for (let i = 0; i < count; i++) {
    const x = rand(random, -100 * scale, canvas.width);
    const y = rand(random, 0, canvas.height);
    const length = rand(random, 80 * scale, 260 * scale);
    ctx.strokeStyle = random() > 0.5 ? "rgba(245,245,230,0.18)" : "rgba(85,95,85,0.12)";
    ctx.lineWidth = rand(random, 1 * scale, 4 * scale);
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.bezierCurveTo(
      x + length * 0.25,
      y + rand(random, -20 * scale, 20 * scale),
      x + length * 0.7,
      y + rand(random, -30 * scale, 30 * scale),
      x + length,
      y + rand(random, -18 * scale, 18 * scale)
    );
    ctx.stroke();
  }
  ctx.restore();
}
function drawFineGrain(ctx, canvas, random, scale) {
  const tileSize = Math.max(96, Math.round(128 * Math.min(scale, 2)));
  const grainCanvas = createLayerCanvas(tileSize, tileSize);
  const grainCtx = getCanvasContext(grainCanvas);
  if (!grainCtx) {
    return;
  }
  const imageData = grainCtx.createImageData(tileSize, tileSize);
  const data = imageData.data;
  for (let i = 0; i < data.length; i += 4) {
    const noise = Math.round(rand(random, 0, 32));
    data[i] = noise;
    data[i + 1] = noise;
    data[i + 2] = noise;
    data[i + 3] = 34;
  }
  grainCtx.putImageData(imageData, 0, 0);
  const pattern = ctx.createPattern(grainCanvas, "repeat");
  if (!pattern) {
    return;
  }
  ctx.save();
  ctx.globalCompositeOperation = "overlay";
  ctx.fillStyle = pattern;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.restore();
}
function drawForestAssets(ctx, canvas) {
  const scale = deviceScale(canvas);
  const random = createBackgroundRandom(canvas.width + 17, canvas.height + 31);
  const areaScale = getBackgroundAreaScale(canvas, scale);
  ctx.save();
  traceIslandPath(ctx, canvas, 0);
  ctx.clip();
  for (const cluster of FOREST_CLUSTERS) {
    const centerX = cluster.x * canvas.width;
    const centerY = cluster.y * canvas.height;
    const width = cluster.width * canvas.width;
    const height = cluster.height * canvas.height;
    const treeCount = Math.round(cluster.trees * clamp(areaScale, 0.8, 1.35));
    drawForestGroundPatch(ctx, centerX, centerY, width, height, random);
    for (let i = 0; i < treeCount; i++) {
      const point = randomPointInEllipse(centerX, centerY, width * 0.45, height * 0.42, random);
      const size = rand(random, 5.5 * scale, 11 * scale);
      drawMinimalTree(ctx, point.x, point.y, size, random);
    }
  }
  ctx.restore();
}
function drawForestGroundPatch(ctx, centerX, centerY, width, height, random) {
  ctx.save();
  ctx.globalAlpha = 0.44;
  ctx.fillStyle = "rgba(28, 92, 42, 0.34)";
  ctx.beginPath();
  ctx.ellipse(centerX, centerY, width * rand(random, 0.42, 0.5), height * rand(random, 0.36, 0.48), rand(random, -0.45, 0.45), 0, Math.PI * 2);
  ctx.fill();
  ctx.globalAlpha = 0.28;
  ctx.fillStyle = "rgba(18, 73, 36, 0.30)";
  ctx.beginPath();
  ctx.ellipse(centerX + width * rand(random, -0.12, 0.12), centerY + height * rand(random, -0.14, 0.14), width * 0.36, height * 0.3, rand(random, -0.7, 0.7), 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}
function randomPointInEllipse(centerX, centerY, radiusX, radiusY, random) {
  const angle = rand(random, 0, Math.PI * 2);
  const distance = Math.sqrt(random());
  return {
    x: centerX + Math.cos(angle) * radiusX * distance,
    y: centerY + Math.sin(angle) * radiusY * distance
  };
}
function drawMinimalTree(ctx, x, y, size, random) {
  const height = size * rand(random, 1.2, 1.8);
  const width = size * rand(random, 0.8, 1.15);
  const sway = rand(random, -0.12, 0.12);
  ctx.save();
  ctx.translate(x, y);
  ctx.rotate(sway);
  ctx.fillStyle = "rgba(42, 106, 49, 0.72)";
  drawTreeTriangle(ctx, 0, -height * 0.55, width * 0.58, height * 0.48);
  ctx.fillStyle = "rgba(23, 82, 38, 0.78)";
  drawTreeTriangle(ctx, 0, -height * 0.24, width * 0.72, height * 0.54);
  ctx.fillStyle = "rgba(17, 63, 32, 0.82)";
  drawTreeTriangle(ctx, 0, height * 0.08, width * 0.86, height * 0.58);
  ctx.strokeStyle = "rgba(77, 61, 35, 0.52)";
  ctx.lineWidth = Math.max(1, size * 0.13);
  ctx.beginPath();
  ctx.moveTo(0, height * 0.06);
  ctx.lineTo(0, height * 0.42);
  ctx.stroke();
  ctx.restore();
}
function drawTreeTriangle(ctx, centerX, topY, width, height) {
  ctx.beginPath();
  ctx.moveTo(centerX, topY);
  ctx.lineTo(centerX - width * 0.5, topY + height);
  ctx.lineTo(centerX + width * 0.5, topY + height);
  ctx.closePath();
  ctx.fill();
}
function createBackgroundRandom(width, height) {
  let seed = (width * 73856093 ^ height * 19349663 ^ 1831565813) >>> 0;
  return () => {
    seed = seed + 1831565813 | 0;
    let value = Math.imul(seed ^ seed >>> 15, 1 | seed);
    value ^= value + Math.imul(value ^ value >>> 7, 61 | value);
    return ((value ^ value >>> 14) >>> 0) / 4294967296;
  };
}
function rand(random, min, max) {
  return random() * (max - min) + min;
}
function drawDynamicMapLayer(ctx, canvas, geometry, currentGameloop) {
  drawPlayerGasBadges(ctx, canvas, geometry.playerGasBadges, currentGameloop);
  drawMiddleLine(ctx, canvas, geometry.middleLine, geometry.middleControl, currentGameloop);
  for (const landmark of geometry.landmarks) {
    if (landmark.diedGameloop != null && landmark.diedGameloop <= currentGameloop) {
      continue;
    }
    drawLandmark(ctx, canvas, landmark.projected, landmark.radius, landmark.color, landmark.kind, landmark.label);
  }
}
function createObjectiveDeathAnnouncements(landmarks, stepGameloops, gameloopsPerSecond) {
  if (landmarks.length === 0) {
    return [];
  }
  const step = Math.max(1, Math.round(Number.isFinite(stepGameloops) ? stepGameloops : 1));
  const loopsPerSecond = Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0 ? gameloopsPerSecond : 22.4;
  const fadeGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_FADE_SECONDS * loopsPerSecond);
  const holdGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_HOLD_SECONDS * loopsPerSecond);
  const durationGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_SECONDS * loopsPerSecond);
  const announcements = [];
  for (const landmark of landmarks) {
    if (!OBJECTIVE_DEATH_LABELS.has(landmark.label) || landmark.diedGameloop == null) {
      continue;
    }
    const diedGameloop = landmark.diedGameloop;
    if (!Number.isFinite(diedGameloop)) {
      continue;
    }
    const anchorGameloop = Math.max(0, Math.round(diedGameloop / step) * step);
    const message = landmark.kills > 0 ? `${landmark.label} down at ${formatGameloopClock(diedGameloop, loopsPerSecond)} with ${landmark.kills} kills` : `${landmark.label} down at ${formatGameloopClock(diedGameloop, loopsPerSecond)}`;
    announcements.push({
      message,
      accentColor: landmark.color,
      anchorGameloop,
      startGameloop: Math.max(0, anchorGameloop - fadeGameloops),
      holdEndGameloop: anchorGameloop + holdGameloops,
      endGameloop: anchorGameloop + durationGameloops - fadeGameloops
    });
  }
  return announcements;
}
function formatGameloopClock(gameloop, gameloopsPerSecond) {
  const totalSeconds = Math.max(0, Math.round(gameloop / gameloopsPerSecond));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}
function drawObjectiveDeathAnnouncements(ctx, canvas, announcements, currentGameloop) {
  if (announcements.length === 0) {
    return;
  }
  for (const announcement of announcements) {
    if (currentGameloop < announcement.startGameloop || currentGameloop > announcement.endGameloop) {
      continue;
    }
    const alpha = getObjectiveDeathAnnouncementAlpha(announcement, currentGameloop);
    if (alpha <= 0) {
      continue;
    }
    drawObjectiveDeathAnnouncement(ctx, canvas, announcement, alpha);
  }
}
function getObjectiveDeathAnnouncementAlpha(announcement, currentGameloop) {
  if (currentGameloop < announcement.anchorGameloop) {
    const fadeDuration2 = Math.max(1, announcement.anchorGameloop - announcement.startGameloop);
    return clamp((currentGameloop - announcement.startGameloop) / fadeDuration2, 0, 1);
  }
  if (currentGameloop <= announcement.holdEndGameloop) {
    return 1;
  }
  const fadeDuration = Math.max(1, announcement.endGameloop - announcement.holdEndGameloop);
  return clamp(1 - (currentGameloop - announcement.holdEndGameloop) / fadeDuration, 0, 1);
}
function drawObjectiveDeathAnnouncement(ctx, canvas, announcement, alpha) {
  const scale = deviceScale(canvas);
  const fontSize = Math.max(15, 16 * scale);
  const horizontalPadding = 18 * scale;
  const verticalPadding = 10 * scale;
  const accentWidth = 4 * scale;
  const y = Math.max(48 * scale, canvas.height * 0.18);
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.font = `700 ${fontSize}px sans-serif`;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  const textWidth = ctx.measureText(announcement.message).width;
  const panelWidth = Math.min(canvas.width - 24 * scale, textWidth + horizontalPadding * 2 + accentWidth);
  const panelHeight = fontSize + verticalPadding * 2;
  const x = (canvas.width - panelWidth) / 2;
  const panelY = y - panelHeight / 2;
  const radius = 8 * scale;
  ctx.fillStyle = "rgba(7, 16, 21, 0.86)";
  drawRoundedRect(ctx, x, panelY, panelWidth, panelHeight, radius);
  ctx.fill();
  ctx.fillStyle = withAlpha(announcement.accentColor, "F2");
  drawRoundedRect(ctx, x, panelY, accentWidth, panelHeight, radius);
  ctx.fill();
  ctx.strokeStyle = "rgba(255, 255, 255, 0.18)";
  ctx.lineWidth = Math.max(1, scale);
  drawRoundedRect(ctx, x, panelY, panelWidth, panelHeight, radius);
  ctx.stroke();
  ctx.fillStyle = "rgba(255, 255, 255, 0.94)";
  ctx.fillText(announcement.message, canvas.width / 2 + accentWidth / 2, y, panelWidth - horizontalPadding * 2);
  ctx.restore();
}
function isEndSummaryVisible(currentGameloop, durationGameloop) {
  return Number.isFinite(currentGameloop) && Number.isFinite(durationGameloop) && durationGameloop > 0 && currentGameloop >= durationGameloop;
}
function drawEndOfReplaySummary(ctx, canvas, summary, currentGameloop, durationGameloop) {
  if (!isEndSummaryVisible(currentGameloop, durationGameloop) || summary.players.length === 0 && summary.topUnits.length === 0) {
    return;
  }
  const scale = deviceScale(canvas);
  const panelWidth = Math.min(canvas.width - 28 * scale, 820 * scale);
  const stacked = panelWidth < 620 * scale;
  const padding = 16 * scale;
  const headerHeight = 34 * scale;
  const sectionTitleHeight = 24 * scale;
  const rowHeight = 18 * scale;
  const gap = 16 * scale;
  const playerRowsHeight = summary.players.length * rowHeight;
  const topRowsHeight = Math.max(1, summary.topUnits.length) * rowHeight;
  const sectionHeight = sectionTitleHeight + (stacked ? playerRowsHeight + topRowsHeight + sectionTitleHeight + gap : Math.max(playerRowsHeight, topRowsHeight));
  const panelHeight = Math.min(canvas.height - 28 * scale, padding * 2 + headerHeight + sectionHeight);
  const x = (canvas.width - panelWidth) / 2;
  const y = (canvas.height - panelHeight) / 2;
  const radius = 8 * scale;
  ctx.save();
  ctx.fillStyle = "rgba(7, 16, 21, 0.90)";
  drawRoundedRect(ctx, x, y, panelWidth, panelHeight, radius);
  ctx.fill();
  ctx.strokeStyle = "rgba(255, 255, 255, 0.18)";
  ctx.lineWidth = Math.max(1, scale);
  drawRoundedRect(ctx, x, y, panelWidth, panelHeight, radius);
  ctx.stroke();
  ctx.textAlign = "left";
  ctx.textBaseline = "middle";
  ctx.font = `700 ${Math.max(15, 16 * scale)}px sans-serif`;
  ctx.fillStyle = "rgba(255, 255, 255, 0.96)";
  ctx.fillText("Replay summary", x + padding, y + padding + 10 * scale);
  ctx.textAlign = "right";
  ctx.font = `700 ${Math.max(13, 14 * scale)}px sans-serif`;
  ctx.fillStyle = "rgba(255, 193, 7, 0.94)";
  ctx.fillText(`${formatCount(summary.totalKills)} total kills`, x + panelWidth - padding, y + padding + 10 * scale);
  const contentY = y + padding + headerHeight;
  if (stacked) {
    drawPlayerSummaryRows(ctx, summary, x + padding, contentY, panelWidth - padding * 2, rowHeight, scale);
    drawTopUnitSummaryRows(
      ctx,
      summary,
      x + padding,
      contentY + sectionTitleHeight + playerRowsHeight + gap,
      panelWidth - padding * 2,
      rowHeight,
      scale
    );
  } else {
    const columnWidth = (panelWidth - padding * 2 - gap) / 2;
    drawPlayerSummaryRows(ctx, summary, x + padding, contentY, columnWidth, rowHeight, scale);
    drawTopUnitSummaryRows(ctx, summary, x + padding + columnWidth + gap, contentY, columnWidth, rowHeight, scale);
  }
  ctx.restore();
}
function drawPlayerSummaryRows(ctx, summary, x, y, width, rowHeight, scale) {
  drawSummarySectionTitle(ctx, "Player kills", x, y, width, scale);
  let rowY = y + 24 * scale;
  const killsWidth = 72 * scale;
  const labelWidth = Math.min(width - killsWidth - 16 * scale, 180 * scale);
  const killsX = x + 10 * scale + labelWidth + 14 * scale + killsWidth;
  for (const row of summary.players) {
    drawSummaryRowAccent(ctx, row.teamId, x, rowY, rowHeight, scale);
    ctx.textAlign = "left";
    ctx.font = `600 ${Math.max(10, 11 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
    ctx.fillText(fitText(ctx, `P${row.gamePos} ${row.playerName}`, labelWidth), x + 10 * scale, rowY + rowHeight / 2);
    ctx.textAlign = "right";
    ctx.fillStyle = "rgba(255, 255, 255, 0.78)";
    ctx.fillText(formatCount(row.kills), killsX, rowY + rowHeight / 2);
    rowY += rowHeight;
  }
}
function drawTopUnitSummaryRows(ctx, summary, x, y, width, rowHeight, scale) {
  drawSummarySectionTitle(ctx, "Top units", x, y, width, scale);
  let rowY = y + 24 * scale;
  if (summary.topUnits.length === 0) {
    ctx.textAlign = "left";
    ctx.font = `${Math.max(10, 11 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 255, 255, 0.58)";
    ctx.fillText("No unit kill events", x, rowY + rowHeight / 2, width);
    return;
  }
  for (const row of summary.topUnits) {
    drawSummaryRowAccent(ctx, row.teamId, x, rowY, rowHeight, scale);
    ctx.textAlign = "left";
    ctx.font = `600 ${Math.max(10, 11 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
    const playerWidth = width * 0.42;
    const unitWidth = width * 0.28;
    ctx.fillText(fitText(ctx, `P${row.gamePos} ${row.playerName}`, playerWidth), x + 10 * scale, rowY + rowHeight / 2);
    ctx.fillStyle = "rgba(255, 255, 255, 0.72)";
    ctx.fillText(fitText(ctx, row.unitName, unitWidth), x + width * 0.48, rowY + rowHeight / 2);
    ctx.textAlign = "right";
    ctx.fillStyle = "rgba(255, 255, 255, 0.78)";
    ctx.fillText(formatCount(row.kills), x + width, rowY + rowHeight / 2);
    rowY += rowHeight;
  }
}
function fitText(ctx, text, maxWidth) {
  if (maxWidth <= 0 || text.length === 0) {
    return "";
  }
  if (ctx.measureText(text).width <= maxWidth) {
    return text;
  }
  const ellipsis = "...";
  if (ctx.measureText(ellipsis).width > maxWidth) {
    return "";
  }
  let left = 0;
  let right = text.length;
  while (left < right) {
    const middle = Math.ceil((left + right) / 2);
    const candidate = text.slice(0, middle) + ellipsis;
    if (ctx.measureText(candidate).width <= maxWidth) {
      left = middle;
    } else {
      right = middle - 1;
    }
  }
  return text.slice(0, left) + ellipsis;
}
function drawSummarySectionTitle(ctx, title, x, y, width, scale) {
  ctx.textAlign = "left";
  ctx.font = `700 ${Math.max(11, 12 * scale)}px sans-serif`;
  ctx.fillStyle = "rgba(255, 193, 7, 0.88)";
  ctx.fillText(title, x, y + 9 * scale, width);
}
function drawSummaryRowAccent(ctx, teamId, x, y, rowHeight, scale) {
  ctx.fillStyle = withAlpha(TEAM_COLORS[teamId] ?? "#FFFFFF", "CC");
  ctx.fillRect(x, y + 4 * scale, 3 * scale, rowHeight - 8 * scale);
}
function formatCount(value) {
  return Math.max(0, Math.round(value)).toLocaleString("en-US");
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
  ctx.lineWidth = Math.max(2.25, deviceScale(canvas) * 2.25);
  ctx.setLineDash([7 * deviceScale(canvas), 4 * deviceScale(canvas)]);
  for (const area of spawnAreas) {
    if (area.points.length === 0) {
      continue;
    }
    ctx.fillStyle = withAlpha(area.color, "34");
    ctx.strokeStyle = withAlpha(area.color, "F2");
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
    const tierLevel = getTierLevelAtGameloop(badge.tierUpgradeGameloops, currentGameloop);
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
    ctx.fillStyle = "rgba(255, 193, 7, 0.92)";
    ctx.fillText(`T${tierLevel}`, x + 31 * scale, badge.y);
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
function getTierLevelAtGameloop(tierUpgradeGameloops, currentGameloop) {
  let upgradeCount = 0;
  while (upgradeCount < tierUpgradeGameloops.length && upgradeCount < 2 && tierUpgradeGameloops[upgradeCount] <= currentGameloop) {
    upgradeCount++;
  }
  return 1 + upgradeCount;
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
  const teamId = reachedChanges % 2 === 1 ? middleControl.firstTeamId : getOtherTeamId(middleControl.firstTeamId);
  return withAlpha(TEAM_COLORS[teamId], "CC");
}
function getOtherTeamId(teamId) {
  return teamId === 1 ? 2 : 1;
}
function drawLandmark(ctx, canvas, projected, radius, color, kind, label) {
  if (!projected) {
    return;
  }
  ctx.save();
  const scale = deviceScale(canvas);
  const objectiveSize = objectiveIconCatalog.getSize(label, kind, radius, scale);
  const renderedIcon = objectiveIconCatalog.render(ctx, {
    name: label,
    kind,
    teamColor: color,
    x: projected.x,
    y: projected.y,
    size: objectiveSize
  });
  if (!renderedIcon) {
    drawFallbackLandmark(ctx, projected.x, projected.y, radius, color, kind, canvas);
  }
  ctx.fillStyle = "rgba(255, 255, 255, 0.82)";
  ctx.font = `${Math.max(10, 10 * scale)}px sans-serif`;
  ctx.textAlign = "center";
  ctx.textBaseline = "top";
  const labelOffset = renderedIcon ? objectiveSize / 2 : radius;
  ctx.fillText(label, projected.x, projected.y + labelOffset + 3 * scale);
  ctx.restore();
}
function drawFallbackLandmark(ctx, x, y, radius, color, kind, canvas) {
  ctx.lineWidth = Math.max(2.25, deviceScale(canvas) * 2.25);
  ctx.strokeStyle = withAlpha(color, "FF");
  ctx.fillStyle = withAlpha(color, kind === "Base" ? "64" : "72");
  if (kind === "Base") {
    ctx.beginPath();
    ctx.moveTo(x, y - radius);
    ctx.lineTo(x + radius, y);
    ctx.lineTo(x, y + radius);
    ctx.lineTo(x - radius, y);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    return;
  }
  ctx.beginPath();
  ctx.arc(x, y, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(x - radius * 0.75, y);
  ctx.lineTo(x + radius * 0.75, y);
  ctx.moveTo(x, y - radius * 0.75);
  ctx.lineTo(x, y + radius * 0.75);
  ctx.stroke();
}
function drawUnitLayer(ctx, projection, activeUnits, currentGameloop) {
  let drawnUnits = 0;
  for (const unit of activeUnits) {
    if (drawUnit(ctx, projection, unit, currentGameloop)) {
      drawnUnits++;
    }
  }
  return drawnUnits;
}
function drawUnit(ctx, projection, unit, currentGameloop) {
  if (currentGameloop < unit.spawnGameloop || unit.expiresGameloop <= currentGameloop) {
    return false;
  }
  const progress = clamp((currentGameloop - unit.spawnGameloop) * unit.inverseLifetime, 0, 1);
  const x = projectX(projection, unit.spawnX + unit.deltaX * progress);
  const y = projectY(projection, unit.spawnY + unit.deltaY * progress);
  const sprite = unit.render?.sprite;
  const radius = unit.render?.radius ?? 3;
  if (sprite) {
    ctx.drawImage(sprite, x - sprite.width / 2, y - sprite.height / 2);
  } else {
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fillStyle = withAlpha(unit.color, "99");
    ctx.fill();
  }
  return true;
}
function drawAliveUnitHighlightLayer(ctx, projection, activeUnits, currentGameloop, highlightedAliveUnitKey) {
  ctx.save();
  ctx.strokeStyle = withAlpha(ALIVE_UNIT_HIGHLIGHT_COLOR, "EE");
  ctx.shadowColor = withAlpha(ALIVE_UNIT_HIGHLIGHT_COLOR, "AA");
  ctx.shadowBlur = 8;
  for (const unit of activeUnits) {
    if (unit.aliveUnitHighlightKey !== highlightedAliveUnitKey || currentGameloop < unit.spawnGameloop || unit.expiresGameloop <= currentGameloop) {
      continue;
    }
    const progress = clamp((currentGameloop - unit.spawnGameloop) * unit.inverseLifetime, 0, 1);
    const x = projectX(projection, unit.spawnX + unit.deltaX * progress);
    const y = projectY(projection, unit.spawnY + unit.deltaY * progress);
    const radius = unit.render?.radius ?? 3;
    ctx.lineWidth = Math.max(2, radius * 0.36);
    ctx.beginPath();
    ctx.arc(x, y, radius + Math.max(4, radius * 0.45), 0, Math.PI * 2);
    ctx.stroke();
  }
  ctx.restore();
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

// TypeScript/state.ts
var ALIVE_UNIT_ROW_SELECTOR = "[data-spawn-playback-alive-unit-row]";
var ALIVE_UNIT_CLEAR_SELECTOR = "[data-spawn-playback-clear-highlight]";
var ALIVE_UNIT_SELECTED_CLASS = "spawn-playback-alive-row-selected";
var longTaskObservers = /* @__PURE__ */ new WeakMap();
function initializeSpawnPlayback(canvas, rootElement, replay, callbackRef, gameloopsPerSecond, speedMultiplier) {
  const startedAt = performance.now();
  let stageStarted = startedAt;
  const stages = [];
  const markStage = (name) => {
    const now = performance.now();
    stages.push(`${name}=${(now - stageStarted).toFixed(1)}ms`);
    stageStarted = now;
  };
  const normalizedReplay = normalizeReplay(replay);
  markStage("normalizeReplay");
  const state = {
    replay: normalizedReplay,
    callbackRef,
    gameloopsPerSecond: Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0 ? gameloopsPerSecond : 22.4,
    speedMultiplier: Number.isFinite(speedMultiplier) && speedMultiplier > 0 ? speedMultiplier : 1,
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
    unitSpriteCache: /* @__PURE__ */ new Map(),
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
    startedAt
  );
}
function observeSpawnPlaybackResize(canvas) {
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
    state.resizeObserver = new ResizeObserver((entries) => handleResizeObserved(canvas, state, entries));
  }
  state.resizeObserver.observe(canvas);
  logPlaybackDiagnostic(
    `observe resize attached modal=${state.modalElement !== null} client=${canvas.clientWidth}x${canvas.clientHeight} backing=${canvas.width}x${canvas.height}`,
    startedAt
  );
}
function startSpawnPlayback(canvas, currentGameloop, speedMultiplier) {
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
  state.animationFrameId = requestAnimationFrame((timestamp) => animateSpawnPlayback(canvas, timestamp));
}
function pauseSpawnPlayback(canvas, notify = true) {
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
function stopSpawnPlayback(canvas, notify = true) {
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
function setSpawnPlaybackSpeed(canvas, speedMultiplier) {
  const state = getState(canvas);
  if (!state || state.isDisposing || !Number.isFinite(speedMultiplier) || speedMultiplier <= 0) {
    return;
  }
  state.speedMultiplier = speedMultiplier;
}
async function setSpawnPlaybackFullscreen(canvas, rootElement, fullscreen) {
  const state = getState(canvas);
  if (!state || state.isDisposing) {
    return;
  }
  if (rootElement) {
    state.rootElement = rootElement;
  }
  if (fullscreen) {
    const target = state.rootElement;
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
function disposeSpawnPlayback(canvas) {
  const startedAt = performance.now();
  const state = getState(canvas);
  console.log(`spawnPlayback dispose start hasState=${state !== void 0} frame=${state?.animationFrameId ?? 0} running=${state?.running ?? false} sprites=${state?.unitSpriteCache.size ?? 0} active=${state?.activeUnits.length ?? 0} hasStatic=${state?.staticBackgroundCanvas !== null} - ${Date.now()}`);
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
function syncAliveUnitHighlightSelection(canvas) {
  const state = getState(canvas);
  syncAliveUnitHighlightRows(state);
}
function animateSpawnPlayback(canvas, timestamp) {
  const state = getState(canvas);
  if (!state?.running || !state.isMounted || state.isDisposing) {
    return;
  }
  if (state.lastFrameTimestamp === 0) {
    state.lastFrameTimestamp = timestamp;
  }
  const elapsedSeconds = Math.max(0, timestamp - state.lastFrameTimestamp) / 1e3;
  state.lastFrameTimestamp = timestamp;
  state.currentGameloop = clampGameloop(
    state,
    state.currentGameloop + elapsedSeconds * state.gameloopsPerSecond * state.speedMultiplier
  );
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
  state.animationFrameId = requestAnimationFrame((nextTimestamp) => {
    const nextState = getState(canvas);
    if (!nextState?.isMounted || nextState.isDisposing) {
      return;
    }
    animateSpawnPlayback(canvas, nextTimestamp);
  });
}
function notifyProgress(state, status) {
  state.callbackRef?.invokeMethodAsync(
    "ReceiveSpawnPlaybackProgress",
    Math.round(state.currentGameloop),
    status
  ).catch(() => {
  });
}
function disposeState(state) {
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
function cancelAnimation(state) {
  if (state?.animationFrameId) {
    cancelAnimationFrame(state.animationFrameId);
    state.animationFrameId = 0;
  }
}
function handleResizeObserved(canvas, state, entries) {
  const startedAt = performance.now();
  const entry = entries[0];
  console.log(
    `spawnPlayback resizeObserver start entries=${entries.length} content=${entry?.contentRect.width.toFixed(1) ?? "-"}x${entry?.contentRect.height.toFixed(1) ?? "-"} client=${canvas.clientWidth}x${canvas.clientHeight} backing=${canvas.width}x${canvas.height} contains=${document.contains(canvas)} ${getRootVisibilityDiagnostics(state.rootElement)} - ${Date.now()}`
  );
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
function getResizeSkipReason(state, canvas, entry) {
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
function isRootOrModalHidden(rootElement) {
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
  return modalStyle.display === "none" || modalStyle.visibility === "hidden" || modal.getAttribute("aria-hidden") === "true";
}
function suspendSpawnPlayback(state, reason) {
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
function cancelPendingResize(state) {
  if (state.pendingResizeRaf === null) {
    return;
  }
  cancelAnimationFrame(state.pendingResizeRaf);
  state.pendingResizeRaf = null;
  console.log(`spawnPlayback pending resize raf canceled - ${Date.now()}`);
}
function startLongTaskObserver(canvas) {
  if (!("PerformanceObserver" in window)) {
    return;
  }
  const supportedTypes = PerformanceObserver.supportedEntryTypes ?? [];
  if (!supportedTypes.includes("longtask")) {
    return;
  }
  try {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        console.log(`spawnPlayback longtask duration=${entry.duration.toFixed(1)}ms start=${entry.startTime.toFixed(1)} - ${Date.now()}`);
      }
    });
    observer.observe({ entryTypes: ["longtask"] });
    longTaskObservers.set(canvas, observer);
  } catch {
  }
}
function stopLongTaskObserver(canvas) {
  const observer = longTaskObservers.get(canvas);
  if (!observer) {
    return;
  }
  observer.disconnect();
  longTaskObservers.delete(canvas);
}
function logPlaybackStage(message, startedAt) {
  console.log(`spawnPlayback ${message} elapsed=${(performance.now() - startedAt).toFixed(1)}ms - ${Date.now()}`);
}
function logPlaybackDiagnostic(message, startedAt) {
  console.log(`spawnPlayback ${message} elapsed=${(performance.now() - startedAt).toFixed(1)}ms - ${Date.now()}`);
}
function handleFullscreenChange(canvas) {
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
function notifyFullscreenChanged(state) {
  state.callbackRef?.invokeMethodAsync(
    "ReceiveSpawnPlaybackFullscreenChanged",
    document.fullscreenElement === state.rootElement
  ).catch(() => {
  });
}
function initializeAliveUnitHighlightEvents(canvas, state) {
  if (!state.rootElement) {
    return;
  }
  state.aliveUnitClickListener = (event) => handleAliveUnitHighlightClick(canvas, event);
  state.aliveUnitKeydownListener = (event) => handleAliveUnitHighlightKeydown(canvas, event);
  state.rootElement.addEventListener("click", state.aliveUnitClickListener);
  state.rootElement.addEventListener("keydown", state.aliveUnitKeydownListener);
}
function disposeAliveUnitHighlightEvents(state) {
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
function handleAliveUnitHighlightClick(canvas, event) {
  const target = event.target;
  if (!(target instanceof Element)) {
    return;
  }
  if (target.closest(ALIVE_UNIT_CLEAR_SELECTOR)) {
    setAliveUnitHighlight(canvas, null);
    return;
  }
  const row = target.closest(ALIVE_UNIT_ROW_SELECTOR);
  if (!row) {
    return;
  }
  const key = row.dataset.spawnPlaybackHighlightKey;
  if (key) {
    toggleAliveUnitHighlight(canvas, key);
  }
}
function handleAliveUnitHighlightKeydown(canvas, event) {
  const keyboardEvent = event;
  if (keyboardEvent.key !== "Enter" && keyboardEvent.key !== " ") {
    return;
  }
  const target = keyboardEvent.target;
  if (!(target instanceof Element)) {
    return;
  }
  const row = target.closest(ALIVE_UNIT_ROW_SELECTOR);
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
function toggleAliveUnitHighlight(canvas, key) {
  const state = getState(canvas);
  if (!state) {
    return;
  }
  setAliveUnitHighlight(canvas, resolveAliveUnitHighlightToggle(state.highlightedAliveUnitKey, key));
}
function resolveAliveUnitHighlightToggle(currentKey, nextKey) {
  return currentKey === nextKey ? null : nextKey;
}
function setAliveUnitHighlight(canvas, key) {
  const state = getState(canvas);
  if (!state || state.highlightedAliveUnitKey === key) {
    return;
  }
  state.highlightedAliveUnitKey = key;
  syncAliveUnitHighlightRows(state);
  requestAliveUnitHighlightRedraw(canvas, state);
}
function syncAliveUnitHighlightRows(state) {
  const rootElement = state?.rootElement;
  if (!rootElement) {
    return;
  }
  const selectedKey = state.highlightedAliveUnitKey;
  const rows = rootElement.querySelectorAll(ALIVE_UNIT_ROW_SELECTOR);
  for (const row of rows) {
    const selected = selectedKey !== null && row.dataset.spawnPlaybackHighlightKey === selectedKey;
    row.classList.toggle(ALIVE_UNIT_SELECTED_CLASS, selected);
    row.setAttribute("aria-pressed", selected ? "true" : "false");
  }
}
function requestAliveUnitHighlightRedraw(canvas, state) {
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
function getRootVisibilityDiagnostics(rootElement) {
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
export {
  disposeSpawnPlayback,
  drawSpawnPlayback,
  hydrateUnitIcons,
  initializeSpawnPlayback,
  observeSpawnPlaybackResize,
  pauseSpawnPlayback,
  setSpawnPlaybackFullscreen,
  setSpawnPlaybackSpeed,
  startSpawnPlayback,
  stopSpawnPlayback,
  syncAliveUnitHighlightSelection
};
