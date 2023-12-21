import * as annotationPlugin from './chartjs-plugin-annotation.min.js';

export async function registerPlugin() {
    // await import('./chartjs-plugin-annotation.min.js');
    Chart.register(annotationPlugin);
}

export function drawArrows(team, chartId) {
    const chart = Chart.getChart(chartId);

    if (chart == undefined) {
        return;
    }

    const annotations = {
        annotations: {
            pentagon1: {
                type: 'polygon',
                xValue: 1,
                yValue: 60,
                sides: 5,
                radius: 60,
                backgroundColor: 'rgba(255, 99, 132, 0.25)'
            },
            pentagon2: {
                type: 'polygon',
                xValue: 4,
                yValue: 60,
                sides: 5,
                radius: 60,
                backgroundColor: 'rgba(255, 99, 132, 0.25)'
            }
        }
    };


    //if (team == 1) {
    //    annotations.annotations.pentagon1.yMin = 1;
    //    annotations.annotations.pentagon1.yMax = 5;
    //    annotations.annotations.pentagon2.yMin = 1;
    //    annotations.annotations.pentagon2.yMax = 5;

    //} else {
    //    annotations.annotations.pentagon1.yMin = 11;
    //    annotations.annotations.pentagon1.yMax = 15;
    //    annotations.annotations.pentagon2.yMin = 11;
    //    annotations.annotations.pentagon2.yMax = 15;
    //}

    chart.config.options.plugins["annotation"] = annotations;

    chart.update();
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