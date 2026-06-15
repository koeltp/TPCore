# Taipi.Core NuGet Publish Script (Option 2: Permanently update version in csproj)
# Usage: .\publish.ps1

$ErrorActionPreference = "Stop"
$projectPath = "$PSScriptRoot\Taipi.Core.csproj"

# ---------- 1. Read current version ----------
Write-Host "Reading current version from csproj ..." -ForegroundColor Cyan
$xml = [xml](Get-Content $projectPath)
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003")
$currentVersionNode = $xml.SelectSingleNode("/ns:Project/ns:PropertyGroup/ns:Version", $ns)
if ($currentVersionNode -eq $null) {
    $currentVersionNode = $xml.SelectSingleNode("/Project/PropertyGroup/Version")
}
if ($currentVersionNode -eq $null) {
    Write-Host "Error: Cannot find <Version> element in csproj" -ForegroundColor Red
    exit 1
}
$currentVersion = $currentVersionNode.InnerText
Write-Host "Current version: $currentVersion" -ForegroundColor Green

# ---------- 2. Ask for new version ----------
$newVersion = Read-Host "New version (press Enter to keep $currentVersion)"
if ([string]::IsNullOrWhiteSpace($newVersion)) {
    $newVersion = $currentVersion
    Write-Host "Keeping current version: $newVersion" -ForegroundColor Yellow
}

# ---------- 3. Ask for API Key ----------
$ApiKey = Read-Host "NuGet API Key"
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "Error: API Key is required" -ForegroundColor Red
    exit 1
}

# ---------- 4. Update csproj if version changed ----------
if ($newVersion -ne $currentVersion) {
    Write-Host "Updating csproj version from $currentVersion to $newVersion ..." -ForegroundColor Cyan
    $currentVersionNode.InnerText = $newVersion
    $xml.Save($projectPath)
    Write-Host "csproj updated." -ForegroundColor Green
} else {
    Write-Host "Version unchanged, skipping csproj update." -ForegroundColor DarkGray
}

# ---------- 5. Clean output directories ----------
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

# ---------- 6. Pack (let dotnet restore automatically) ----------
Write-Host "Packing Taipi.Core version $newVersion ..." -ForegroundColor Cyan
dotnet pack $projectPath -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

# ---------- 7. Verify generated package ----------
$nupkg = "$PSScriptRoot\bin\Release\Taipi.Core.$newVersion.nupkg"
if (-not (Test-Path $nupkg)) {
    Write-Host "Error: Package not found at $nupkg" -ForegroundColor Red
    exit 1
}

# ---------- 8. Push to NuGet ----------
Write-Host "Pushing to NuGet ..." -ForegroundColor Cyan
dotnet nuget push $nupkg --source https://api.nuget.org/v3/index.json --api-key $ApiKey

if ($LASTEXITCODE -eq 0) {
    Write-Host "Success! Taipi.Core $newVersion published to NuGet" -ForegroundColor Green
} else {
    Write-Host "Failed to push to NuGet!" -ForegroundColor Red
    exit 1
}