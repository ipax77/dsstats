

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

export function drawCellInfo2(x, y, gridCellSize, color) {
    var scene = document.getElementById("canvas");
    var ctx = scene.getContext("2d");

    ctx.save();
    ctx.fillStyle = color;
    ctx.fillRect(x, y, gridCellSize, gridCellSize);
    ctx.restore();
}