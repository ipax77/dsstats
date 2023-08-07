//v 1.1

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

function increaseChartHeight(chartId, height) {
    const chart = Chart.getChart(chartId);

    const originalFit = chart.legend.fit;
    chart.legend.fit = function fit() {
        // Call the original function and bind scope in order to use `this` correctly inside it
        originalFit.bind(chart.legend)();
        // Change the height as suggested in other answers
        this.height += height;
    }
}

function setDatalabelsFormatter(chartId) {
    const chart = Chart.getChart(chartId);
    chart.options.plugins.datalabels.formatter = function (value, context) {
        var float = Number.parseFloat(value);
        if (float < 100) {
            return float.toFixed(2);
        } else {
            return Math.round(float)
        }
    };
    chart.update();
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
                    ctx.fillStyle = "#3F5FFA";
                    ctx.fillText(item.label.toString(), position.x, position.y - item.r - fontSize)
                })
            })
        },
    }
}

function bubblePointHover(chartId, index) {
    const chart = Chart.getChart(chartId);

    if (chart.data.datasets.length == 0) {
        return;
    }

    if (chart.data.datasets[0].data.length < index) {
        return;
    }

    chart.setActiveElements([
        {
            datasetIndex: 0,
            index: index,
        }
    ]);
    chart.update();
}

function setBubbleChartTooltips(vs, min, max, rMin, rMax, chartId) {
    const chart = Chart.getChart(chartId);

    if (chart == undefined) {
        return;
    }

    chart.options.plugins.tooltip.callbacks.label = (tooltipItem) => {
        if (tooltipItem == undefined) {
            return "";
        }
        let avgStrength = Math.round((((tooltipItem.raw.r - rMin) * (max - min)) / (rMax - rMin)) + min);
        if (vs) {
            return [vs + " vs " + tooltipItem.raw.label, "Winrate " + tooltipItem.raw.x + "%", "AvgGain: " + tooltipItem.raw.y, "AvgRating: " + avgStrength];
        } else {
            return [tooltipItem.raw.label, "Winrate " + tooltipItem.raw.x + "%", "AvgGain: " + tooltipItem.raw.y, "AvgRating: " + avgStrength];
        }
    };
}


function setZeroLineColor(defaultColor, defaultTickColor, zeroColor, chartId) {
    const chart = Chart.getChart(chartId);

    if (chart == undefined) {
        return;
    }

    chart.options.scales.y.grid.color = (context) => {
        if (context.tick.value === 0) {
            return zeroColor;
        } else {
            return defaultColor;
        }
    }

    chart.options.scales.y.grid.tickColor = (context) => {
        if (context.tick.value === 0) {
            return zeroColor;
        } else {
            return defaultColor;
        }
    }

    chart.options.scales.y.ticks.color = (context) => {
        if (context.tick.value === 0) {
            return 'red';
        } else {
            return defaultTickColor;
        }
    }

    chart.options.scales.x.grid.color = (context) => {
        if (context.tick.value === 50) {
            return zeroColor;
        } else {
            return defaultColor;
        }
    }

    chart.options.scales.x.grid.tickColor = (context) => {
        if (context.tick.value === 50) {
            return zeroColor;
        } else {
            return defaultColor;
        }
    }

    chart.options.scales.x.ticks.color = (context) => {
        if (context.tick.value === 50) {
            return 'red';
        } else {
            return defaultTickColor;
        }
    }

    chart.update();
}

function setChartLegendFilter(chartId) {
    const chart = Chart.getChart(chartId);
    if (chart !== undefined) {
        chart.options.plugins.legend.labels.filter = function (legendItem, chartData) {
            return !(legendItem.lineWidth == 0);
        };
        chart.update();
    }
}

function drawYValueLine(chartId, yvalue) {
    const horizontalLine = horizontalLinePlugin();
    Chart.register(horizontalLine);

    const chart = Chart.getChart(chartId);
    if (chart !== undefined) {
        chart.options.plugins.horizontalLine = { value: yvalue };
        chart.update();
    }
}

function horizontalLinePlugin() {
    return {
        id: 'horizontalLine',
        beforeDatasetDraw(chart, args, options) {
            const { ctx, chartArea: { top, right, bottom, left, width, height },
                scales: { x, y } } = chart;
            ctx.save();

            const y0 = y.getPixelForValue(options.value);
            ctx.setLineDash([10, 5]);
            ctx.strokeStyle = 'red';
            ctx.strokeRect(left, y0, width, 0);

            const text = 'Avg';
            const textX = left - 25; // X-coordinate of the text
            const textY = y0 + 5; // Y-coordinate of the text (adjust as needed)

            ctx.fillStyle = 'red';
            ctx.font = '12px Arial';
            ctx.fillText(text, textX, textY);

            ctx.restore();
        }
    }
}

function getWindowDimension() {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
}

window.dsoptions = {
    autocomplete: {
        initialize: (elementRef, dotNetHelper) => {
            let dropdownToggleEl = elementRef;

            dropdownToggleEl.addEventListener('show.bs.dropdown', function () {
                dotNetHelper.invokeMethodAsync('bsShowAutocomplete');
            });
            dropdownToggleEl.addEventListener('shown.bs.dropdown', function () {
                dotNetHelper.invokeMethodAsync('bsShownAutocomplete');
            });
            dropdownToggleEl.addEventListener('hide.bs.dropdown', function () {
                dotNetHelper.invokeMethodAsync('bsHideAutocomplete');
            });
            dropdownToggleEl.addEventListener('hidden.bs.dropdown', function () {
                dotNetHelper.invokeMethodAsync('bsHiddenAutocomplete');
            });

            bootstrap?.Dropdown?.getOrCreateInstance(elementRef);
        },
        show: (elementRef) => {
            bootstrap?.Dropdown?.getOrCreateInstance(elementRef)?.show();
        },
        hide: (elementRef) => {
            bootstrap?.Dropdown?.getOrCreateInstance(elementRef)?.hide();
        },
        dispose: (elementRef) => {
            bootstrap?.Dropdown?.getOrCreateInstance(elementRef)?.dispose();
        },
        focusListItem: (ul, key, startIndex) => {
            if (!ul || startIndex < -1) return 0;

            let childNodes = ul.getElementsByTagName('LI');

            if (!childNodes || childNodes.length === 0) return 0;

            if (startIndex === undefined || startIndex === null)
                startIndex = -1;

            let nextSelectedIndex = startIndex;

            if (key === "ArrowDown") { // ARROWDOWN
                if (nextSelectedIndex < childNodes.length - 1)
                    nextSelectedIndex++;
            }
            else if (key === "ArrowUp") { // ARROWUP
                if (nextSelectedIndex > 0 && nextSelectedIndex <= childNodes.length - 1)
                    nextSelectedIndex--;
            }
            else if (key === "Home") { // HOME
                nextSelectedIndex = 0;
            }
            else if (key === "End") { // END
                nextSelectedIndex = childNodes.length - 1;
            }
            else
                return 0;

            // reset li element focus
            let highlighted = ul.querySelectorAll('.dropdown-item-highlight');
            if (highlighted.length) {
                for (let i = 0; i < highlighted.length; i++) {
                    highlighted[i].classList.remove('dropdown-item-highlight');
                }
            }

            // focus on the next li element
            if (nextSelectedIndex >= 0 && nextSelectedIndex <= childNodes.length - 1) {
                childNodes[nextSelectedIndex].classList.add('dropdown-item-highlight');
                ul.scrollTop = childNodes[nextSelectedIndex].offsetTop;
            }

            return nextSelectedIndex;
        }
    }
}