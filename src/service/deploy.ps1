dotnet publish .\dsstats.service\dsstats.service.csproj -c Release
dotnet build .\dsstats.installer\dsstats.installer.wixproj -c Release

$releasePath = ".\dsstats.installer\bin\Release"

$filesToDeploy = Get-ChildItem -Path $releasePath | Select-Object Name | Where-Object { $_ -match 'dsstats.installer.*' }

$file1 = Join-Path -Path $releasePath -ChildPath $filesToDeploy[0].Name

$fileContent  = Get-Content -Path "./dsstats.service/Services/DsstatsService.cs"
$regexPattern = 'CurrentVersion = new\((\d+), (\d+), (\d+)\);'
$versionMatch = $fileContent | Select-String -Pattern $regexPattern
$major = $versionMatch.Matches.Groups[1].Value
$minor = $versionMatch.Matches.Groups[2].Value
$patch = $versionMatch.Matches.Groups[3].Value
$versionString = "$major.$minor.$patch"

$sha256Checksum = Get-FileHash -Path $file1 -Algorithm SHA256 | Select-Object -ExpandProperty Hash

$yamlContent = @"
Version: $versionString
Checksum: $sha256Checksum
"@
$yamlFilePath = Join-Path -Path $releasePath -ChildPath 'latest.yml'
$yamlContent | Out-File -FilePath $yamlFilePath -Encoding UTF8

$ghVersion = "v$versionString"
gh release create --repo ipax77/dsstats.service --generate-notes --draft $ghVersion $file1 $yamlFilePath