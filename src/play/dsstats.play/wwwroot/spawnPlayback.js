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
    middleControl: normalizeMiddleControl(replay),
    landmarks: readArray(replay, "landmarks", "Landmarks").map(asObject),
    buildUnits: readArray(replay, "buildUnits", "BuildUnits"),
    snapshots: readArray(replay, "snapshots", "Snapshots"),
    players,
    units
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
  return segment ? projectSegment(segment, bounds, canvas) : null;
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
var terranUnits = {
  marine: terranMarine
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
var zergUnits = {
  zergling: zergZergling
};

// TypeScript/unitIcons.ts
var definitions = [
  terranUnits.marine,
  zergUnits.zergling
];
var aliases = /* @__PURE__ */ new Map();
var svgCache = /* @__PURE__ */ new Map();
var tokenCache = /* @__PURE__ */ new Map();
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
    const definition = unitIconCatalog.resolve(commander, unitName);
    const renderKey = `${commander}|${unitName}|${size}|${teamColor ?? ""}|${definition?.id ?? ""}`;
    if (host.dataset.renderedIconKey === renderKey) {
      continue;
    }
    host.dataset.renderedIconKey = renderKey;
    host.innerHTML = definition ? toSvg(definition, { size, teamColor }) : fallbackSvg(size, teamColor ?? "#8a949e");
  }
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
function normalize(value) {
  return String(value ?? "").trim().toLowerCase().replace(/ /g, "");
}

// TypeScript/rendering.ts
function drawSpawnPlayback(canvas, currentGameloop) {
  const state = getState(canvas);
  if (!state?.replay) {
    return;
  }
  const resized = resizeCanvas(canvas);
  const ctx = getCanvasContext(canvas);
  const replay = state.replay;
  const bounds = replay.bounds;
  if (!ctx || !bounds) {
    return;
  }
  state.currentGameloop = clampGameloop(state, currentGameloop);
  if (resized || !state.staticGeometry || !state.renderCache || state.staticCanvasWidth !== canvas.width || state.staticCanvasHeight !== canvas.height) {
    state.renderCache = createRenderCache(bounds, canvas);
    state.staticGeometry = createStaticGeometry(replay, bounds, canvas);
    state.staticBackgroundCanvas = createStaticBackgroundCanvas(canvas, state.staticGeometry);
    state.staticCanvasWidth = canvas.width;
    state.staticCanvasHeight = canvas.height;
    state.activeUnits.length = 0;
    state.nextUnitIndex = 0;
    state.lastActiveGameloop = Number.NEGATIVE_INFINITY;
    prepareUnitSprites(state, canvas);
  }
  if (!state.staticBackgroundCanvas || !state.staticGeometry || !state.renderCache) {
    return;
  }
  ctx.drawImage(state.staticBackgroundCanvas, 0, 0);
  drawDynamicMapLayer(ctx, canvas, state.staticGeometry, state.currentGameloop);
  const activeUnits = getActiveUnits(state, state.currentGameloop);
  const drawnUnits = drawUnitLayer(ctx, state.renderCache.projection, activeUnits, state.currentGameloop);
  if (drawnUnits === 0) {
    drawEmptyState(ctx, canvas);
  }
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
  ctx.fillStyle = "#071015";
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.restore();
  drawGrid(ctx, canvas, geometry.gridLines);
  drawSpawnAreas(ctx, canvas, geometry.spawnAreas);
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
  ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
  ctx.strokeStyle = withAlpha(color, "EE");
  ctx.fillStyle = withAlpha(color, kind === "Base" ? "44" : "55");
  if (kind === "Base") {
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
  ctx.fillText(label, projected.x, projected.y + radius + 3 * deviceScale(canvas));
  ctx.restore();
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
function initializeSpawnPlayback(canvas, rootElement, replay, callbackRef, gameloopsPerSecond, speedMultiplier) {
  const state = {
    replay: normalizeReplay(replay),
    callbackRef,
    gameloopsPerSecond: Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0 ? gameloopsPerSecond : 22.4,
    speedMultiplier: Number.isFinite(speedMultiplier) && speedMultiplier > 0 ? speedMultiplier : 1,
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
    unitSpriteCache: /* @__PURE__ */ new Map(),
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
function startSpawnPlayback(canvas, currentGameloop, speedMultiplier) {
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
  state.animationFrameId = requestAnimationFrame((timestamp) => animateSpawnPlayback(canvas, timestamp));
}
function pauseSpawnPlayback(canvas, notify = true) {
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
function stopSpawnPlayback(canvas, notify = true) {
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
function setSpawnPlaybackSpeed(canvas, speedMultiplier) {
  const state = getState(canvas);
  if (!state || !Number.isFinite(speedMultiplier) || speedMultiplier <= 0) {
    return;
  }
  state.speedMultiplier = speedMultiplier;
}
async function setSpawnPlaybackFullscreen(canvas, rootElement, fullscreen) {
  const state = getState(canvas);
  if (!state) {
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
  const state = getState(canvas);
  disposeState(state);
  deleteState(canvas);
}
function animateSpawnPlayback(canvas, timestamp) {
  const state = getState(canvas);
  if (!state?.running) {
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
  state.animationFrameId = requestAnimationFrame((nextTimestamp) => animateSpawnPlayback(canvas, nextTimestamp));
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
function cancelAnimation(state) {
  if (state?.animationFrameId) {
    cancelAnimationFrame(state.animationFrameId);
    state.animationFrameId = 0;
  }
}
function handleFullscreenChange(canvas) {
  const state = getState(canvas);
  if (!state) {
    return;
  }
  notifyFullscreenChanged(state);
  requestAnimationFrame(() => drawSpawnPlayback(canvas, state.currentGameloop));
}
function notifyFullscreenChanged(state) {
  state.callbackRef?.invokeMethodAsync(
    "ReceiveSpawnPlaybackFullscreenChanged",
    document.fullscreenElement === state.rootElement
  ).catch(() => {
  });
}
export {
  disposeSpawnPlayback,
  drawSpawnPlayback,
  hydrateUnitIcons,
  initializeSpawnPlayback,
  pauseSpawnPlayback,
  setSpawnPlaybackFullscreen,
  setSpawnPlaybackSpeed,
  startSpawnPlayback,
  stopSpawnPlayback
};
