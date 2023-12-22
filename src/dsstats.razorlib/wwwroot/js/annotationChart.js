import * as annotationPlugin from './chartjs-plugin-annotation.min.js';

export async function registerPlugin() {
    // await import('./chartjs-plugin-annotation.min.js');
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
            return tooltipItem.raw.label + " " + tooltipItem.raw.x + "|" + tooltipItem.raw.y;
            // return tooltipItem.raw.label;
        }
    };
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