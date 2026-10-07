# SC2Arcade diagnostic backfill

This standalone .NET 10 command archives SC2Arcade history without connecting to a database.
The separate `import` command can compare or import a completed archive into MySQL.
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
through collection, import and review. Account for newer data arriving during collection.

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
The complete cutoff-crossing page is retained. The import command filters dates and
deduplicates against production. Crawl/status do not perform importing, matching, or rating changes.

## Import a completed archive

The configuration file must contain `dsstats:ConnectionString` for the intended target.
An optional `dsstats:ServerVersion` selects its MySQL version (default 8.4).
Credentials are read from the file and never printed or copied into the archive.

```text
dotnet run --project src/tools/sc2arcade.backfill -- import --archive artifacts/sc2arcade-backfill --config C:\data\localserverconfig.json
dotnet run --project src/tools/sc2arcade.backfill -- import --archive artifacts/sc2arcade-backfill --config C:\data\localserverconfig.json --execute --finalize
```

Without `--execute`, import is a read-only comparison of archived replay keys with the
target database. Preview counts distinguish existing from missing eligible replays.
The archive is fully validated first and locked against concurrent collection/import.

Execution imports at most 500 eligible replays per transaction, creating only missing
players and preserving existing player names. Players and replay rows commit together.
The unique `(RegionId, BnetBucketId, BnetRecordId)` database key is the durable resume
checkpoint. Rerun the same command after interruption; committed replays are recognized
and skipped. A concurrent conflicting insert rolls back the batch and stops the command;
rerunning rechecks the database rather than trusting stale in-memory keys. No schema
migrations, deletes, or updates to existing replays are performed.

An import receipt in `imports/<target hash>/session.json` preserves the original import
start across retries. Keep this receipt and use the same target configuration on resume.
`--finalize` re-matches newly imported arcade replays with dsstats replays and calls the
existing `BatchImportCombinedReplays` procedure. Both steps can be rerun after failure.
It does **not** perform the full historical rating rebuild: run the production server's
full rating job, or let the next nightly job do it. Hourly incremental rating updates
alone do not incorporate all historical changes.

After execution, rerun the preview and verify `missing=0` before considering collection
fully imported. The June–August historical gap cannot be filled by this archive.

## Production pacing and release

The production crawler now shares the backfill's three-second minimum and header-aware
request policy, including region transitions. Requests remain sequential; it stops all
regions at the first HTTP failure or invalid response, with no automatic retry. Selected
headers and the next permitted request time are logged. The shared deadline also applies
to subsequent jobs in the same process; unlike the archive journal it is not persisted
across production restarts. Keep server clocks synchronized.

The nightly cutoff is UTC midnight minus five days. Following the usual merge and server
release tag, manually deploy the API on the server and restore `SC2Arcade:CrawlEnabled=true`
(or remove the override). Verify NA and EU completion and absence of HTTP-failure logs
on the next run. Slower pacing reduces request pressure; it cannot guarantee no future 403.

Do not invoke `eng/New-ReleaseArtifacts.ps1` with its default output directory while this
archive lives under `artifacts/`: that script deletes its output directory first. Use a
dedicated output such as `-OutputPath artifacts/releases/<version>` to preserve the archive.
