@echo off
echo ================================================
echo   Threader Add-in Uninstaller
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.
echo Running uninstall script...
echo.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Uninstall-Threader.ps1"
echo.
pause
