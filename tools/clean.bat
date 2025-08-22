@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Cleaning Chronomancer's Paradox
echo ========================================
echo.

:: Change to source directory
cd /d "%~dp0\..\src"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to navigate to source directory
    exit /b 1
)

:: Run dotnet clean
echo [1/3] Running dotnet clean...
dotnet clean --verbosity quiet
if !ERRORLEVEL! NEQ 0 (
    echo WARNING: dotnet clean had issues, continuing with manual cleanup...
)

:: Remove bin directories
echo [2/3] Removing bin directories...
if exist bin (
    rmdir /s /q bin
    if !ERRORLEVEL! EQU 0 (
        echo   - bin directory removed
    ) else (
        echo   - WARNING: Failed to remove bin directory
    )
) else (
    echo   - bin directory not found
)

:: Remove obj directories
echo [3/4] Removing obj directories...
if exist obj (
    rmdir /s /q obj
    if !ERRORLEVEL! EQU 0 (
        echo   - obj directory removed
    ) else (
        echo   - WARNING: Failed to remove obj directory
    )
) else (
    echo   - obj directory not found
)

:: Clean release artifacts
echo [4/4] Cleaning release artifacts...
cd /d "%~dp0\.."
if exist release (
    rmdir /s /q release
    if !ERRORLEVEL! EQU 0 (
        echo   - release directory removed
    ) else (
        echo   - WARNING: Failed to remove release directory
    )
) else (
    echo   - release directory not found
)

:: Remove zip packages
for %%f in (*.zip) do (
    del "%%f"
    echo   - removed %%f
)

echo.
echo ========================================
echo  CLEAN COMPLETE
echo ========================================

endlocal