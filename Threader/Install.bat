@echo off
echo ================================================
echo   Threader Add-in Installer
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.
echo Running installation script...
echo.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Install-Threader.ps1"
echo.
pause
