@echo off
REM ============================================================================
REM iNode Add-in Installer (Batch Wrapper)
REM For Autodesk Inventor 2022-2026
REM ============================================================================

echo.
echo ================================================
echo   iNode Add-in Installer
echo   Visual Parametric Editor for Inventor
echo ================================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: PowerShell not found. Please install PowerShell.
    pause
    exit /b 1
)

REM Run the PowerShell installer
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-iNode.ps1" %*

echo.
if %errorlevel% neq 0 (
    echo Installation failed. See errors above.
) else (
    echo Installation completed successfully.
)
echo.
pause
