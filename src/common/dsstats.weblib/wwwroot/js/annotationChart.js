//v0.6

import * as annotationPlugin from './chartjs-plugin-annotation.min.js';

export async function registerPlugin() {
    Chart.register(annotationPlugin);
}

export async function registerChartWithPlugin() {
    const { Chart } = await import('chart.js');
    Chart.register(annotationPlugin);
}

export function setChartTooltips(chartId) {
    const chart = Chart.getChart(chartId);

    if (chart == undefined) {
        return;
    }

    chart.options.plugins.tooltip.callbacks.label = (tooltipItem) => {
        if (tooltipItem == undefined) {
            return "";
        } else {
            return tooltipItem.raw.label;
        }
    };
}

export function setDatasetPointsActive(chartId, datasetIndex) {
    const chart = Chart.getChart(chartId);

    if (chart.data.datasets.length <= datasetIndex) {
        return;
    }

    if (!chart) {
        return;
    }

    if (!chart.data || !chart.data.datasets || chart.data.datasets.length <= datasetIndex) {
        return;
    }

    if (chart.getActiveElements().length > 0) {
        chart.setActiveElements([]);
        chart.update();
    }

    var dataset = chart.data.datasets[datasetIndex];

    var elements = [];
    for (var i = 0; i < dataset.data.length; i++) {
        elements.push({ datasetIndex: datasetIndex, index: i });
    }

    chart.setActiveElements(elements);
    chart.update();
}


export function drawChartBorder(chartId) {
    const chart = Chart.getChart(chartId);

    if (chart == undefined) {
        return;
    }

    Chart.register(chartAreaBorder);

    chart.config.options.plugins.chartAreaBorder = {
        borderColor: '#dee2e6',
        borderWidth: 2,
    };

    chart.update();
}

const chartAreaBorder = {
    id: 'chartAreaBorder',
    beforeDraw(chart, args, options) {
        const { ctx, chartArea: { left, top, width, height } } = chart;
        ctx.save();
        ctx.strokeStyle = options.borderColor;
        ctx.lineWidth = options.borderWidth;
        ctx.strokeRect(left, top, width, height);
        ctx.restore();
    }
};

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}