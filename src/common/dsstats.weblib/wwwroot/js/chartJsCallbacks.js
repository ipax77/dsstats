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
    signedComparisonValue(value) {
        const number = getNumber(value);
        return `${number >= 0 ? "+" : ""}${number.toFixed(2)}`;
    },

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
    },

    winrateComparisonTooltipLabel(context) {
        const point = getTooltipPoint(context);
        const label = context?.dataset?.label ?? "Period";
        const metric = point?.metric ?? point?.Metric ?? "Metric";
        const before = getNumber(point?.before ?? point?.Before);
        const after = getNumber(point?.after ?? point?.After);
        const difference = getNumber(point?.difference ?? point?.Difference);
        const beforeReplays = getNumber(point?.beforeReplays ?? point?.BeforeReplays);
        const afterReplays = getNumber(point?.afterReplays ?? point?.AfterReplays);
        const value = label === "Before" ? before : label === "After" ? after : difference;
        const suffix = metric === "Unadjusted win rate" ? "%" : "";

        if (label === "Change") {
            const confidenceLow = point?.confidenceLow ?? point?.ConfidenceLow;
            const confidenceHigh = point?.confidenceHigh ?? point?.ConfidenceHigh;
            const confidenceStatus = point?.confidenceStatus ?? point?.ConfidenceStatus ?? "";
            const interval = confidenceLow == null || confidenceHigh == null
                ? "95% CI unavailable"
                : `95% CI [${getNumber(confidenceLow).toFixed(2)}, ${getNumber(confidenceHigh).toFixed(2)}]`;

            return `Change: ${difference >= 0 ? "+" : ""}${difference.toFixed(2)}; Before ${before.toFixed(2)} → After ${after.toFixed(2)}; ${interval}; ${confidenceStatus}; replays ${beforeReplays} → ${afterReplays}`;
        }

        return `${label}: ${value.toFixed(2)}${suffix}; Δ ${difference >= 0 ? "+" : ""}${difference.toFixed(2)}${suffix}; replays ${beforeReplays} → ${afterReplays}`;
    }
};
