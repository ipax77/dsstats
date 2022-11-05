
function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time));
}

async function enableTooltips() {
    await delay(100);
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
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

function registerImagePlugin(chartId) {
    const chart = Chart.getChart(chartId);
    const barIcons = barIconsPlugin();
    Chart.register(barIcons);
}

function barIconsPlugin() {
    return {
        id: 'barIcons',
        // beforeDraw(chart, args, options) {
        // afterDatasetDraw(chart, args, options) {
        afterDraw(chart, args, options) {
            const { ctx, chartArea: { top, right, bottom, left, width, height }, scales: { x, y } } = chart;

            ctx.save();
            const meta = chart.getDatasetMeta(0);
            if (meta != undefined) {
                for (let i = 0; i < options.length; i++) {
                    var option = options[i];
                    const xWidth = option.xWidth;
                    const yWidth = option.xWidth;
                    const yOffset = option.yOffset;
                    const img = new Image();
                    img.src = option.imageSrc;

                    const x0 = x.getPixelForValue(i) - (xWidth / 2);

                    const elem = meta.data[i];
                    if (elem != undefined) {
                        const y0 = elem.y - yWidth + yOffset;

                        ctx.drawImage(img, x0, y0, xWidth, yWidth);
                    }
                }
            }
            ctx.restore();
        }
    };
}