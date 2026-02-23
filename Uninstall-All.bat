@echo off
echo ================================================
echo   Inventor Add-ins - Uninstall All
echo ================================================
echo.
echo Running uninstallation script...
echo.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Uninstall-All.ps1"
echo.
pause
