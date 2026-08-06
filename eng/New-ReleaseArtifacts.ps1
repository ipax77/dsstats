[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("parser", "mydsstats", "service", "maui", "server")]
    [string]$Component,
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$OutputPath = "artifacts"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
if (-not $artifactRoot.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Artifact output must remain inside the repository."
}

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactRoot | Out-Null

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed."
    }
}

function Compress-Directory([string]$Source, [string]$Destination) {
    Compress-Archive -Path (Join-Path $Source "*") -DestinationPath $Destination -CompressionLevel Optimal
}

Push-Location $repositoryRoot
try {
    switch ($Component) {
        "parser" {
            Invoke-Dotnet @("restore", "src/common/dsstats.parser/dsstats.parser.csproj")
            Invoke-Dotnet @("pack", "src/common/dsstats.parser/dsstats.parser.csproj", "-c", "Release", "--no-restore", "-o", $artifactRoot)
        }
        "mydsstats" {
            Invoke-Dotnet @("workload", "install", "wasm-tools", "--skip-manifest-update")
            Invoke-Dotnet @("restore", "src/mydsstats/dsstats.pwa/dsstats.pwa.csproj")
            $publish = Join-Path $artifactRoot "site"
            Invoke-Dotnet @("publish", "src/mydsstats/dsstats.pwa/dsstats.pwa.csproj", "-c", "Release", "--no-restore", "-p:RunAOTCompilation=true", "-o", $publish)
            Compress-Directory $publish (Join-Path $artifactRoot "mydsstats-v$Version.zip")
            Remove-Item -LiteralPath $publish -Recurse -Force
        }
        "server" {
            foreach ($project in @("api", "web")) {
                Invoke-Dotnet @("restore", "src/server/dsstats.$project/dsstats.$project.csproj")
                $publish = Join-Path $artifactRoot $project
                Invoke-Dotnet @("publish", "src/server/dsstats.$project/dsstats.$project.csproj", "-c", "Release", "--no-restore", "-o", $publish)
                Compress-Directory $publish (Join-Path $artifactRoot "dsstats-$project-v$Version.zip")
                Remove-Item -LiteralPath $publish -Recurse -Force
            }
        }
        "service" {
            Invoke-Dotnet @("restore", "src/service/service.slnx")
            Invoke-Dotnet @("publish", "src/service/dsstats.service/dsstats.service.csproj", "-c", "Release", "--no-restore")
            Invoke-Dotnet @("build", "src/service/dsstats.installer/dsstats.installer.wixproj", "-c", "Release", "--no-restore")
            $installer = "src/service/dsstats.installer/bin/Release/dsstats.installer.msi"
            if (-not (Test-Path -LiteralPath $installer)) {
                throw "Service installer was not produced."
            }
            $target = Join-Path $artifactRoot "dsstats.installer.msi"
            Copy-Item -LiteralPath $installer -Destination $target
            $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
            Set-Content -LiteralPath (Join-Path $artifactRoot "latest.yml") -Encoding utf8 -Value "Version: $Version`nChecksum: $hash"
        }
        "maui" {
            Invoke-Dotnet @("workload", "install", "maui-windows", "--skip-manifest-update")
            Invoke-Dotnet @("restore", "src/maui/dsstats.maui/dsstats.maui.csproj")
            $publish = Join-Path $artifactRoot "package"
            $packageOutput = $publish + [System.IO.Path]::DirectorySeparatorChar
            Invoke-Dotnet @("publish", "src/maui/dsstats.maui/dsstats.maui.csproj", "-f", "net10.0-windows10.0.19041.0", "-c", "Release", "--no-restore", "-p:WindowsPackageType=MSIX", "-p:AppxPackageSigningEnabled=false", "-p:AppxPackageDir=$packageOutput", "-o", $publish)
            $packages = @(Get-ChildItem -LiteralPath $publish -Recurse -File | Where-Object { $_.Extension -in @(".msix", ".msixupload", ".appxupload") })
            if ($packages.Count -eq 0) {
                throw "MAUI publish did not produce an MSIX or Store upload package."
            }
            foreach ($package in $packages) {
                Copy-Item -LiteralPath $package.FullName -Destination $artifactRoot
            }
            Remove-Item -LiteralPath $publish -Recurse -Force
        }
    }

    $checksumLines = Get-ChildItem -LiteralPath $artifactRoot -File | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    }
    Set-Content -LiteralPath (Join-Path $artifactRoot "SHA256SUMS") -Encoding utf8 -Value $checksumLines
}
finally {
    Pop-Location
}
