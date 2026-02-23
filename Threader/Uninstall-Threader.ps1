<#
.SYNOPSIS
    Uninstalls the Threader Add-in for Autodesk Inventor (2022-2026).

.DESCRIPTION
    Removes Threader from all detected Inventor Add-ins folders.

.PARAMETER UninstallScope
    Specifies the scope: 'User' (default), 'Machine', or 'All'.

.EXAMPLE
    .\Uninstall-Threader.ps1

.EXAMPLE
    .\Uninstall-Threader.ps1 -UninstallScope All
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('User', 'Machine', 'All')]
    [string]$UninstallScope = 'User',

    [Parameter()]
    [switch]$KeepSettings
)

$ErrorActionPreference = 'Stop'
$AddinName = 'Threader'
$SupportedYears = @('2022', '2023', '2024', '2025', '2026')

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  $AddinName Add-in Uninstaller" -ForegroundColor Cyan
Write-Host "  For Autodesk Inventor 2022-2026" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check admin for machine/all scope
if ($UninstallScope -in @('Machine', 'All')) {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "ERROR: Machine-level uninstallation requires administrator privileges." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Uninstall Scope: $UninstallScope" -ForegroundColor Yellow
Write-Host ""

# Check if Inventor is running
$inventorProcess = Get-Process -Name 'Inventor' -ErrorAction SilentlyContinue
if ($inventorProcess) {
    Write-Host "WARNING: Inventor is currently running." -ForegroundColor Yellow
    $response = Read-Host "Continue anyway? (Y/N)"
    if ($response -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Cancelled." -ForegroundColor Gray
        exit 0
    }
    Write-Host ""
}

# Build list of folders to check
$foldersToRemove = @()

foreach ($year in $SupportedYears) {
    if ($UninstallScope -in @('User', 'All')) {
        $foldersToRemove += Join-Path $env:APPDATA "Autodesk\Inventor $year\Addins\$AddinName"
    }
    if ($UninstallScope -in @('Machine', 'All')) {
        $foldersToRemove += "C:\ProgramData\Autodesk\Inventor $year\Addins\$AddinName"
    }
}

# Shared machine-level folder
if ($UninstallScope -in @('Machine', 'All')) {
    $foldersToRemove += "C:\ProgramData\Autodesk\Inventor Addins\$AddinName"
}

# Legacy locations
$foldersToRemove += Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\$AddinName"
$foldersToRemove += "C:\ProgramData\Autodesk\ApplicationPlugins\$AddinName"

# Remove folders
$removedCount = 0
foreach ($folder in $foldersToRemove) {
    if (Test-Path $folder) {
        Write-Host "Removing: $folder" -ForegroundColor Yellow
        try {
            Remove-Item -Path $folder -Recurse -Force
            Write-Host "  Removed" -ForegroundColor Green
            $removedCount++
        }
        catch {
            Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
if ($removedCount -gt 0) {
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "  Uninstallation Complete!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Removed $removedCount installation(s). Restart Inventor to complete." -ForegroundColor Cyan
} else {
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host "  No Installations Found" -ForegroundColor Yellow
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "$AddinName was not found in any expected location." -ForegroundColor Gray
}
Write-Host ""

