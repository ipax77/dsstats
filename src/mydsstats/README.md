
# mydsstats

`mydsstats` is the browser client for decoding, storing, viewing, and uploading
SC2 Direct Strike replays. It is a Blazor WebAssembly PWA backed by the
`dsstats.indexedDb` Razor class library for browser storage and JavaScript
interop.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET WASM workload:

```bash
dotnet workload install wasm-tools
```

### Running The Application

```bash
cd src/mydsstats/dsstats.pwa
dotnet run
```

### Development Build

```powershell
$env:DSSTATS_PWA_ENVIRONMENT = "Development"
dotnet publish src/mydsstats/dsstats.pwa/dsstats.pwa.csproj -c Release
```

The same environment can be passed as an MSBuild property:

```bash
dotnet publish src/mydsstats/dsstats.pwa/dsstats.pwa.csproj -c Release -p:DsstatsPwaEnvironment=Development
```

## IndexedDB And Replay Sidecars

Replay JSON, list metadata, ratings, directory handles, and spawn playback
sidecars are stored in IndexedDB. The replay JSON/upload backup flow still uses
the existing gzip/pako behavior.

Spawn playback sidecars are stored separately from replay JSON. New sidecars are
stored and uploaded as `SpawnPlaybackCompression.Brotli`; existing GZip sidecars
remain valid and are exported unchanged.

The browser cannot rely on the .NET `BrotliStream` path for PWA replay viewing.
For spawn playback sidecars, the PWA uses `brotli-wasm` from the
`dsstats.indexedDb` static web assets:

- decode workers receive raw `DSPB` payloads from .NET replay parsing and
  Brotli-compress them in JavaScript before returning data to the main thread;
- single-file/non-worker saves Brotli-compress raw `DSPB` sidecars before the
  IndexedDB write transaction;
- replay playback Brotli-decompresses sidecars in JavaScript, then passes raw
  `DSPB` bytes back to C# for `SpawnPlaybackSidecarDto` parsing.

Decoded sidecars are not persisted. Shared web components use a scoped
`SpawnPlaybackSidecarCache` to keep up to 8 decoded
`SpawnPlaybackSidecarDto` instances in memory per host/session/circuit. This
avoids repeated IndexedDB/API reads and repeat Brotli/GZip decompression when
the same replay is opened in `ReplayComponent`, build hydration, or replay
modals.

## Useful Checks

Run JavaScript tests and rebuild static web assets from
`src/mydsstats/dsstats.indexedDb`:

```powershell
node .\node_modules\vitest\vitest.mjs run
node .\node_modules\webpack\bin\webpack.js
```

Build the PWA from the repository root:

```powershell
dotnet build src/mydsstats/dsstats.pwa/dsstats.pwa.csproj
```
