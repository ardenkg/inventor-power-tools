@echo off
REM ============================================================================
REM CaseSelection Add-in Uninstaller (Batch Wrapper)
REM For Autodesk Inventor 2022-2026
REM ============================================================================

echo.
echo ================================================
echo   CaseSelection Add-in Uninstaller
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.

REM Get the directory of this script
set "SCRIPT_DIR=%~dp0"

REM Check if PowerShell is available
where powershell >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not available on this system.
    echo Please run the Uninstall-CaseSelection.ps1 script manually.
    pause
    exit /b 1
)

REM Run the PowerShell uninstaller
echo Running PowerShell uninstaller...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Uninstall-CaseSelection.ps1" %*

if %errorlevel% neq 0 (
    echo.
    echo Uninstallation encountered an error.
    pause
    exit /b %errorlevel%
)

echo.
echo Press any key to exit...
pause >nul
