# Deployment runbook

Production consumes immutable artifacts from the central `ipax77/dsstats`
release workflow. A production host never pulls `main`, runs `dotnet publish`,
or uses a .NET SDK image. Apache terminates TLS; the API and web containers
listen on loopback HTTP only.

Concrete Compose files, Apache virtual hosts, secrets, and deployment scripts
are private host configuration under `/opt/dsstats-server` (the maintained
Windows copy is `C:\data\ds\serversetup`).

## Release artifacts

| Product | Tag | Artifacts |
| --- | --- | --- |
| Parser | `parser/v3.1.0` | NuGet package |
| mydsstats | `mydsstats/v3.1.0` | `mydsstats-v3.1.0.zip` |
| Service | `service/v3.1.0` | MSI and `latest.yml` |
| Server | `server/v3.1.0` | API/web ZIPs and `dsstats-migrations-v3.1.0` |
| MAUI | `maui/v3.1.0` | Store package |

Every set contains `release-manifest.json` and `SHA256SUMS`. The manifest
records component, version, source commit, and whether the source tree was
dirty. Server manifests also record the latest included EF `MigrationId`. CI
refuses to produce release artifacts from a dirty checkout. The Linux-x64 EF
migration bundle is self-contained, so neither the host nor runtime image needs
the SDK or `dotnet-ef` tool.

## Create and approve a release

1. Merge only reviewed, green changes to `main`.
2. Confirm `Directory.Build.props` contains the intended component version.
3. Create and push an annotated component tag:

   ```bash
   git switch main
   git pull --ff-only origin main
   git tag -a server/v3.1.0 -m "server 3.1.0"
   git push origin server/v3.1.0
   ```

4. Review the draft central release, manifest, filenames, and checksums.
5. Publish the approved draft. Never move or recreate an immutable tag; issue
   a new patch version instead.

## Host layout and prerequisites

The optimized host layout is:

```text
/opt/dsstats-server/              private Compose and deployment scripts
/opt/dsstats/
  releases/server/<version>/      immutable API/web/migration release sets
  api-current -> .../api          active API
  web-current -> .../web          active web
/srv/dsstats/api/                 API configuration and replay data
/var/lib/dsstats/                 locks, audit log, Data Protection, shared data
/var/www/releases/mydsstats/      immutable PWA releases
/var/www/mydsstats -> .../wwwroot active PWA
```

Required host commands are Bash, Docker Compose, `curl`, `flock`, `unzip`,
`sha256sum`, and `realpath`. Release-download mode additionally requires an
authenticated `gh` CLI. Application containers use
`mcr.microsoft.com/dotnet/aspnet:10.0`; no SDK is installed on the host.

Copy the private setup and create host-local environment files:

```bash
sudo install -d -o root -g root /opt/dsstats-server
sudo cp -a /path/to/serversetup/. /opt/dsstats-server/
sudo cp /opt/dsstats-server/docker/apps/.env.example \
  /opt/dsstats-server/docker/apps/.env
sudo cp /opt/dsstats-server/docker/mysql/.env.example \
  /opt/dsstats-server/docker/mysql/.env
sudo cp /opt/dsstats-server/deploy/deploy.env.example \
  /opt/dsstats-server/deploy/deploy.env
sudo chmod 0755 /opt/dsstats-server/deploy/*.sh
```

Store MySQL passwords as root-owned files outside the setup directory and use
the same application password in `/srv/dsstats/api/localserverconfig.json`:

```bash
sudo install -d -m 0700 /etc/dsstats/secrets
sudo install -m 0600 /secure/source/mysql_app_password \
  /etc/dsstats/secrets/mysql_app_password
sudo install -m 0600 /secure/source/mysql_root_password \
  /etc/dsstats/secrets/mysql_root_password
```

The API configuration must contain production connection strings using
`mysql8:3306`, `dsstats:ServerVersion` matching the installed server,
`Database:MigrateOnStartup` set to `false`, and the existing
authentication/storage settings. The one-shot migration container reads this
same protected configuration file; connection credentials are never passed on
its command line. Make the API data, shared CSV, and Data Protection
directories writable by the `app` UID from the approved runtime image;
releases themselves remain read-only.

Create the shared network once:

```bash
docker network inspect dsstats >/dev/null 2>&1 || docker network create dsstats
```

## Preserve and pin the current MySQL installation

Do not combine this deployment refactor with a MySQL upgrade. While the current
database is running, record its exact image digest, version, and Docker network
gateway:

```bash
sudo /opt/dsstats-server/deploy/inspect-production.sh mysql8 dsstats dsstats10
```

Put the reported `MYSQL_IMAGE` digest in `docker/mysql/.env`; put the gateway
and `DSS_MYSQL_SERVER_VERSION` in `docker/apps/.env`; and put the reported
baseline in `deploy/deploy.env`. Use the same database version for
`dsstats:ServerVersion`. Point the production Compose override at the existing
MySQL data and log directories. Validate the rendered model before stopping
anything:

```bash
cd /opt/dsstats-server/docker/mysql
docker compose --env-file .env \
  -f compose.yaml -f compose.production.yaml config --quiet
```

Back up MySQL through the established production backup system. In its own
maintenance window, stop the old MySQL Compose service and start the new
definition against the unchanged data directory and pinned image:

```bash
docker compose --env-file .env \
  -f compose.yaml -f compose.production.yaml up -d mysql8
docker compose --env-file .env \
  -f compose.yaml -f compose.production.yaml ps
```

Wait for `healthy`, verify the server version, and test an application login.
Never run `docker compose down -v`; the named local volume and production data
directory are intentionally persistent.

## Configure optional database backups

Normal deployments do not create a full database backup. Pass `--backup` for a
release whose migration risk or recovery requirements justify the cost.
`DSS_BACKUP_HOOK` in `deploy/deploy.env` then names an executable. It receives
the target server version as its only argument, must perform or verify the
production backup, exit nonzero on failure, and print one non-empty backup
identifier on stdout. Diagnostics belong on stderr.

Example contract:

```bash
#!/usr/bin/env bash
set -Eeuo pipefail
version=$1
backup_id=$(/usr/local/sbin/run-dsstats-backup "$version")
/usr/local/sbin/verify-dsstats-backup "$backup_id"
printf '%s\n' "$backup_id"
```

With `--backup`, deployment stops before migration or activation if this hook
fails. Without it, the audit record contains `backup=skipped`.

## EF migration and downgrade policy

Production API startup never applies migrations. Each checked server artifact
contains the EF migration bundle built from the same commit as the API. The
deployer stops API, runs that bundle to the manifest's latest migration, then
activates API and web. It records the migration before and after deployment in
`deployments-v2.tsv`.

For the first conversion, set `DSS_DATABASE_BASELINE_MIGRATION` to the last row
in `__EFMigrationsHistory`. Use `0` only for a genuinely empty database. Later
deployments obtain the boundary from the active release manifest.

EF downgrade is deliberately opt-in. A migration's `Down()` method may drop
tables or columns, truncate values when narrowing a column, or fail partway
because MySQL DDL is not generally transactional. Inspect every migration
between the current and target IDs before approving a downgrade. A logical dump
or storage snapshot remains the only recovery path for deleted row data and
partially applied DDL.

## One-time application-container cutover

First update the Apache files and run `apachectl configtest`. The revised
virtual hosts still work with the old containers because those already expose
HTTP ports 6976 and 6876; only the HTTPS Kestrel/WebSocket upstreams are removed.
Reload Apache after a successful config test.

Validate the application Compose file and pull the approved runtime explicitly:

```bash
cd /opt/dsstats-server/docker/apps
docker compose --env-file .env config --quiet
docker pull "$(sed -n 's/^DOTNET_ASPNET_IMAGE=//p' .env)"
```

The first cutover is a maintenance operation because no prior release symlinks
exist. Preserve the old Compose directories and loose `wwwbin` trees. Stop only
the old API/web containers, then run the first artifact deployment. If it
fails, the new containers stop; restart the preserved old Compose services.
Later deployments have automatic symlink rollback.

## Deploy or roll back the server

API and web always use the same `server/v...` release. Deploy a published
release as root (or as a deployment user with access to all configured paths):

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh deploy \
  --version 3.1.0 --source release
```

Request the configured full backup only when appropriate:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh deploy \
  --version 3.1.0 --source release --backup
```

For a pre-release rehearsal with locally generated artifacts:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh deploy \
  --version 3.1.0 --source local \
  --artifact-dir /path/to/artifacts
```

The script verifies the exact file set, manifest, and SHA-256 values; installs
an immutable version; applies its migration bundle; switches/recreates API;
runs liveness and database-backed smoke tests; then switches/recreates web. It
never pulls a runtime image. A failed activation restores previous application
links and containers. By default it leaves the newly applied schema in place.

For a migration whose reviewed `Down()` methods are safe enough, the operator
may pre-authorize schema downgrade when activation fails:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh deploy \
  --version 3.1.0 --source release \
  --database-down-on-failure --accept-data-loss
```

Roll back application files only:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh rollback --version 3.0.9
```

Use application-only rollback when the old application supports the applied
schema. To explicitly run EF `Down()` methods to the target release's recorded
migration, stop both applications and downgrade before activation:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh rollback \
  --version 3.0.9 --database-down --accept-data-loss
```

Migration-aware releases take the target from their manifest. For a legacy
release without `MigrationId`, also pass the reviewed boundary explicitly:

```bash
sudo /opt/dsstats-server/deploy/deploy-server.sh rollback \
  --version 3.0.9 --database-down --accept-data-loss \
  --target-migration 20260724071102_SpecialUnits
```

Add `--backup` to either rollback command when a full recovery point is worth
the time and storage. Full database restore is never automatic. Migration-aware
deployment audit records are appended to
`/var/lib/dsstats/deployments-v2.tsv`.

## Deploy or roll back mydsstats

The PWA deployment is also artifact-only:

```bash
sudo /opt/dsstats-server/deploy/deploy-mydsstats.sh deploy \
  --version 3.1.0 --source release

sudo /opt/dsstats-server/deploy/deploy-mydsstats.sh rollback \
  --version 3.0.9
```

The deployer extracts to `/var/www/releases/mydsstats/<version>`, validates
`wwwroot/index.html` and `wwwroot/_framework`, atomically switches
`/var/www/mydsstats`, and restores the previous link if the public smoke test
fails. It does not run Git, build, or erase the live directory.

## WSL 2 rehearsal

Keep container state on the native WSL filesystem. The maintained copy may
remain on Windows, but the bootstrap copies it before running containers.

Generate a local server artifact set from the repository on Windows:

```powershell
./eng/New-ReleaseArtifacts.ps1 `
  -Component server -Version 3.1.0 -OutputPath artifacts/server-local
```

From WSL, install the setup, create local secrets/configuration, start a healthy
MySQL instance, and deploy through the production activation path:

```bash
bash /mnt/c/data/ds/serversetup/deploy/bootstrap-wsl.sh \
  --artifact-dir /mnt/c/Users/pax77/source/repos/dsstats/artifacts/server-local \
  --version 3.1.0
```

If a healthy local MySQL container already owns the `mysql8` network alias and
port 9801, preserve its native-Linux data and reuse it during the rehearsal:

```bash
bash /mnt/c/data/ds/serversetup/deploy/bootstrap-wsl.sh \
  --existing-mysql-container mysql8-mysql8-1 \
  --existing-mysql-database dsstats10 \
  --artifact-dir /mnt/c/Users/pax77/source/repos/dsstats/artifacts/server-local \
  --version 3.1.0
```

Compatibility mode reads the existing initialization credentials into private
WSL secret files without printing them, detects the actual MySQL version/image
and EF migration boundary, and leaves the database container and data directory
untouched. Bootstrap skips the expensive logical dump by default. Add
`--backup` to the bootstrap command when that rehearsal needs one. The sample
hook uses balanced gzip compression; for large databases, prefer a hook that
validates a recent snapshot.

Verify:

```bash
curl --fail http://127.0.0.1:6976/health/live
curl --fail http://127.0.0.1:6976/api10/Stats
curl --fail http://127.0.0.1:6876/health/live
curl --fail http://127.0.0.1:6876/
~/dsstats-server/deploy/measure-runtime.sh
```

Also exercise a Blazor connection, a replay/API operation, container restart,
a deliberately failing backup hook with `--backup`, and a deliberately failing smoke URL.
Confirm the prior application links remain active after each failure.

## Runtime image maintenance and performance

Runtime changes are separate from application deployment. Pull and test a new
ASP.NET patch image in WSL, update `DOTNET_ASPNET_IMAGE` to its approved digest,
then recreate API and web during a maintenance window. Record the digest in the
deployment log. Apply the same explicit process to MySQL, but only as a planned
database maintenance project.

Use `deploy/measure-runtime.sh` and `docker stats --no-stream` to record image
size, startup time, idle CPU/memory, and Docker disk usage. Logs are rotated by
Compose. Do not add CPU/memory limits until measurements establish safe values.
Liveness checks never query MySQL; only the deployment smoke test performs a
database-backed request.

## Service and MAUI distribution

For the Windows service, download and verify the central `service/v<version>`
release, mirror its exact MSI and `latest.yml` to a draft release in
`ipax77/dsstats.service`, test the updater, then publish the draft. Do not use
`src/service/deploy.ps1` for production.

For MAUI, download and verify the central `maui/v<version>` package and submit
that exact unsigned Store candidate through Microsoft Partner Center. Do not
rebuild or repack it locally.

## Emergency rule

If provenance, checksum, configuration, backup, migration state, image digest,
or target path is uncertain, stop and leave the running release in place. Keep
at least the current and previous known-good application releases; pruning is a
separate explicit operation.
