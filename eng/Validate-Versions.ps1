[CmdletBinding()]
param(
    [string]$BaseRef = "",
    [string]$Tag = "",
    [switch]$ParserOutputIdentical
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Get-IntegerProperty([xml]$document, [string]$name) {
    $node = $document.SelectSingleNode("/Project/PropertyGroup/$name")
    if ($null -eq $node) {
        throw "Missing version property '$name'."
    }

    $value = 0
    if (-not [int]::TryParse($node.InnerText, [ref]$value) -or $value -lt 0) {
        throw "Version property '$name' must be a non-negative integer."
    }

    return $value
}

function Read-VersionManifest([string]$content) {
    [xml]$document = $content
    $major = Get-IntegerProperty $document "DsstatsVersionMajor"
    $parserLine = Get-IntegerProperty $document "DsstatsParserLine"
    $patches = [ordered]@{
        parser = Get-IntegerProperty $document "DsstatsParserPatch"
        mydsstats = Get-IntegerProperty $document "DsstatsMyDsstatsPatch"
        service = Get-IntegerProperty $document "DsstatsServicePatch"
        maui = Get-IntegerProperty $document "DsstatsMauiPatch"
        server = Get-IntegerProperty $document "DsstatsServerPatch"
    }

    $versions = [ordered]@{}
    foreach ($component in $patches.Keys) {
        $versions[$component] = "$major.$parserLine.$($patches[$component])"
    }

    return [pscustomobject]@{
        Major = $major
        ParserLine = $parserLine
        Patches = $patches
        Versions = $versions
    }
}

function Assert-Equal([string]$actual, [string]$expected, [string]$description) {
    if ($actual -ne $expected) {
        throw "$description is '$actual'; expected '$expected'."
    }
}

function Get-EvaluatedVersion([string]$projectPath) {
    $output = & dotnet msbuild $projectPath -nologo -getProperty:Version
    if ($LASTEXITCODE -ne 0) {
        throw "Failed evaluating Version for '$projectPath'."
    }

    return ($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1).Trim()
}

Push-Location $repositoryRoot
try {
    $manifestPath = Join-Path $repositoryRoot "Directory.Build.props"
    $manifest = Read-VersionManifest (Get-Content -LiteralPath $manifestPath -Raw)
    Assert-Equal ([string]$manifest.Major) "3" "DsstatsVersionMajor"
    Assert-Equal ([string]$manifest.Patches.parser) "0" "DsstatsParserPatch"

    $projects = [ordered]@{
        parser = "src/common/dsstats.parser/dsstats.parser.csproj"
        mydsstats = "src/mydsstats/dsstats.pwa/dsstats.pwa.csproj"
        service = "src/service/dsstats.service/dsstats.service.csproj"
        maui = "src/maui/dsstats.maui/dsstats.maui.csproj"
        server = "src/server/dsstats.api/dsstats.api.csproj"
    }
    foreach ($component in $projects.Keys) {
        Assert-Equal (Get-EvaluatedVersion $projects[$component]) $manifest.Versions[$component] "$component MSBuild version"
    }
    Assert-Equal (Get-EvaluatedVersion "src/server/dsstats.web/dsstats.web.csproj") $manifest.Versions.server "server web MSBuild version"

    $prefixSource = Get-Content "src/common/dsstats.shared/Upload/ReplayDecoderVersion.cs" -Raw
    foreach ($requiredPrefix in @(
        'MauiPrefix = "ma"',
        'MyDsstatsPrefix = "myds"',
        'ServicePrefix = "ser"',
        'ApiPrefix = "api"')) {
        if (-not $prefixSource.Contains($requiredPrefix, [StringComparison]::Ordinal)) {
            throw "Missing canonical upload prefix contract: $requiredPrefix"
        }
    }

    if ([string]::IsNullOrWhiteSpace($Tag) -and $env:GITHUB_REF_TYPE -eq "tag") {
        $Tag = $env:GITHUB_REF_NAME
    }
    if (-not [string]::IsNullOrWhiteSpace($Tag)) {
        if ($Tag -notmatch '^(parser|mydsstats|service|maui|server)/v(\d+\.\d+\.\d+)$') {
            throw "Release tag '$Tag' must use <component>/v<major>.<parser-line>.<patch>."
        }

        Assert-Equal $Matches[2] $manifest.Versions[$Matches[1]] "release tag version"
    }

    if (-not [string]::IsNullOrWhiteSpace($BaseRef)) {
        $baseManifestContent = & git show "${BaseRef}:Directory.Build.props" 2>$null
        if ($LASTEXITCODE -eq 0 -and $baseManifestContent) {
            $baseManifest = Read-VersionManifest ($baseManifestContent -join [Environment]::NewLine)
            $changedPaths = @(& git diff --name-only "$BaseRef...HEAD")
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to compare version changes with '$BaseRef'."
            }

            $parserContractPaths = @(
                '^src/common/dsstats\.parser/(?!README\.md$|.*\.slnx?$)',
                '^src/common/dsstats\.shared/(ReplayDto|ReplayV2Dto|ReplayTourneyInfoDto|SpawnPlayback[^/]*|Enums)\.cs$'
            )
            $parserChanged = $false
            foreach ($path in $changedPaths) {
                if ($parserContractPaths | Where-Object { $path -match $_ }) {
                    $parserChanged = $true
                    break
                }
            }

            if ($parserChanged -and $manifest.ParserLine -eq $baseManifest.ParserLine -and -not $ParserOutputIdentical) {
                throw "Parser/output changes require a parser-line bump or the controlled parser-output-identical exemption."
            }

            if ($manifest.ParserLine -ne $baseManifest.ParserLine) {
                if ($manifest.ParserLine -le $baseManifest.ParserLine) {
                    throw "DsstatsParserLine must increase."
                }
                foreach ($component in $manifest.Patches.Keys) {
                    Assert-Equal ([string]$manifest.Patches[$component]) "0" "$component patch after parser-line bump"
                }
            }
            else {
                $componentPatterns = [ordered]@{
                    mydsstats = '^src/mydsstats/'
                    service = '^src/service/'
                    maui = '^src/maui/'
                    server = '^src/server/'
                }
                $sharedRuntimeChanged = $changedPaths | Where-Object {
                    $_ -match '^src/common/' -and
                    $_ -notmatch '/README\.md$' -and
                    $_ -notmatch '\.slnx?$' -and
                    $_ -notmatch '^src/common/dsstats\.parser/'
                }
                foreach ($component in $componentPatterns.Keys) {
                    $componentChanged = $sharedRuntimeChanged -or ($changedPaths | Where-Object { $_ -match $componentPatterns[$component] })
                    if ($componentChanged -and $manifest.Patches[$component] -le $baseManifest.Patches[$component]) {
                        throw "$component runtime changes require increasing Dsstats$component patch while the parser line is unchanged."
                    }
                }
            }
        }
        else {
            Write-Host "Base revision has no central version manifest; treating this as the initial harmonization."
            # A missing manifest is expected during the one-time conversion. Clear
            # git show's native exit code so a successful script does not fail CI.
            $global:LASTEXITCODE = 0
        }
    }

    Write-Host "Version contract valid: parser=$($manifest.Versions.parser), mydsstats=$($manifest.Versions.mydsstats), service=$($manifest.Versions.service), maui=$($manifest.Versions.maui), server=$($manifest.Versions.server)."
}
finally {
    Pop-Location
}
