# SC2Arcade diagnostic backfill

This standalone .NET 10 command archives SC2Arcade history without connecting to a database.
It stops **all requests at the first 403** (also on other HTTP failures, network failures,
invalid responses, or repeated cursors). There are no automatic retries. Inspect the
diagnostics, then explicitly run `resume` to try the outstanding request again.

## Coordinate with production

Deploy the accompanying production switch and set `SC2Arcade:CrawlEnabled` to `false`
in the production configuration (or environment variable `SC2Arcade__CrawlEnabled=false`).
This skips crawling only; duplicate checks, player maintenance, ratings and unrelated
scheduled work continue. The default is `true`. Verify the production log says
“SC2Arcade crawling disabled by configuration; continuing maintenance.” before the
local attempt. A crawl already in progress must finish or be stopped first.

The console tool cannot disable production remotely. Keep production crawling disabled
through collection and review. Importing the archive and returning to the five-day
window are separate follow-up steps; account for newer data arriving during collection.

## Commands

From the repository root (PowerShell or a shell with equivalent quoting):

```text
dotnet run --project src/tools/sc2arcade.backfill -- crawl --archive artifacts/sc2arcade-backfill --cutoff 2026-05-31T03:31:59Z
dotnet run --project src/tools/sc2arcade.backfill -- status --archive artifacts/sc2arcade-backfill
dotnet run --project src/tools/sc2arcade.backfill -- resume --archive artifacts/sc2arcade-backfill
```

`crawl` requires a new archive and an explicit UTC cutoff. `resume` uses the stored
configuration; an optional `--cutoff` must match. There is no deadline bypass option.
`status` only reads local files and may be used while a writer is active.
Use Ctrl+C to stop; committed progress survives both cancellation and process termination.

Exit codes: `0` means collection finished or status succeeded, `2` means a diagnostic
stop requiring inspection, `130` means cancellation, and `1` means a command/archive error.
Collection finishing with `HistoryExhaustedBeforeCutoff` is explicitly limited coverage,
not evidence that the missing history exists upstream.

## Archive and diagnostics

Numbered JSON records form an immutable hash chain. A configuration record pins the run
start, cutoff, NA/EU maps, profile, page size, and API base. Each request has a `Started`
record, an early `Headers` record, and a `Response` record. A successful response contains
the original JSON body as a string, its SHA-256, and both cursors. Decode the envelope's
`Payload` JSON to inspect it. A committed page **is** the checkpoint: no separate cursor
file can advance ahead of saved data. Resume streams the journal to rebuild progress.

Records are flushed and atomically renamed before advancing. A crash before the rename
can repeat that one outstanding request; orphan `.tmp` files are ignored. An interrupted
request with no received headers imposes a conservative 60-second wait from request start.
Saved response headers preserve server deadlines even if body reading was interrupted.
Concurrent writers are excluded by an OS file lock. Do not edit, delete, reorder, or copy
individual committed records; preserve the full directory. Archives create their own
ignore-all `.gitignore`, and the example `artifacts/` directory is already ignored.

The status report compares the last response with the preceding successfully committed
page: timestamps, request duration to headers, HTTP status, rate-limit headers,
`Retry-After`, `Date`, content type, `cf-ray`, and `cf-mitigated`. Failure bodies are sampled
up to 4,096 characters. Cookies and authorization headers are not captured. Raw response
bodies may contain player names or diagnostic content; treat the archive as local data.

Minimum spacing is three seconds after response headers, across region transitions too.
`Retry-After` seconds and HTTP dates are honored, including server clock skew. The
`x-ratelimit-reset` interpretation remains relative seconds, matching the existing crawler
and observed small values; raw values are retained for review. Near-exhausted quota waits
for reset. Missing/malformed quota information on successful responses falls back to
60 seconds. Invalid `Retry-After` also falls back to 60 seconds. A 403 does not by itself
prove rate limiting or schedule an hours-long cooldown. Explicit resume still respects
the recorded deadline.

Per-region reports include pages, date range, daily lobby counts, conversion eligibility,
rejection reasons, and duplicate eligible replay keys within the archive. Eligibility uses
the same converter as live import, including its existing acceptance of unknown winner
teams (counted separately). It does not imply a new production row or rating eligibility.
The complete cutoff-crossing page is retained. A later importer must filter dates and
deduplicate against production. No importing, matching, or rating changes are performed.
