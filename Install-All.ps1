<#
.SYNOPSIS
    Installs all Inventor Add-ins (CaseSelection, ColoringTool, Threader).

.DESCRIPTION
    This script builds and installs all add-ins in this repository to the
    Inventor Add-ins folder. Each add-in appears under the "Power Tools" tab in Inventor's ribbon.

.PARAMETER InstallScope
    Specifies the installation scope: 'User' (default) or 'Machine'.
    Machine scope requires administrator privileges.

.PARAMETER Configuration
    Build configuration: 'Release' (default) or 'Debug'.

.PARAMETER SkipBuild
    If specified, skips the build step and only copies existing files.

.EXAMPLE
    .\Install-All.ps1

.EXAMPLE
    .\Install-All.ps1 -InstallScope Machine -Configuration Debug
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

$ErrorActionPreference = 'Continue'
$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Inventor Add-ins - Install All" -ForegroundColor Cyan
Write-Host "  CaseSelection | ColoringTool | Threader | iNode" -ForegroundColor Cyan
Write-Host "  For Autodesk Inventor 2022-2026" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Script directory: $ScriptDir" -ForegroundColor Gray
Write-Host ""

$addins = @('CaseSelection', 'ColoringTool', 'Threader', 'iNode')
$failed = @()
$succeeded = @()

foreach ($addin in $addins) {
    $installerPath = Join-Path $ScriptDir "$addin\Install-$addin.ps1"

    if (-not (Test-Path $installerPath)) {
        Write-Host "WARNING: Installer not found at $installerPath - skipping $addin." -ForegroundColor Yellow
        $failed += $addin
        continue
    }

    Write-Host ""
    Write-Host "--- Installing $addin ---" -ForegroundColor Cyan

    try {
        if ($SkipBuild) {
            & $installerPath -InstallScope $InstallScope -Configuration $Configuration -SkipBuild
        } else {
            & $installerPath -InstallScope $InstallScope -Configuration $Configuration
        }

        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
            Write-Host "ERROR: $addin installer returned exit code $LASTEXITCODE" -ForegroundColor Red
            $failed += $addin
        } else {
            $succeeded += $addin
        }
    }
    catch {
        Write-Host "ERROR: Failed to install $addin - $($_.Exception.Message)" -ForegroundColor Red
        $failed += $addin
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Installation Summary" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

if ($succeeded.Count -gt 0) {
    Write-Host "  Installed: $($succeeded -join ', ')" -ForegroundColor Green
}
if ($failed.Count -gt 0) {
    Write-Host "  Failed:    $($failed -join ', ')" -ForegroundColor Red
}

Write-Host ""
Write-Host "Restart Inventor to load the add-ins." -ForegroundColor Yellow
Write-Host "All tools appear under the 'Power Tools' tab in the ribbon." -ForegroundColor Yellow
Write-Host "Supports Inventor 2022, 2023, 2024, 2025, and 2026." -ForegroundColor Yellow
Write-Host ""
