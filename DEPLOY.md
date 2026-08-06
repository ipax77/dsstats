# Deployment runbook

This runbook promotes the immutable artifacts produced by the central
`ipax77/dsstats` release workflow. Production hosts must not pull `main` and
rebuild a release. The tag, compiled files, and checksums reviewed in the
central repository are the files that reach production.

Deployment remains manually approved. The workflow creates release candidates;
it does not deploy a website, publish the service updater release, sign a
package, or submit an app to the Microsoft Store.

## Release artifacts

| Product | Central tag | Produced artifacts | Production destination |
| --- | --- | --- | --- |
| Parser | `parser/v3.1.0` | `dsstats.parser.3.1.0.nupkg` | Package/archive only; no standalone production process |
| mydsstats | `mydsstats/v3.1.0` | `mydsstats-v3.1.0.zip` | Static files for `mydsstats.pax77.org` |
| Service | `service/v3.1.0` | `dsstats.installer.msi`, `latest.yml` | Public release in `ipax77/dsstats.service` |
| Server | `server/v3.1.0` | `dsstats-api-v3.1.0.zip`, `dsstats-web-v3.1.0.zip` | API and web application hosts |
| MAUI | `maui/v3.1.0` | App MSIX and SHA-256 manifest | Manual Microsoft Partner Center submission |

Every artifact set contains `SHA256SUMS`. It covers all other files in that
set. The service's `latest.yml` additionally contains the MSI checksum expected
by the updater.

## 1. Create and approve a release candidate

1. Merge the version change and product changes to `main` only after CI is
   green.
2. Confirm `Directory.Build.props` contains the intended version. A parser-line
   change must reset all product patches to zero.
3. Create an annotated, component-specific tag at the reviewed commit and push
   it to the central repository:

   ```bash
   git switch main
   git pull --ff-only origin main
   git tag -a mydsstats/v3.1.0 -m "mydsstats 3.1.0"
   git push origin mydsstats/v3.1.0
   ```

4. Wait for the `Release candidate` workflow. It validates the tag, builds the
   component, creates `SHA256SUMS`, and opens a draft release in
   `ipax77/dsstats`.
5. Review the workflow, release notes, artifact names, and checksums. Never
   delete and recreate or move an immutable tag. Fix a bad candidate with a new
   patch version.
6. Publish the central draft when it is approved for promotion:

   ```bash
   gh release edit mydsstats/v3.1.0 \
     --repo ipax77/dsstats \
     --draft=false \
     --latest=false
   ```

For a parser-line release, create the parser tag and a tag for every product
that will be released from that line. A practical rollout order is server,
mydsstats, service, then MAUI. This lets the API record the new decoder sources
before updated clients become common.

## 2. Download and verify artifacts

Download into a new, empty directory. Do not reuse a previous candidate
directory.

```bash
REPOSITORY=ipax77/dsstats
TAG=mydsstats/v3.1.0
CANDIDATE_DIR="$HOME/dsstats-releases/$TAG"

mkdir -p "$CANDIDATE_DIR"
gh release download "$TAG" --repo "$REPOSITORY" --dir "$CANDIDATE_DIR"
cd "$CANDIDATE_DIR"
sha256sum --check SHA256SUMS
```

On Windows, the equivalent checksum verification is:

```powershell
$candidate = "C:\dsstats-releases\service\v3.1.0"
Get-Content (Join-Path $candidate "SHA256SUMS") | ForEach-Object {
    $expected, $name = $_ -split '\s+', 2
    $path = Join-Path $candidate $name.Trim()
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $expected) {
        throw "Checksum mismatch: $path"
    }
}
```

Stop immediately if a file is missing, an unexpected file is present, or a
checksum differs. Do not repair or rebuild the candidate locally.

## 3. Deploy mydsstats

The previous `publishMyDsstats.sh` flow pulled `main`, rebuilt the PWA, deleted
the live directory, and copied the new build in place. Replace that flow with an
artifact-only deployment. Keep each extracted version so activation and
rollback are atomic symlink changes.

One-time host preparation is required because `/var/www/mydsstats` is currently
a real directory. During a maintenance window, move that directory into the
release store and replace it with a symlink. Confirm that the nginx site root
continues to resolve `/var/www/mydsstats` before proceeding.

```bash
sudo install -d -o root -g root /var/www/releases/mydsstats
LEGACY_RELEASE="/var/www/releases/mydsstats/pre-artifact-$(date -u +%Y%m%dT%H%M%SZ)"
sudo mv /var/www/mydsstats "$LEGACY_RELEASE"
sudo ln -s "$LEGACY_RELEASE" /var/www/mydsstats
```

For each release, download and verify the artifact as the normal deployment
user, then extract and activate it:

```bash
VERSION=3.1.0
CANDIDATE_DIR="$HOME/dsstats-releases/mydsstats/v$VERSION"
RELEASE_ROOT="/var/www/releases/mydsstats/$VERSION"
SITE_DIR="$RELEASE_ROOT/wwwroot"
NEXT_LINK=/var/www/mydsstats.next

sudo test ! -e "$RELEASE_ROOT"
sudo install -d -o www-data -g www-data "$RELEASE_ROOT"
sudo -u www-data unzip -q "$CANDIDATE_DIR/mydsstats-v$VERSION.zip" -d "$RELEASE_ROOT"
sudo test -f "$SITE_DIR/index.html"
sudo test -d "$SITE_DIR/_framework"
sudo chown -R www-data:www-data "$RELEASE_ROOT"

sudo ln -s "$SITE_DIR" "$NEXT_LINK"
sudo mv -Tf "$NEXT_LINK" /var/www/mydsstats
```

Do not run `buildMydsstats.pl`, `dotnet publish`, or `git pull` during this
deployment. The new deployment script should accept a version/tag, download the
central artifact, verify it, extract it, and switch the symlink only.

Smoke-test the public site and a replay decode/upload:

```bash
curl --fail --silent --show-error https://mydsstats.pax77.org/ > /dev/null
```

Confirm that the dashboard records the upload under `mydsstats` with the
expected three-part decoder version.

To roll back static files, point the symlink at the previous release directory:

```bash
PREVIOUS_VERSION=3.1.0
sudo ln -s "/var/www/releases/mydsstats/$PREVIOUS_VERSION/wwwroot" /var/www/mydsstats.next
sudo mv -Tf /var/www/mydsstats.next /var/www/mydsstats
```

Keep at least the current and previous known-good releases.

## 4. Deploy server API and web

API and web share one version and must be promoted from the same `server/v...`
release. The archives are framework-dependent .NET publishes; the host needs
the .NET 10 ASP.NET Core runtime, not the SDK.

The exact production service/container names and publish roots are host
configuration, not repository data. Record them in the private server
operations configuration. The example below assumes systemd units named
`dsstats-api` and `dsstats-web`, versioned release directories under
`/opt/dsstats/releases`, and stable symlinks used by `ExecStart`. Substitute the
real names if production uses Docker or different paths.

Converting an existing in-place deployment to stable symlinks is a one-time
maintenance operation like the mydsstats preparation above: move each current
publish directory into a named legacy release, point the API/web process at a
stable `*-current` symlink, and verify that release before the first switch.

Before deployment:

- back up MySQL using the production credential file; do not place credentials
  on the command line or in this repository;
- confirm `/data/localserverconfig.json` and other production configuration are
  external to the publish directories;
- record the current API/web symlink targets and database backup location;
- verify both archives with `SHA256SUMS`.

The API calls `Database.Migrate()` at startup. Starting the new API therefore
applies pending MySQL migrations. Database rollback is not automatic.

```bash
VERSION=3.1.0
CANDIDATE_DIR="$HOME/dsstats-releases/server/v$VERSION"
API_RELEASE="/opt/dsstats/releases/api/$VERSION"
WEB_RELEASE="/opt/dsstats/releases/web/$VERSION"

sudo test ! -e "$API_RELEASE"
sudo test ! -e "$WEB_RELEASE"
sudo install -d -o dsstats -g dsstats "$API_RELEASE" "$WEB_RELEASE"
sudo -u dsstats unzip -q "$CANDIDATE_DIR/dsstats-api-v$VERSION.zip" -d "$API_RELEASE"
sudo -u dsstats unzip -q "$CANDIDATE_DIR/dsstats-web-v$VERSION.zip" -d "$WEB_RELEASE"
sudo test -f "$API_RELEASE/dsstats.api.dll"
sudo test -f "$WEB_RELEASE/dsstats.web.dll"

sudo ln -s "$API_RELEASE" /opt/dsstats/api.next
sudo mv -Tf /opt/dsstats/api.next /opt/dsstats/api-current
sudo systemctl restart dsstats-api
sudo systemctl is-active --quiet dsstats-api
API_SMOKE_URL=http://127.0.0.1:5279/api10/Stats # Set to the real host-local API URL.
curl --fail --silent --show-error "$API_SMOKE_URL" > /dev/null

sudo ln -s "$WEB_RELEASE" /opt/dsstats/web.next
sudo mv -Tf /opt/dsstats/web.next /opt/dsstats/web-current
sudo systemctl restart dsstats-web
sudo systemctl is-active --quiet dsstats-web
curl --fail --silent --show-error https://dsstats.pax77.org/ > /dev/null
```

If Docker runs the applications, mount the stable symlinks or versioned release
directories read-only and recreate only the affected API/web containers. Do not
build an image from a newly pulled working tree unless a later CI adapter wraps
these exact archives into the image without rebuilding them.

For an application rollback, switch both symlinks to the previous shared server
version and restart both processes. Leave a successfully applied additive
database migration in place when the old application supports it. If a future
migration is destructive or incompatible, use the documented database restore
procedure and treat that as a separate, explicitly approved recovery action.

## 5. Publish the Windows service updater release

`src/service/deploy.ps1` is now a legacy build-and-publish helper. Do not use it
for a production release because it rebuilds the MSI outside the tagged central
workflow.

Download and verify `service/v<version>` from `ipax77/dsstats`. Check that:

- the MSI ProductVersion is `3.<parser-line>.<patch>.0`;
- `latest.yml` contains the three-part version;
- the checksum in `latest.yml` equals the SHA-256 of
  `dsstats.installer.msi`;
- `SHA256SUMS` validates both files.

Mirror those exact two files to a draft release in the distribution repository:

```powershell
$version = "3.1.0"
$candidate = "C:\dsstats-releases\service\v$version"
$distributionTag = "v$version"

gh release view $distributionTag --repo ipax77/dsstats.service *> $null
if ($LASTEXITCODE -eq 0) {
    throw "Distribution release $distributionTag already exists; do not overwrite it."
}

gh release create $distributionTag `
    (Join-Path $candidate "dsstats.installer.msi") `
    (Join-Path $candidate "latest.yml") `
    --repo ipax77/dsstats.service `
    --draft `
    --generate-notes `
    --title $distributionTag
```

Download the two draft assets once from `ipax77/dsstats.service`, verify them
again, and test the installer/update path on a non-production Windows machine.
Then make the distribution release public:

```powershell
gh release edit "v3.1.0" --repo ipax77/dsstats.service --draft=false
```

Publishing this release is the service deployment gate because clients discover
the public updater release. If a release is bad, stop further uptake by marking
it as a prerelease or removing it from the public updater channel, then issue a
new patch release. Prefer patch-forward recovery; an MSI downgrade may not be
supported for clients that already upgraded.

## 6. Submit the MAUI Store candidate

`src/maui/build.ps1` is also replaced as the production build step. The central
`maui/v<version>` release already contains the unsigned Store candidate built
from the reviewed tag.

1. Download the release and verify `SHA256SUMS` on Windows.
2. Confirm the app package filename and manifest identity use
   `3.<parser-line>.<patch>.1`, for example `3.1.0.1`.
3. Sideload/test the candidate on a clean machine if required. A bundled
   `Microsoft.WindowsAppRuntime` MSIX is a local testing dependency and is not
   the dsstats application package.
4. Upload the dsstats application MSIX to its existing Microsoft Partner Center
   submission. Do not rebuild, edit, repack, or locally change its version.
5. Complete Store validation and use a staged rollout when available.
6. After Store installation, decode/upload a replay and confirm dashboard
   telemetry reports `maui` and the expected three-part version.

The Store controls signing and distribution. If certification fails, fix the
source and create a new product patch/tag. If a released package is bad, halt
the rollout and submit a patch-forward package; Store clients cannot be directly
rolled back by replacing files on the host.

## 7. Complete and record the deployment

For every promotion, record:

- central tag and commit SHA;
- artifact SHA-256 values;
- operator and UTC deployment time;
- previous production version;
- database backup identifier for server releases;
- smoke-test result;
- service distribution release or Store submission URL, when applicable.

Keep the central component release and its artifacts. Production automation may
later implement these steps behind GitHub environment approvals, but it must
consume the same checksummed artifacts and must never rebuild during promotion.

## Emergency rule

If provenance, checksum, configuration, migration state, or target path is
uncertain, stop. Leave the currently running release in place and resolve the
uncertainty before changing production.
