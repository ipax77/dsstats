import './chartjs-adapter-date-fns.bundle.min.js'

export function registerPlugin() {

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