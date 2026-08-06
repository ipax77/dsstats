dotnet publish .\dsstats.service\dsstats.service.csproj -c Release
dotnet build .\dsstats.installer\dsstats.installer.wixproj -c Release

$releasePath = ".\dsstats.installer\bin\Release"
$file1 = Join-Path -Path $releasePath -ChildPath "dsstats.installer.msi"
if (-not (Test-Path -LiteralPath $file1)) {
    throw "Installer was not produced at $file1"
}

$versionString = dotnet msbuild .\dsstats.service\dsstats.service.csproj -getProperty:DsstatsServiceVersion
$versionString = $versionString.Trim()
$parsedVersion = $null
if (-not [Version]::TryParse($versionString, [ref]$parsedVersion)) {
    throw "Invalid service version '$versionString' in Directory.Build.props"
}

$sha256Checksum = Get-FileHash -Path $file1 -Algorithm SHA256 | Select-Object -ExpandProperty Hash

$yamlContent = @"
Version: $versionString
Checksum: $sha256Checksum
"@
$yamlFilePath = Join-Path -Path $releasePath -ChildPath 'latest.yml'
$yamlContent | Out-File -FilePath $yamlFilePath -Encoding UTF8

$ghVersion = "v$versionString"
gh release create --repo ipax77/dsstats.service --generate-notes --draft $ghVersion $file1 $yamlFilePath
