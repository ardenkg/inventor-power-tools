@echo off
echo ================================================
echo   Inventor Add-ins - Install All
echo   CaseSelection ^| ColoringTool ^| Threader ^| iNode
echo   For Autodesk Inventor 2022-2026
echo ================================================
echo.
echo Running installation script...
echo.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Install-All.ps1"
echo.
pause
