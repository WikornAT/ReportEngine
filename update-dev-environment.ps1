# update-dev-environment.ps1

$ErrorActionPreference = "Stop"

Write-Host "=== .NET Info ===" -ForegroundColor Cyan
dotnet --info

Write-Host "`n=== Update Global Tools ===" -ForegroundColor Cyan
dotnet tool update --global dotnet-format
dotnet tool update --global dotnet-outdated-tool
dotnet tool update --global dotnet-reportgenerator-globaltool
dotnet tool update --global dotnet-sonarscanner

Write-Host "`n=== Update Local Tools ===" -ForegroundColor Cyan
dotnet tool restore

# update only if exists in manifest
$localTools = dotnet tool list --local | Select-Object -Skip 2

if ($localTools -match "dotnet-ef") {
    dotnet tool update dotnet-ef
}

if ($localTools -match "dotnet-stryker") {
    dotnet tool update dotnet-stryker
}

Write-Host "`n=== Check NuGet Packages ===" -ForegroundColor Cyan
dotnet outdated

Write-Host "`n=== Final Tool Versions ===" -ForegroundColor Cyan
dotnet tool list --global
dotnet tool list --local