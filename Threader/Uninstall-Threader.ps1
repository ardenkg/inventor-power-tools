<#
.SYNOPSIS
    Uninstalls the Threader Add-in for Autodesk Inventor 2026.

.DESCRIPTION
    This script removes the Threader Add-in files from the Inventor Add-ins folder.
    It supports both user-level and machine-level uninstallation.

.PARAMETER UninstallScope
    Specifies the uninstallation scope: 'User' (default), 'Machine', or 'All'.
    Machine and All scopes require administrator privileges.

.PARAMETER KeepSettings
    If specified, preserves any user settings or configuration files.

.EXAMPLE
    .\Uninstall-Threader.ps1
    
.EXAMPLE
    .\Uninstall-Threader.ps1 -UninstallScope All

.NOTES
    Author: Threader Development Team
    Version: 1.0.0
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('User', 'Machine', 'All')]
    [string]$UninstallScope = 'User',
    
    [Parameter()]
    [switch]$KeepSettings
)

# Script configuration
$ErrorActionPreference = 'Stop'

# Define installation paths
$UserAddinsFolder = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins\Threader'
$MachineAddinsFolder = 'C:\ProgramData\Autodesk\Inventor Addins\Threader'

# Banner
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Threader Add-in Uninstaller" -ForegroundColor Cyan
Write-Host "  For Autodesk Inventor 2026" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check for administrator privileges if needed
if ($UninstallScope -in @('Machine', 'All')) {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host "ERROR: Machine-level uninstallation requires administrator privileges." -ForegroundColor Red
        Write-Host "Please run this script as Administrator." -ForegroundColor Yellow
        exit 1
    }
}

# Determine folders to remove
$foldersToRemove = @()

switch ($UninstallScope) {
    'User' {
        $foldersToRemove += $UserAddinsFolder
    }
    'Machine' {
        $foldersToRemove += $MachineAddinsFolder
    }
    'All' {
        $foldersToRemove += $UserAddinsFolder
        $foldersToRemove += $MachineAddinsFolder
    }
}

# Display configuration
Write-Host "Uninstall Scope: $UninstallScope" -ForegroundColor Yellow
Write-Host ""

# Check if Inventor is running
$inventorProcess = Get-Process -Name 'Inventor' -ErrorAction SilentlyContinue
if ($inventorProcess) {
    Write-Host "WARNING: Autodesk Inventor is currently running." -ForegroundColor Yellow
    Write-Host "Please close Inventor before uninstalling the add-in." -ForegroundColor Yellow
    Write-Host ""
    
    $response = Read-Host "Do you want to continue anyway? (Y/N)"
    if ($response -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host "Uninstallation cancelled." -ForegroundColor Gray
        exit 0
    }
    Write-Host ""
}

# Remove folders
$removedCount = 0
foreach ($folder in $foldersToRemove) {
    if (Test-Path $folder) {
        Write-Host "Removing: $folder" -ForegroundColor Yellow
        try {
            Remove-Item -Path $folder -Recurse -Force
            Write-Host "  Removed successfully." -ForegroundColor Green
            $removedCount++
        }
        catch {
            Write-Host "  ERROR: Failed to remove folder: $_" -ForegroundColor Red
        }
    } else {
        Write-Host "Not found: $folder" -ForegroundColor Gray
    }
}

# Completion message
Write-Host ""
if ($removedCount -gt 0) {
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "  Uninstallation Complete!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "The Threader Add-in has been removed." -ForegroundColor Cyan
    Write-Host "Please restart Inventor to complete the uninstallation." -ForegroundColor Yellow
} else {
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host "  No Installation Found" -ForegroundColor Yellow
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "The Threader Add-in was not found in the specified location(s)." -ForegroundColor Gray
}
Write-Host ""
