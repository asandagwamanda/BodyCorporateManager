$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'BodyCorporateManager.Desktop/BodyCorporateManager.Desktop.csproj'
$publishDir = Join-Path $PSScriptRoot 'publish/win-x64'
$databaseSource = Join-Path $PSScriptRoot 'bodycorporate.db'

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $publishDir

if (Test-Path $databaseSource) {
    Copy-Item $databaseSource -Destination $publishDir -Force
}

Write-Host ''
Write-Host 'Published app folder:'
Write-Host $publishDir
Write-Host ''
Write-Host 'Run the app from:'
Write-Host (Join-Path $publishDir 'BodyCorporateManager.Desktop.exe')
