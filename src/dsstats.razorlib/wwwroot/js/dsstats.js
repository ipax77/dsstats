//v1.13

const cmdrIconsMap = new Map();
let dsmodal = null;

function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time));
}

async function enableTooltips() {
    await delay(100);
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
}

function disableTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        var tooltip = bootstrap.Tooltip.getInstance(tooltipTriggerEl);
        tooltip.hide();
        tooltip.disable();
    })
}

function hideTooltip(id) {
    // bootstrap.Tooltip.getInstance(id).hide();
    let tooltip = document.getElementsByClassName("tooltip");
    tooltip[0].parentNode.removeChild(tooltip[0]);
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

    const loadPromises = cmdrs.map(cmdr => {
        return new Promise((resolve, reject) => {
            const img = new Image(xWidth, yWidth);
            img.onload = () => {
                cmdrIconsMap.set(cmdr, img);
                resolve();
            };
            img.onerror = reject;
            img.src = `_content/dsstats.razorlib/images/${cmdr}-min.png`;
        });
    });

    return Promise.all(loadPromises);
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

function setChartLabelsToLabel(chartId) {
    const chart = Chart.getChart(chartId);

    chart.options.plugins.datalabels.formatter = (val, ctx) => (ctx.chart.data.labels[ctx.dataIndex]);
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

function toggleCheckbox(id) {
    const checkbox = document.getElementById(id);

    if (checkbox !== undefined) {
        checkbox.checked = !checkbox.checked;
    }
}

function uncheckCheckboxes(startid) {
    const checkboxes = document.querySelectorAll(`[id^="${startid}"]`);

    checkboxes.forEach(checkbox => {
        checkbox.checked = false;
    });
}

function scrollToElementId(id) {
    const element = document.getElementById(id);

    if (element !== undefined) {
        element.scrollIntoView();
    }
}

function openModalById(id) {
    const modalElement = document.getElementById(id);

    if (!modalElement) {
        return;
    }

    dsmodal = new bootstrap.Modal(modalElement);
    dsmodal.show();
}

function closeModalById(id) {
    if (dsmodal !== undefined && dsmodal !== null) {
        dsmodal.hide();
        dsmodal = null;
    } else {
        const modalEl = document.getElementById(id);
        const myModal = bootstrap.Modal.getInstance(modalEl);
        if (myModal !== undefined && myModal != null) {
            myModal.hide();
            modalEl.addEventListener('hidden.bs.modal', () => {
                modal.dispose();
            }, { once: true });
        }
    }
}

function toggleButton(buttonId, elementId) {
    var button = document.getElementById(buttonId);
    var bsButton = new bootstrap.Button(button);
    bsButton.toggle();

    var targetElement = document.getElementById(elementId);
    if (targetElement) {
        var isCollapsed = targetElement.classList.contains('show');

        if (isCollapsed) {
            targetElement.classList.remove('show');
        } else {
            targetElement.classList.add('show');
        }
    }
}

function closeDropdown(dropdownId) {
    var dropdown = document.getElementById(dropdownId);
    if (dropdown) {
        var dropdownInstance = bootstrap.Dropdown.getInstance(dropdown);
        if (dropdownInstance) {
            dropdownInstance.hide();
        }
    }
}