@echo off
REM ============================================================================
REM iNode Add-in Uninstaller (Batch Wrapper)
REM ============================================================================

echo.
echo ================================================
echo   iNode Add-in Uninstaller
echo ================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-iNode.ps1" %*

echo.
pause
