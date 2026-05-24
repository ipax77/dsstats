function getRatingUrl(apiBaseAddress, replayHash) {
    const base = apiBaseAddress ? apiBaseAddress.replace(/\/$/, "") : "";
    return `${base}/api10/Replays/${encodeURIComponent(replayHash)}/user-rating`;
}

async function readRatingResponse(response) {
    const text = await response.text();
    const rating = text ? JSON.parse(text) : null;
    return {
        ok: response.ok,
        status: response.status,
        rating
    };
}

export async function getReplayUserRating(apiBaseAddress, replayHash) {
    const response = await fetch(getRatingUrl(apiBaseAddress, replayHash), {
        method: "GET",
        headers: { "Accept": "application/json" },
        credentials: "omit"
    });

    return readRatingResponse(response);
}

export async function submitReplayUserRating(apiBaseAddress, replayHash, score) {
    const response = await fetch(getRatingUrl(apiBaseAddress, replayHash), {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        credentials: "omit",
        body: JSON.stringify({ score })
    });

    return readRatingResponse(response);
}
