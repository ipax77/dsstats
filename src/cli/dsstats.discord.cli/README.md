# Direct Strike patch-note CLI

This CLI initializes and updates the Direct Strike patch-note database. It is a temporary
collection/import surface until collection and delivery move into dedicated server services.

The database stores one row per change. This supports chronological keyset pagination,
commander filters (for example, every Karax change), and MySQL full-text searches for unit
names without maintaining a unit-name parser.

## Commands

Run from this project directory:

```powershell
dotnet run -- init
dotnet run -- add
```

Or run from the repository root:

```powershell
dotnet run --project src\cli\dsstats.discord.cli -- init
dotnet run --project src\cli\dsstats.discord.cli -- add
```

### `init`

Synchronizes historical sources into `PatchNotes`:

- `C:\data\ds\discord\patchnotes\dsstats_DsUpdates.sql`
- `C:\data\ds\discord\patchnotes\manual.txt`

The import is idempotent. It adds new legacy/manual rows, updates changed rows, and removes
legacy/manual rows no longer present in the source files. Discord rows are not removed by
`init`.

Override either input when needed:

```powershell
dotnet run -- init --sql C:\path\DsUpdates.sql --manual C:\path\manual.txt
```

### `add`

Reads messages newer than the cursor in `PatchNoteSyncStates`, splits patch-note sections into
individual changes, inserts new rows, and advances the cursor atomically.

Only followed-channel webhook messages are accepted. Ordinary messages posted directly in the
destination channel are ignored. This excludes local test/chat messages while retaining posts
followed from the official Direct Strike patch-notes channel.

## Development and production

Development is the default:

- Discord settings come from `C:\data\localserverconfig.json`.
- The database connection comes from
  `src/server/dsstats.api/appsettings.Development.json`.
- Development database connections must target `localhost`, `127.0.0.1`, or `::1`.
- The development appsettings path is discovered automatically when running anywhere inside
  the repository.

Use production only with the explicit `--prod` flag:

```powershell
dotnet run -- init --prod
dotnet run -- add --prod
```

In production mode, the database connection is `dsstats:ConnectionString` from the file passed
through `--config` (default: `C:\data\localserverconfig.json`). `--prod` cannot be combined with
`--db-config`.

To use another development configuration:

```powershell
dotnet run -- init --db-config C:\path\appsettings.Development.json
```

## Migration ownership and safety

The CLI never creates or applies database migrations. Migrations belong to the API/deployment
workflow and must be applied before running `init` or `add`. If the required tables are missing,
the CLI fails instead of changing the schema.

`--prod` authorizes data synchronization only. It never authorizes schema changes.

The Discord bot token may be stored as `discord:Token` or supplied through the
`DSSTATS_DISCORD_TOKEN` environment variable. Tokens and database credentials are never printed.

## Database layout

`PatchNotes` contains:

- stable, unique `SourceKey` values for idempotency;
- source and source-message metadata;
- UTC publication timestamp;
- `Commander` using the shared `Commander` enum;
- normalized change text.

Indexes support the intended read paths:

- `(PublishedAtUtc DESC, PatchNoteId DESC)` for the full paginated list;
- `(Commander, PublishedAtUtc DESC, PatchNoteId DESC)` for commander-filtered pagination;
- a MySQL FULLTEXT index on `Content` for unit/change searches.

For example, Karax is commander value `70`. A later server query can combine that filter with a
boolean prefix search such as `+mirage*`.

`PatchNoteSyncStates` stores the Discord cursor separately so cursor advancement and inserted
changes can be committed together.

## Options

```text
--config <path>    Discord configuration; also production DB configuration with --prod
--db-config <path> Development database configuration
--sql <path>       Historical SQL backup used by init
--manual <path>    Manual additions used by init
--prod             Explicitly use the production database
--help, -h         Show CLI help
```
