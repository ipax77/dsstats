function getTooltipPoint(context) {
    const dataIndex = context?.dataIndex;
    const points = context?.dataset?.tooltipPoints ?? context?.dataset?.TooltipPoints;

    if (!Array.isArray(points) || dataIndex == null) {
        return undefined;
    }

    return points[dataIndex];
}

function getNumber(value, fallback = 0) {
    const number = Number(value ?? fallback);
    return Number.isFinite(number) ? number : fallback;
}

export const chartJsCallbacks = {
    buildUnitTooltipLabel(context) {
        return context?.raw?.label ?? context?.raw?.Label ?? "";
    },

    synergyTooltipLabel(context) {
        const point = getTooltipPoint(context);
        const normalized = getNumber(point?.normalized ?? point?.Normalized ?? context?.parsed?.r ?? context?.raw).toFixed(2);
        const games = getNumber(point?.games ?? point?.Games, 0);
        const label = context?.dataset?.label ?? "Unknown";

        if (!games) {
            return `${label}: n=${normalized} (no games)`;
        }

        const teammate = point?.teammate ?? point?.Teammate ?? "";
        const avgGain = getNumber(point?.avgGain ?? point?.AvgGain).toFixed(2);
        const winratePercent = (getNumber(point?.winrate ?? point?.Winrate) * 100).toFixed(1);

        return `${label} + ${teammate}: n=${normalized}, AvgGain=${avgGain}, Winrate=${winratePercent}%, Games=${games}`;
    },

    timelineTooltipLabel(context) {
        const point = getTooltipPoint(context);
        const label = context?.dataset?.label ?? "Unknown";
        const bucket = context?.label ?? "";
        const games = getNumber(point?.games ?? point?.Games, 0);
        const avgGain = getNumber(point?.avgGain ?? point?.AvgGain).toFixed(2);
        const winratePercent = (getNumber(point?.winrate ?? point?.Winrate) * 100).toFixed(1);

        if (!games) {
            return `${label} (${bucket}): no games`;
        }

        return `${label} (${bucket}) - AvgGain: ${avgGain}, Winrate: ${winratePercent}%, Games: ${games}`;
    }
};
