# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

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
- **`dsstats.dbServices`** — Scoped services implementing shared interfaces via `DsstatsContext` directly. Used in `dsstats.api`.
- **`dsstats.apiServices`** — HTTP client wrappers implementing the **same interfaces** as `dsstats.dbServices`, forwarding calls to the API. Used in `dsstats.web`.
- **`dsstats.ratings`** — Elo-style rating calculation engine via `IRatingService` (singleton).
- **`sc2arcade.crawler`** — Background service for crawling SC2 Arcade match data.
- **`dsstats.migrations.mysql/postgresql/sqlite`** — Provider-specific EF Core migration assemblies.

### Common (`src/common/`)
- **`dsstats.shared`** — Shared DTOs, enums (`Commander`, `RatingType`, `GameMode`, `StatsType`, etc.), and all service interfaces (`IStatsService`, `IReplayRepository`, `IPlayerService`, etc.).
- **`dsstats.weblib`** — Shared Blazor component library (razor components, Chart.js integration via `pax.BlazorChartJs`).
- **`dsstats.parser`** — SC2 replay file parser producing `ReplayDto`.

### Tests (`src/tests/dsstats.tests/`)
Uses **MSTest** with **Moq**. Tests that require a database use **SQLite in-memory** (via `dsstats.migrations.sqlite`):

```csharp
var connection = new SqliteConnection("Filename=:memory:");
connection.Open();
services.AddDbContext<DsstatsContext>(o => o.UseSqlite(connection, options =>
    options.MigrationsAssembly("dsstats.migrations.sqlite")));
```

Test data (actual `.SC2Replay` files and `.json` metadata) is in `src/tests/dsstats.tests/testdata/`.

## Key Conventions

### Dual service registration pattern
Interfaces in `dsstats.shared/Interfaces/` are implemented twice — once in `dsstats.dbServices` (direct EF Core) and once in `dsstats.apiServices` (HTTP proxies). When adding a new data access feature, implement it in **both**.

### Partial classes for large services
Large services are split into domain-focused partial classes:
- `PlayerService.cs` + `PlayerService.Player.cs`, `PlayerService.CmdrStrength.cs`, `PlayerService.Distrubution.cs`
- `RatingService.cs` + `RatingService.Calc.cs`, `RatingService.Pre.cs`, `RatingService.Arcade.cs`, `RatingService.Csv.cs`
- `ImportService.cs` + `ImportService.Candidates.cs`, `ImportService.Duplicates.cs`, `ImportService.Arcade.cs`
- `ReplayRepository.cs` + `ReplayRepository.Arcade.cs`, `ReplayRepository.Ratings.cs`

### Stats provider pattern
New stat types require:
1. A class inheriting `StatsProviderBase<T>` in `dsstats.dbServices/Stats/`
2. Registration as `IStatsProvider` in `Program.cs`
3. A corresponding `StatsType` enum value in `dsstats.shared/Enums.cs`

`StatsService` dispatches to the appropriate provider by `StatsType` at runtime.

### Database
- Primary: **MySQL 8.4** in production. Connection string config key: `dsstats:ConnectionString`.
- `DsstatsContext` is always injected via DI (scoped). `StagingDsstatsContext` maps rating tables to `*_tmp` variants — use during rating recalculation jobs.
- `DsstatsContext.Week(DateTime)` is a mapped MySQL `WEEK(..., 3)` DB function; it **cannot** be called client-side.
- All EF queries use `ToListAsync`/`FirstOrDefaultAsync` with a `CancellationToken`.
- Migrations are in separate assemblies per provider.

### Upload flow
Uploads arrive at `UploadController` and are pushed onto a `Channel<UploadJob>` or `Channel<ReplayUploadJob>`. Background `IHostedService` implementations drain the channels. `UploadService` is a **singleton**.

### Other conventions
- **API routes**: `[Route("api10/[controller]")]`. Legacy alias `api8/v1/[controller]` exists on `UploadController` for backward compatibility.
- **Authentication**: `AuthenticationFilterAttribute` checks the `Authorization` header for a static key (singleton).
- **Caching**: `IMemoryCache` for expensive stats queries (cache keys via `StatsRequest.GetMemKey()`, 3-hour TTL).
- **Rate limiting**: Fixed-window policy (`"fixed"`: 4 requests / 12 seconds, queue 2) on replay upload endpoints.
- **Production config**: Loads `/data/localserverconfig.json`. `TimedHostedService` only runs in production.
- **SignalR hubs**: `/hubs/upload` (`UploadHub`) and `/hubs/pickban` (`PickBanHub`).
