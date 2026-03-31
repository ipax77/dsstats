# dsstats Copilot Instructions

Fan project for analyzing StarCraft II [Direct Strike](https://www.patreon.com/Tya) replays. The backend serves data to the website [dsstats.pax77.org](https://dsstats.pax77.org) and a .NET MAUI desktop client distributed via the Microsoft Store.

## Build & Test

```bash
# Build the server solution
dotnet build src/server/server.sln

# Run all tests
dotnet test src/tests/dsstats.tests/dsstats.tests.sln

# Run a single test by name
dotnet test src/tests/dsstats.tests/dsstats.tests.sln --filter "FullyQualifiedName~<TestMethodName>"

# Run the API (requires MySQL - see docker/ for local setup)
cd src/server/dsstats.api && dotnet run

# Run the web frontend
cd src/server/dsstats.web && dotnet run

# Start MySQL via Docker (WSL/Linux)
cd src/server/docker && docker-compose up -d
```

## Architecture

The solution targets **.NET 10** with `LangVersion=latest` and nullable reference types enabled throughout.

### Server (`src/server/`)
- **`dsstats.api`** — ASP.NET Core Web API. All routes prefixed `api10/`. Handles replay uploads (two upload paths: legacy `UploadDto` and newer `UploadRequestDto`), stats queries, player data, and builds.
- **`dsstats.web`** — Blazor Server app (interactive server render mode). Calls `dsstats.api` over HTTP; does **not** access the database directly.
- **`dsstats.db`** — EF Core `DsstatsContext` + all entity models. Also contains `StagingDsstatsContext` which redirects `PlayerRatings`, `ReplayRatings`, and `ReplayPlayerRatings` to `*_tmp` tables for bulk rating recalculations.
- **`dsstats.dbServices`** — Scoped services that implement shared interfaces using `DsstatsContext` directly. Used in `dsstats.api`.
- **`dsstats.apiServices`** — HTTP client wrappers that implement the **same interfaces** as `dsstats.dbServices`, but forward calls to the API. Used in `dsstats.web`.
- **`dsstats.ratings`** — Elo-style rating calculation. Runs via `IRatingService` (singleton).
- **`sc2arcade.crawler`** — Background service for crawling SC2 Arcade match data.
- **`dsstats.migrations.mysql/postgresql/sqlite`** — Provider-specific EF Core migration assemblies. Migrations target is selected per-provider at `AddDbConfig()`.

### Common (`src/common/`)
- **`dsstats.shared`** — Shared DTOs, enums (`Commander`, `RatingType`, `GameMode`, `StatsType`, etc.), and all service interfaces (`IStatsService`, `IReplayRepository`, `IPlayerService`, etc.).
- **`dsstats.weblib`** — Shared Blazor component library (razor components, Chart.js integration via `pax.BlazorChartJs`).
- **`dsstats.parser`** — SC2 replay file parser producing `ReplayDto`.

### Tests (`src/tests/dsstats.tests/`)
Uses **MSTest** (`Microsoft.VisualStudio.TestTools.UnitTesting`) with **Moq** for mocking. Test files cover parsing, ratings, hash, arcade matching, and tournament info. Tests that require a database use **SQLite in-memory** (via `dsstats.migrations.sqlite`):

```csharp
var connection = new SqliteConnection("Filename=:memory:");
connection.Open();
services.AddDbContext<DsstatsContext>(o => o.UseSqlite(connection, options =>
    options.MigrationsAssembly("dsstats.migrations.sqlite")));
```

## Key Conventions

### Dual service registration pattern
`dsstats.shared/Interfaces/` defines interfaces like `IStatsService`, `IReplayRepository`, etc. These are implemented twice:
- In `dsstats.dbServices` — direct EF Core queries (used by `dsstats.api`)
- In `dsstats.apiServices` — HTTP client proxies (used by `dsstats.web`)

When adding a new data access feature, implement it in both places.

### Partial classes for large services
Large service files are split into domain-focused partial classes rather than a single large file. Follow this when a service grows:
- `PlayerService.cs` + `PlayerService.Player.cs`, `PlayerService.CmdrStrength.cs`, `PlayerService.Distrubution.cs`
- `ImportService.cs` + `ImportService.Candidates.cs`, `ImportService.Duplicates.cs`, `ImportService.Arcade.cs`
- `ReplayRepository.cs` + `ReplayRepository.Arcade.cs`, `ReplayRepository.Ratings.cs`
- `RatingService.cs` + `RatingService.Calc.cs`, `RatingService.Pre.cs`, `RatingService.Arcade.cs`, `RatingService.Csv.cs`

### Stats provider pattern
New stat types require:
1. A class inheriting `StatsProviderBase<T>` in `dsstats.dbServices/Stats/`
2. Registering it in `Program.cs` as `IStatsProvider`
3. Adding the corresponding `StatsType` enum value to `dsstats.shared/Enums.cs`

`StatsService` dispatches by `StatsType` to the appropriate provider at runtime.

### Database
- Primary database is **MySQL 8.4** in production. Connection string config key: `dsstats:ConnectionString`.
- EF Core context is `DsstatsContext`; always inject via DI (scoped).
- `StagingDsstatsContext` (also scoped) maps rating tables to `_tmp` variants — use this during rating recalculation jobs.
- `DsstatsContext.Week(DateTime)` is a mapped MySQL `WEEK(..., 3)` DB function; it cannot be called client-side.
- All EF queries should use `ToListAsync`/`FirstOrDefaultAsync` with a `CancellationToken`.
- Migrations are in separate assemblies per provider (`dsstats.migrations.mysql`, etc.).

### Upload flow
Uploads arrive at `UploadController` and are pushed onto a `Channel<UploadJob>` or `Channel<ReplayUploadJob>`. Background `IHostedService` implementations (`UploadProcessingService`, `ReplayProcessingService`) drain the channels. The `UploadService` is a **singleton**.

### Authentication
Upload endpoints are protected by `AuthenticationFilterAttribute` (applied via `[ServiceFilter]`). It checks the `Authorization` header for a static key. This is registered as a singleton.

### API route prefix
All API controllers use `[Route("api10/[controller]")]`. The legacy alias `api8/v1/[controller]` exists on `UploadController` for backward compatibility.

### Caching
`IMemoryCache` is used for expensive stats queries (cache keys built from request parameters via `StatsRequest.GetMemKey()`). Default TTL is 3 hours.

### Rate limiting
A fixed-window rate limiter (`"fixed"` policy: 4 requests / 12 seconds, queue limit 2) is applied to the replay upload endpoint.

### Production config
Production loads extra config from `/data/localserverconfig.json`. `TimedHostedService` (scheduled background jobs) only runs in production.

### SignalR hubs
- `/hubs/upload` — `UploadHub` for upload progress
- `/hubs/pickban` — `PickBanHub` for pick/ban sessions

### Response compression
Both request decompression and response compression (`EnableForHttps = true`) are enabled on the API.
