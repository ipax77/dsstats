dotnet publish .\dsstats.maui8.csproj -f net8.0-windows10.0.19041.0 -c Release

$releasePath = "./dsstats.maui8/bin/Release/net8.0-windows10.0.19041.0/win10-x64/AppPackages"

$latestDir = (Get-ChildItem -Directory $releasePath | Sort-Object CreationTime)[-1].FullName

$filesToDeploy = Get-ChildItem -Path $latestDir | Select-Object Name | Where-Object { $_ -match 'dsstats.maui8*' }

$regex = [regex] "\d+.\d+.\d+.\d+"

$version = $regex.Match($filesToDeploy[0].Name).Value

# $file1 = Join-Path -Path $latestDir -ChildPath $filesToDeploy[0].Name
$file2 = Join-Path -Path $latestDir -ChildPath $filesToDeploy[1].Name
$file3 = Join-Path -Path $latestDir -ChildPath "latest.yml"

if (!(Test-Path $file3))
{
    New-Item -Path $file3
    "Version: " + $version + "`r`n" | Out-File -FilePath $file3
}

$ghVersion = "v" + $version

# gh release create --generate-notes --draft $ghVersion $file1 $file2 $file3
gh release create --generate-notes --draft $ghVersion $file2 $file3