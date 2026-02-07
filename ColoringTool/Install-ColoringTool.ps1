<#
.SYNOPSIS
    Installs the ColoringTool Add-in for Autodesk Inventor 2026.

.DESCRIPTION
    This script builds the ColoringTool project and copies the necessary files
    to the Inventor Add-ins folder. It supports both user-level and machine-level
    installation.

.PARAMETER InstallScope
    Specifies the installation scope: 'User' (default) or 'Machine'.
    Machine scope requires administrator privileges.

.PARAMETER SkipBuild
    If specified, skips the build step and only copies existing files.

.PARAMETER Configuration
    Build configuration: 'Release' (default) or 'Debug'.

.EXAMPLE
    .\Install-ColoringTool.ps1
    
.EXAMPLE
    .\Install-ColoringTool.ps1 -InstallScope Machine -Configuration Debug

.NOTES
    Author: ColoringTool Development Team
    Version: 1.0.0
    Requires: .NET SDK 8.0+, Autodesk Inventor 2026
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

# Script configuration
$ErrorActionPreference = 'Stop'
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Definition }
$ProjectDir = $ScriptDir
$ProjectFile = Join-Path $ProjectDir 'ColoringTool.csproj'
$AddinFile = Join-Path $ProjectDir 'ColoringTool.addin'

# Determine output directory
$OutputDir = Join-Path $ProjectDir "bin\$Configuration"

# Determine target Add-ins folder based on scope
if ($InstallScope -eq 'Machine') {
    $AddinsFolder = 'C:\ProgramData\Autodesk\Inventor Addins'
} else {
    $AddinsFolder = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins'
}

$TargetFolder = Join-Path $AddinsFolder 'ColoringTool'

# Banner
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  ColoringTool Add-in Installer" -ForegroundColor Cyan
Write-Host "  For Autodesk Inventor 2026" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check for administrator privileges if machine scope
if ($InstallScope -eq 'Machine') {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host "ERROR: Machine-level installation requires administrator privileges." -ForegroundColor Red
        Write-Host "Please run this script as Administrator." -ForegroundColor Yellow
        exit 1
    }
}

# Display configuration
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Install Scope: $InstallScope"
Write-Host "  Build Config:  $Configuration"
Write-Host "  Target Folder: $TargetFolder"
Write-Host ""

# Step 1: Build the project
if (-not $SkipBuild) {
    Write-Host "[1/3] Building project..." -ForegroundColor Green
    
    # Check if dotnet is available
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetPath) {
        Write-Host "ERROR: .NET SDK not found. Please install .NET SDK 8.0 or later." -ForegroundColor Red
        exit 1
    }
    
    # Check if project file exists
    if (-not (Test-Path $ProjectFile)) {
        Write-Host "ERROR: Project file not found: $ProjectFile" -ForegroundColor Red
        exit 1
    }
    
    # Build the project
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

# Step 2: Create target directory
Write-Host "[2/3] Preparing installation directory..." -ForegroundColor Green

# Create parent Add-ins folder if it doesn't exist
if (-not (Test-Path $AddinsFolder)) {
    Write-Host "  Creating Add-ins folder: $AddinsFolder"
    New-Item -ItemType Directory -Path $AddinsFolder -Force | Out-Null
}

# Remove existing installation if present
if (Test-Path $TargetFolder) {
    Write-Host "  Removing existing installation..."
    Remove-Item -Path $TargetFolder -Recurse -Force
}

# Create target folder
New-Item -ItemType Directory -Path $TargetFolder -Force | Out-Null
Write-Host "  Created: $TargetFolder"

# Step 3: Copy files
Write-Host "[3/3] Copying files..." -ForegroundColor Green

# Files to copy
$filesToCopy = @(
    'ColoringTool.dll',
    'ColoringTool.pdb',
    'ColoringTool.addin'
)

# Additional dependencies (if any)
$additionalDeps = @(
    'ColoringTool.deps.json',
    'ColoringTool.runtimeconfig.json'
)

# Copy main files
foreach ($file in $filesToCopy) {
    $sourcePath = Join-Path $OutputDir $file
    if ($file -eq 'ColoringTool.addin') {
        $sourcePath = $AddinFile
    }
    
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $TargetFolder -Force
        Write-Host "  Copied: $file"
    } else {
        if ($file -ne 'ColoringTool.pdb') {
            Write-Host "  WARNING: File not found: $file" -ForegroundColor Yellow
        }
    }
}

# Copy additional dependencies
foreach ($file in $additionalDeps) {
    $sourcePath = Join-Path $OutputDir $file
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $TargetFolder -Force
        Write-Host "  Copied: $file"
    }
}

# Create a copy of the .addin file with correct path
$addinContent = Get-Content -Path $AddinFile -Raw
$addinTargetPath = Join-Path $TargetFolder 'ColoringTool.addin'
Set-Content -Path $addinTargetPath -Value $addinContent -Force

# Completion message
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "The ColoringTool Add-in has been installed to:" -ForegroundColor Cyan
Write-Host "  $TargetFolder" -ForegroundColor White
Write-Host ""
Write-Host "To use the add-in:" -ForegroundColor Yellow
Write-Host "  1. Start Autodesk Inventor 2026"
Write-Host "  2. Open a Part or Assembly document"
Write-Host "  3. Go to the 'Power Tools' tab on the ribbon"
Write-Host "  4. Click the 'Coloring Tool' button"
Write-Host ""
Write-Host "To uninstall, run: .\Uninstall-ColoringTool.ps1" -ForegroundColor Gray
Write-Host ""
