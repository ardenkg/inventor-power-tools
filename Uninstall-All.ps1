<#
.SYNOPSIS
    Uninstalls all Inventor Add-ins (CaseSelection, ColoringTool, Threader).

.DESCRIPTION
    This script removes all add-in files from the Inventor Add-ins folder.

.PARAMETER UninstallScope
    Specifies the scope: 'User' (default), 'Machine', or 'All'.

.EXAMPLE
    .\Uninstall-All.ps1

.EXAMPLE
    .\Uninstall-All.ps1 -UninstallScope All
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('User', 'Machine', 'All')]
    [string]$UninstallScope = 'User'
)

$ErrorActionPreference = 'Stop'
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Inventor Add-ins - Uninstall All" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$addins = @('CaseSelection', 'ColoringTool', 'Threader', 'iNode')

foreach ($addin in $addins) {
    $uninstallerPath = Join-Path $ScriptDir "$addin\Uninstall-$addin.ps1"

    if (-not (Test-Path $uninstallerPath)) {
        Write-Host "WARNING: Uninstaller not found for $addin — skipping." -ForegroundColor Yellow
        continue
    }

    Write-Host "--- Uninstalling $addin ---" -ForegroundColor Cyan
    try {
        & $uninstallerPath -UninstallScope $UninstallScope
    }
    catch {
        Write-Host "ERROR: Failed to uninstall $addin — $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Uninstallation Complete" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Restart Inventor to fully unload the add-ins." -ForegroundColor Yellow
Write-Host ""
