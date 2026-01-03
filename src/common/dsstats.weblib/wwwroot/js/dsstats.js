// v3.0

const cmdrIconsMap = new Map();

function openModalById(id) {
    const modalElement = document.getElementById(id);
    if (!modalElement) return;

    let modal = bootstrap.Modal.getInstance(modalElement);
    if (!modal) {
        modal = new bootstrap.Modal(modalElement);
    }
    modal.show();
}

function closeModalById(id) {
    const modalElement = document.getElementById(id);
    if (!modalElement) return;

    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) {
        modal.hide();
    }
}

async function enableTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
}

function disableTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        var tooltip = bootstrap.Tooltip.getInstance(tooltipTriggerEl);
        tooltip.hide();
        tooltip.disable();
    })
}

function registerImagePlugin(xWidth, yWidth) {
    preloadChartIcons(xWidth, yWidth).then(() => {
        const barIcons = barIconsPlugin();
        Chart.register(barIcons);
    });
}

function preloadChartIcons(xWidth, yWidth) {
    if (cmdrIconsMap.size > 0) {
        return Promise.resolve();
    }

    const cmdrs = [
        "terran", "protoss", "zerg", "abathur", "alarak", "artanis", "dehaka",
        "fenix", "horner", "karax", "kerrigan", "mengsk", "nova", "raynor",
        "stetmann", "stukov", "swann", "tychus", "vorazun", "zagara", "zeratul"
    ];

    const loadPromises = cmdrs.map(cmdr =>
        loadImageWithTimeout(cmdr, xWidth, yWidth)
    );

    return Promise.all(loadPromises);
}

function loadImageWithTimeout(cmdr, xWidth, yWidth, timeoutMs = 5000) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.width = xWidth;
        img.height = yWidth;

        const timer = setTimeout(() => {
            resolve(); // continue even if broken
        }, timeoutMs);

        img.onload = () => {
            clearTimeout(timer);
            cmdrIconsMap.set(cmdr, img);
            resolve();
        };

        img.onerror = () => {
            clearTimeout(timer);
            resolve(); // continue despite error
        };

        img.src = `_content/dsstats.weblib/images/${cmdr}-min.png`;
    });
}


function increaseChartHeight(chartId, height) {
    const chart = Chart.getChart(chartId);

    const originalFit = chart.legend.fit;
    chart.legend.fit = function fit() {
        originalFit.bind(chart.legend)();
        this.height += height;
    }
}

function barIconsPlugin() {
    return {
        id: 'barIcons',
        // beforeDraw(chart, args, options) {
        afterDatasetDraw(chart, args, options) {
            // afterDraw(chart, args, options) {
            const { ctx, chartArea: { top, right, bottom, left, width, height }, scales: { x, y } } = chart;

            ctx.save();
            const meta = chart.getDatasetMeta(0);
            if (meta != undefined) {
                for (let i = 0; i < options.length; i++) {
                    var option = options[i];
                    const xWidth = option.xWidth;
                    const yWidth = option.xWidth;
                    const yOffset = option.yOffset;


                    const elem = meta.data[i];
                    if (elem != undefined) {
                        let raw = elem["$context"].raw;
                        let x0 = 0;
                        let y0 = 0;
                        if (x != undefined) {
                            x0 = x.getPixelForValue(i) - (xWidth / 2);
                            y0 = elem.y - yWidth + yOffset;
                        } else {
                            if (elem.startAngle == undefined) {
                                x0 = elem.x;
                                y0 = elem.y;
                            } else {
                                const piePos = getPieIconPos(elem);
                                x0 = piePos.x;
                                y0 = piePos.y + yOffset;
                            }
                        }

                        if (raw < 0) {
                            y0 = y.getPixelForValue(0) - yWidth;
                        }

                        const img = cmdrIconsMap.get(option.cmdr);

                        if (img == undefined) {

                            const img = new Image();
                            img.onload = () => {
                                ctx.drawImage(img, x0, y0, xWidth, yWidth);
                            };
                            img.src = option.imageSrc;
                        } else {
                            ctx.drawImage(img, x0, y0, img.width, img.height);
                        }
                    }
                }
            }
            ctx.restore();
        }
    };
}