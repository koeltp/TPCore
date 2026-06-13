# Taipi.Core NuGet Publish Script
# Usage: .\publish.ps1

$ErrorActionPreference = "Stop"
$projectPath = "$PSScriptRoot\Taipi.Core.csproj"

# Read current version from csproj
$xml = [xml](Get-Content $projectPath)
$currentVersion = $xml.SelectSingleNode('/Project/PropertyGroup/Version').InnerText

# Prompt for version
$Version = Read-Host "Version (current: $currentVersion)"
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $currentVersion
    Write-Host "Using current version: $Version" -ForegroundColor Yellow
}

# Prompt for API key
$ApiKey = Read-Host "NuGet API Key"
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "Error: API Key is required" -ForegroundColor Red
    exit 1
}

# Clean output directories
Write-Host "Cleaning output directories ..." -ForegroundColor Cyan
$dirs = @("$PSScriptRoot\bin", "$PSScriptRoot\obj")
foreach ($dir in $dirs) {
    if (Test-Path $dir) {
        try { Remove-Item $dir -Recurse -Force -ErrorAction Stop }
        catch {
            Write-Host "Warning: Cannot delete $dir, may be locked by another process" -ForegroundColor Yellow
            Write-Host "  $($_.Exception.Message)" -ForegroundColor DarkGray
        }
    }
}

# Pack
Write-Host "Packing ..." -ForegroundColor Cyan
dotnet pack $projectPath -c Release

# Check package
$nupkg = "$PSScriptRoot\bin\Release\Taipi.Core.$Version.nupkg"
if (-not (Test-Path $nupkg)) {
    Write-Host "Error: Package not found at $nupkg" -ForegroundColor Red
    exit 1
}

# Push to NuGet
Write-Host "Pushing to NuGet ..." -ForegroundColor Cyan
dotnet nuget push $nupkg --source https://api.nuget.org/v3/index.json --api-key $ApiKey

if ($LASTEXITCODE -eq 0) {
    Write-Host "Success! Taipi.Core $Version published to NuGet" -ForegroundColor Green
} else {
    Write-Host "Failed!" -ForegroundColor Red
    exit 1
}
