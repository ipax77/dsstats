import './chartjs-adapter-date-fns.bundle.min.js'
import * as annotationPlugin from './chartjs-plugin-annotation.min.js';

export async function registerPlugin() {
    await import('./chartjs-plugin-annotation.min.js');
    Chart.register(annotationPlugin);
}

export function setXAxisMin(chartId, min) {
    const chart = Chart.getChart(chartId);
    if (chart != undefined) {
        chart.options.scales.x.min = min;
        chart.update();
    }
}

export function setXAxisMax(chartId, max) {
    const chart = Chart.getChart(chartId);
    if (chart != undefined) {
        chart.options.scales.x.max = max;
        chart.update();
    }
}