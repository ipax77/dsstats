

export function drawGrid(x, y, width, height, gridCellSize, color, lineWidth = 1) {
    var scene = document.getElementById("canvas");
    var ctx = scene.getContext("2d");
    ctx.clearRect(0, 0, scene.width, scene.height);
    ctx.save();
    ctx.beginPath();
    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = color;

    for (var lx = x; lx <= x + width; lx += gridCellSize) {
        ctx.moveTo(lx, y);
        ctx.lineTo(lx, y + height);
    }

    for (var ly = y; ly <= y + height; ly += gridCellSize) {
        ctx.moveTo(x, ly);
        ctx.lineTo(x + width, ly);
    }

    ctx.stroke();
    ctx.closePath();
    ctx.restore();
}

export function drawCellInfo(x, y, gridCellSize, color, text) {
    var scene = document.getElementById("canvas");
    var ctx = scene.getContext("2d");

    ctx.save();
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x + gridCellSize / 2, y + gridCellSize / 2, gridCellSize / 2, 0, 2 * Math.PI);
    ctx.fill();
    ctx.fillStyle = "white";
    ctx.font = "12px Arial";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(text, x + gridCellSize / 2, y + gridCellSize / 2);

    ctx.restore();
}

export function drawCellInfos(points, gridCellSize, color, team, canvasId) {
    var scene = document.getElementById(canvasId);
    var ctx = scene.getContext("2d");
    ctx.clearRect(0, 0, scene.width, scene.height);
    ctx.save();

    points.forEach((point) => {
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(point.x + gridCellSize / 2, point.y + gridCellSize / 2, gridCellSize / 2, 0, 2 * Math.PI);
        ctx.fill();
        ctx.save();

        ctx.translate(point.x + gridCellSize / 2, point.y + gridCellSize / 2);
        ctx.rotate(-Math.PI / 4);
        ctx.fillStyle = "white";
        ctx.font = "12px Arial";
        ctx.textAlign = "start";
        ctx.textBaseline = "middle";
        ctx.fillText(point.name, 0, 0);
        ctx.restore();
    });

    ctx.restore();
    drawArrows(ctx, scene.width, scene.height, team, 15);
}

function drawArrows(context, width, height, team, arrowSize) {
    if (team === 1) {
        drawArrow(context, arrowSize, arrowSize, arrowSize, arrowSize);
        drawArrow(context, width - arrowSize, arrowSize, arrowSize, arrowSize);
    } else {
        drawArrow(context, arrowSize, height - arrowSize, arrowSize, arrowSize, Math.PI);
        drawArrow(context, width - arrowSize, height - arrowSize, arrowSize, arrowSize, Math.PI);
    }
}

function drawArrow(context, x, y, width, height, rotation = 0) {
    context.save();
    context.translate(x, y);
    context.rotate(rotation);

    context.beginPath();
    context.moveTo(-width / 2, height / 2);
    context.lineTo(0, -height / 2);
    context.lineTo(width / 2, height / 2);
    context.closePath();

    // You can set the fillStyle and strokeStyle as per your requirements
    context.fillStyle = "white";
    context.fill();

    context.restore();
}