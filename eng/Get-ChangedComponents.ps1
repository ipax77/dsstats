[CmdletBinding()]
param(
    [string]$BaseRef = "",
    [string]$GitHubOutput = ""
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Push-Location $repositoryRoot
try {
    if ([string]::IsNullOrWhiteSpace($BaseRef)) {
        $changedPaths = @("Directory.Build.props")
        $forceAll = $true
    }
    else {
        $changedPaths = @(& git diff --name-only "$BaseRef...HEAD")
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to compare changed components with '$BaseRef'."
        }
        $forceAll = $false
    }

    $versioning = $forceAll -or [bool]($changedPaths | Where-Object {
        $_ -eq "Directory.Build.props" -or $_ -match '^eng/' -or $_ -match '^\.github/workflows/'
    })
    $shared = $forceAll -or [bool]($changedPaths | Where-Object { $_ -match '^src/common/' })
    $values = [ordered]@{
        parser = $versioning -or $shared -or [bool]($changedPaths | Where-Object { $_ -match '^src/tests/dsstats\.parser\.tests/' })
        mydsstats = $versioning -or $shared -or [bool]($changedPaths | Where-Object { $_ -match '^src/mydsstats/' })
        service = $versioning -or $shared -or [bool]($changedPaths | Where-Object { $_ -match '^src/service/' })
        maui = $versioning -or $shared -or [bool]($changedPaths | Where-Object { $_ -match '^src/maui/' })
        server = $versioning -or $shared -or [bool]($changedPaths | Where-Object { $_ -match '^src/server/' -or $_ -match '^src/play/' -or $_ -match '^src/tests/dsstats\.tests/' })
    }

    foreach ($entry in $values.GetEnumerator()) {
        $value = $entry.Value.ToString().ToLowerInvariant()
        Write-Host "$($entry.Key)=$value"
        if (-not [string]::IsNullOrWhiteSpace($GitHubOutput)) {
            Add-Content -LiteralPath $GitHubOutput -Value "$($entry.Key)=$value"
        }
    }
}
finally {
    Pop-Location
}
