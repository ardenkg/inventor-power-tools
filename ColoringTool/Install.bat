@echo off
echo ================================================
echo   ColoringTool Add-in Installer
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.
echo Running installation script...
echo.

PowerShell -ExecutionPolicy Bypass -File "%~dp0Install-ColoringTool.ps1"

echo.
pause
