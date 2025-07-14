Get-ChildItem -Path . -Directory -Recurse -Force |
Where-Object {
    ($_.Name -in 'bin', 'obj') -and
    (-not ($_.FullName -match '\\node_modules\\'))
} |
ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Recurse -Force
    Write-Host "Deleted folder: $($_.FullName)"
}