
const cmdrIconsMap = new Map();

function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time));
}

async function enableTooltips() {
    await delay(100);
    //const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    //const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })
}

function disableTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        var tooltip = bootstrap.Tooltip.getInstance(tooltipTriggerEl);
        tooltip.hide();
        tooltip.disable();
    })
}

function ReplayModalOpen(name) {

    const modalElement = document.getElementById(name);

    if (!modalElement) {
        return;
    }

    replayModal = new bootstrap.Modal(modalElement);
    replayModal.show();
}

function ReplayModalClose() {
    if (replayModal) {
        replayModal.hide();
    }
}

function setMultiSelect(id, selectedValues) {
    var multiSelect = document.querySelector('#' + id);
    var options = Array.from(multiSelect.options);
    options.forEach(function (option) {
        if (selectedValues.includes(option.value)) {
            option.selected = true;
        } else {
            option.selected = false;
        }
    });
}

function registerImagePlugin(xWidth, yWidth) {
    const barIcons = barIconsPlugin();
    Chart.register(barIcons);
    preloadChartIcons(xWidth, yWidth);
}

function registerbubbleLabelsPlugin() {
    const bubbleLabels = bubbleLabelsPlugin();
    Chart.register(bubbleLabels);
}

function preloadChartIcons(xWidth, yWidth) {
    if (cmdrIconsMap.size > 0) {
        return;
    }
    const cmdrs = ["terran", "protoss", "zerg", "abathur", "alarak", "artanis", "dehaka", "fenix", "horner", "karax", "kerrigan", "mengsk", "nova", "raynor", "stetmann", "stukov", "swann", "tychus", "vorazun", "zagara", "zeratul"];
    for (let i = 0; i < cmdrs.length; i++) {
        const img = new Image(xWidth, yWidth);
        img.onload = () => {
            cmdrIconsMap.set(cmdrs[i], img);
        };
        img.src = "_content/sc2dsstats.razorlib/images/" + cmdrs[i] + "-min.png";
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

function getPieIconPos(elem) {
    const offset = 30;
    const spacing = 30;
    let halfAngle = (elem.startAngle + elem.endAngle) / 2;
    const halfRadius = (elem.innerRadius + elem.outerRadius + spacing + offset) / 2;

    return {
        x: elem.x + Math.cos(halfAngle) * halfRadius,
        y: elem.y + Math.sin(halfAngle) * halfRadius
    };
}

function scrollToId(id) {
    const ele = document.getElementById(id);
    if (ele != undefined) {
        ele.scrollIntoView({ behavior: 'smooth' });
    }
}


function bubbleLabelsPlugin() {
    const fontSize = Chart.defaults.font.size;

    return {
        id: 'bubbleLabelsPlugin',
        afterDatasetsDraw: (chart, args, options) => {
            const ctx = chart.ctx
            ctx.textAlign = 'center'
            ctx.textBaseline = 'middle'

            chart.data.datasets.forEach((dataset, i) => {
                const meta = chart.getDatasetMeta(i)
                if (meta.type !== 'bubble') return

                meta.data.forEach((element, index) => {
                    const item = dataset.data[index]
                    const position = element.tooltipPosition()
                    ctx.fillStyle = "#4D60EB";
                    ctx.fillText(item.label.toString(), position.x, position.y - item.r - fontSize)
                })
            })
        },
    }
}

function bubblePointHover(chartId, index) {
    const chart = Chart.getChart(chartId);
    chart.setActiveElements([
        {
            datasetIndex: 0,
            index: index,
        }
    ]);
    chart.update();
}