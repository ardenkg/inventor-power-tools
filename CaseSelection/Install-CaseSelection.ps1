<#
.SYNOPSIS
    Installs the CaseSelection Add-in for Autodesk Inventor (2022-2026).

.DESCRIPTION
    This script builds the CaseSelection project and copies the necessary files
    to the Inventor Add-ins folder. Automatically detects installed Inventor versions
    and deploys the correct build (net48 for 2022-2024, net8.0-windows for 2025-2026).

.PARAMETER InstallScope
    Specifies the installation scope: 'User' (default) or 'Machine'.
    Machine scope requires administrator privileges.

.PARAMETER SkipBuild
    If specified, skips the build step and only copies existing files.

.PARAMETER Configuration
    Build configuration: 'Release' (default) or 'Debug'.

.EXAMPLE
    .\Install-CaseSelection.ps1

.EXAMPLE
    .\Install-CaseSelection.ps1 -InstallScope Machine -Configuration Debug
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('User', 'Machine')]
    [string]$InstallScope = 'User',

    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$AddinName = 'CaseSelection'
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
$ProjectDir = $ScriptDir
$ProjectFile = Join-Path $ProjectDir "$AddinName.csproj"
$AddinFile = Join-Path $ProjectDir "$AddinName.addin"

# Inventor version mapping: Year -> .NET framework to use
$InventorVersionMap = @(
    @{ Year = '2026'; Framework = 'net8.0-windows' }
    @{ Year = '2025'; Framework = 'net8.0-windows' }
    @{ Year = '2024'; Framework = 'net48' }
    @{ Year = '2023'; Framework = 'net48' }
    @{ Year = '2022'; Framework = 'net48' }
)

# Banner
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  $AddinName Add-in Installer" -ForegroundColor Cyan
Write-Host "  For Autodesk Inventor 2022-2026" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check for administrator privileges if machine scope
if ($InstallScope -eq 'Machine') {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "ERROR: Machine-level installation requires administrator privileges." -ForegroundColor Red
        Write-Host "Please run this script as Administrator." -ForegroundColor Yellow
        exit 1
    }
}

# Detect installed Inventor versions
$InstalledVersions = @()
foreach ($ver in $InventorVersionMap) {
    $inventorPath = "C:\Program Files\Autodesk\Inventor $($ver.Year)"
    if (Test-Path $inventorPath) {
        $InstalledVersions += $ver
        Write-Host "  Detected: Inventor $($ver.Year)" -ForegroundColor Green
    }
}

if ($InstalledVersions.Count -eq 0) {
    Write-Host "  WARNING: No Inventor installation detected." -ForegroundColor Yellow
    Write-Host "  Will install for all supported versions." -ForegroundColor Yellow
    $InstalledVersions = $InventorVersionMap
}

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Install Scope:   $InstallScope"
Write-Host "  Build Config:    $Configuration"
Write-Host ""

# Step 1: Build the project
if (-not $SkipBuild) {
    Write-Host "[1/3] Building project..." -ForegroundColor Green

    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetPath) {
        Write-Host "ERROR: .NET SDK not found. Please install .NET SDK 8.0 or later." -ForegroundColor Red
        exit 1
    }

    if (-not (Test-Path $ProjectFile)) {
        Write-Host "ERROR: Project file not found: $ProjectFile" -ForegroundColor Red
        exit 1
    }

    Push-Location $ProjectDir
    try {
        & dotnet build $ProjectFile -c $Configuration --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
        Write-Host "  Build completed successfully." -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "[1/3] Skipping build (using existing files)..." -ForegroundColor Yellow
}

# Step 2-3: Install for each detected Inventor version
Write-Host "[2/3] Preparing installation..." -ForegroundColor Green

$installedCount = 0

foreach ($ver in $InstalledVersions) {
    $framework = $ver.Framework
    $year = $ver.Year
    $OutputDir = Join-Path $ProjectDir "bin\$Configuration\$framework"

    if ($InstallScope -eq 'Machine') {
        $AddinsFolder = "C:\ProgramData\Autodesk\Inventor $year\Addins"
    } else {
        $AddinsFolder = Join-Path $env:APPDATA "Autodesk\Inventor $year\Addins"
    }

    $TargetFolder = Join-Path $AddinsFolder $AddinName

    Write-Host ""
    Write-Host "  Installing for Inventor $year ($framework)..." -ForegroundColor Cyan

    if (-not (Test-Path $AddinsFolder)) {
        New-Item -ItemType Directory -Path $AddinsFolder -Force | Out-Null
    }

    if (Test-Path $TargetFolder) {
        Remove-Item -Path $TargetFolder -Recurse -Force
    }
    New-Item -ItemType Directory -Path $TargetFolder -Force | Out-Null

    $filesToCopy = @("$AddinName.dll", "$AddinName.pdb", "$AddinName.deps.json", "$AddinName.runtimeconfig.json")
    foreach ($file in $filesToCopy) {
        $sourcePath = Join-Path $OutputDir $file
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $TargetFolder -Force
            Write-Host "    Copied: $file" -ForegroundColor Gray
        }
    }

    Copy-Item -Path $AddinFile -Destination $TargetFolder -Force
    Write-Host "    Copied: $AddinName.addin" -ForegroundColor Gray
    Write-Host "    -> $TargetFolder" -ForegroundColor Green
    $installedCount++
}

# Also install to shared machine folder if Machine scope
if ($InstallScope -eq 'Machine') {
    $SharedTarget = Join-Path 'C:\ProgramData\Autodesk\Inventor Addins' $AddinName
    $SharedOutputDir = Join-Path $ProjectDir "bin\$Configuration\net48"

    Write-Host ""
    Write-Host "  Installing to shared Addins folder (net48)..." -ForegroundColor Cyan
    if (Test-Path $SharedTarget) { Remove-Item -Path $SharedTarget -Recurse -Force }
    New-Item -ItemType Directory -Path $SharedTarget -Force | Out-Null

    foreach ($file in @("$AddinName.dll", "$AddinName.pdb", "$AddinName.deps.json", "$AddinName.runtimeconfig.json")) {
        $src = Join-Path $SharedOutputDir $file
        if (Test-Path $src) { Copy-Item -Path $src -Destination $SharedTarget -Force }
    }
    Copy-Item -Path $AddinFile -Destination $SharedTarget -Force
    Write-Host "    -> $SharedTarget" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/3] Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installed to $installedCount Inventor version(s)." -ForegroundColor Cyan
Write-Host ""
Write-Host "To use the add-in:" -ForegroundColor Yellow
Write-Host "  1. Start Autodesk Inventor"
Write-Host "  2. Open a Part or Assembly document"
Write-Host "  3. Go to the 'Power Tools' tab on the ribbon"
Write-Host "  4. Click the 'Class Selection' button"
Write-Host ""
Write-Host "To uninstall, run: .\Uninstall-CaseSelection.ps1" -ForegroundColor Gray
Write-Host ""
