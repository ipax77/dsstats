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
var zergUnits = {
  zergling: zergZergling,
  roach: zergRoach,
  queen: zergQueen
};

// TypeScript/unitIcons.ts
var definitions = [
  ...Object.values(terranUnits),
  ...Object.values(zergUnits)
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
