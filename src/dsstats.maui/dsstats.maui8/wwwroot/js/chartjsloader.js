import * as annotationPlugin from './chartjs-plugin-annotation.min.js';

export function registerAnnotationPlugin() {
    Chart.register(annotationPlugin);
}