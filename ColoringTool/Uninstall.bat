@echo off
echo ================================================
echo   ColoringTool Add-in Uninstaller
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.
echo Running uninstallation script...
echo.

PowerShell -ExecutionPolicy Bypass -File "%~dp0Uninstall-ColoringTool.ps1"

echo.
pause
