const states = new WeakMap();

export function initializeSpawnPlayback(canvas, replay) {
    const state = {
        replay,
        resizeObserver: null,
        currentGameloop: 0
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

    resizeCanvas(canvas);

    const ctx = canvas.getContext("2d");
    const replay = state.replay;
    state.currentGameloop = currentGameloop;
    const bounds = replay.bounds ?? replay.Bounds;
    const stepGameloops = replay.stepGameloops ?? replay.StepGameloops ?? 112;
    const players = replay.players ?? replay.Players ?? [];
    const landmarks = replay.landmarks ?? replay.Landmarks ?? [];

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawBackground(ctx, canvas, bounds);
    drawLandmarks(ctx, canvas, bounds, landmarks);

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
    }
}

function drawBackground(ctx, canvas, bounds) {
    ctx.save();
    ctx.fillStyle = "#071015";
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.strokeStyle = "rgba(255, 255, 255, 0.12)";
    ctx.lineWidth = 1;
    for (let i = 1; i < 4; i++) {
        const x = (canvas.width / 4) * i;
        const y = (canvas.height / 4) * i;
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, canvas.height);
        ctx.moveTo(0, y);
        ctx.lineTo(canvas.width, y);
        ctx.stroke();
    }

    ctx.strokeStyle = "rgba(255, 193, 7, 0.45)";
    ctx.setLineDash([8, 8]);
    const mid = project((bounds.minX + bounds.maxX) / 2, (bounds.minY + bounds.maxY) / 2, bounds, canvas);
    ctx.beginPath();
    ctx.moveTo(mid.x, 0);
    ctx.lineTo(mid.x, canvas.height);
    ctx.stroke();
    ctx.restore();
}

function drawLandmarks(ctx, canvas, bounds, landmarks) {
    for (const landmark of landmarks) {
        drawLandmark(ctx, canvas, bounds, landmark);
    }
}

function drawLandmark(ctx, canvas, bounds, landmark) {
    const x = landmark.x ?? landmark.X;
    const y = landmark.y ?? landmark.Y;
    if (x == null || y == null) {
        return;
    }

    const projected = project(x, y, bounds, canvas);
    const kind = landmark.kind ?? landmark.Kind ?? "Defense";
    const name = landmark.name ?? landmark.Name ?? kind;
    const kills = landmark.kills ?? landmark.Kills ?? 0;
    const label = kills > 0 ? `${name} ${kills}k` : name;
    const teamId = landmark.teamId ?? landmark.TeamId;
    const color = landmark.color ?? landmark.Color ?? (teamId === 1 ? "#5DADEC" : "#F87171");
    const radius = Math.max(7, (landmark.radius ?? landmark.Radius ?? 10) * deviceScale(canvas) * 0.7);

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

function drawUnit(ctx, canvas, bounds, unit, teamId, currentGameloop, stepGameloops) {
    const spawnGameloop = unit.spawnGameloop ?? unit.SpawnGameloop;
    if (currentGameloop < spawnGameloop) {
        return false;
    }

    const diedGameloop = unit.diedGameloop ?? unit.DiedGameloop;
    const diedThisStep = diedGameloop != null
        && diedGameloop <= currentGameloop
        && diedGameloop > currentGameloop - stepGameloops;

    if (diedThisStep) {
        const diedX = unit.diedX ?? unit.DiedX;
        const diedY = unit.diedY ?? unit.DiedY;
        if (diedX != null && diedY != null) {
            drawDeath(ctx, canvas, bounds, unit, diedX, diedY);
            return true;
        }
        return false;
    }

    if (diedGameloop != null && diedGameloop <= currentGameloop) {
        return false;
    }

    const position = getUnitPosition(unit, currentGameloop, diedGameloop);
    const projected = project(position.x, position.y, bounds, canvas);
    const radius = Math.max(3, (unit.radius ?? unit.Radius ?? 8) * deviceScale(canvas) * 0.55);
    const color = unit.color ?? unit.Color ?? "#EC7063";
    const kills = unit.kills ?? unit.Kills ?? 0;

    ctx.save();
    ctx.globalAlpha = teamId === 1 ? 0.92 : 0.78;
    ctx.fillStyle = withAlpha(color, "99");
    ctx.strokeStyle = withAlpha(color, "EE");
    ctx.lineWidth = Math.max(kills > 0 ? 2.5 : 1.5, deviceScale(canvas) * (kills > 0 ? 2 : 1.5));
    ctx.beginPath();
    ctx.arc(projected.x, projected.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    if (kills > 0) {
        drawKillLabel(ctx, canvas, projected.x, projected.y - radius - 2 * deviceScale(canvas), kills);
    }

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

function drawDeath(ctx, canvas, bounds, unit, x, y) {
    const projected = project(x, y, bounds, canvas);
    const radius = Math.max(5, (unit.radius ?? unit.Radius ?? 8) * deviceScale(canvas) * 0.65);
    const color = unit.color ?? unit.Color ?? "#EC7063";
    const kills = unit.kills ?? unit.Kills ?? 0;

    ctx.save();
    ctx.strokeStyle = withAlpha(color, "FF");
    ctx.lineWidth = Math.max(2, deviceScale(canvas) * 2);
    ctx.beginPath();
    ctx.moveTo(projected.x - radius, projected.y - radius);
    ctx.lineTo(projected.x + radius, projected.y + radius);
    ctx.moveTo(projected.x + radius, projected.y - radius);
    ctx.lineTo(projected.x - radius, projected.y + radius);
    ctx.stroke();

    if (kills > 0) {
        drawKillLabel(ctx, canvas, projected.x, projected.y - radius - 2 * deviceScale(canvas), kills);
    }

    ctx.restore();
}

function drawKillLabel(ctx, canvas, x, y, kills) {
    const scale = deviceScale(canvas);
    ctx.fillStyle = "rgba(255, 255, 255, 0.9)";
    ctx.font = `${Math.max(9, 9 * scale)}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "bottom";
    ctx.fillText(`${kills}k`, x, y);
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
