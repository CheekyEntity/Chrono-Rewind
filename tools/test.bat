@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Running Chronomancer's Paradox Tests
echo ========================================
echo.

:: Change to source directory
cd /d "%~dp0\..\src"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to navigate to source directory
    exit /b 1
)

:: Check if project builds first
echo [1/2] Verifying project builds...
dotnet build --configuration Debug --verbosity quiet --no-restore
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Project does not build. Run build.bat first to see detailed errors.
    exit /b 1
)
echo   - Project builds successfully

:: Note about test execution
echo [2/2] Test execution info...
echo.
echo NOTE: This mod uses runtime validation tests that execute when the plugin loads.
echo The tests are integrated into the BepInEx plugin initialization process.
echo.
echo To run tests:
echo   1. Build the mod with build.bat
echo   2. Install the mod in your game
echo   3. Check the BepInEx console/logs for test results
echo.
echo Test classes included:
echo   - ConfigManagerTests (configuration validation)
echo   - PositionSnapshotTests (data structure tests)
echo   - HistoryManagerTests (utility method tests)

echo.
echo ========================================
echo  TEST INFO COMPLETE
echo ========================================

endlocal