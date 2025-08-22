@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Chronomancer's Paradox Setup
echo ========================================
echo.

:: Check for .NET SDK
echo [1/4] Checking for .NET SDK...
dotnet --version >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: .NET SDK not found
    echo Please install .NET SDK 6.0 or later from:
    echo https://dotnet.microsoft.com/download
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo   - Found .NET SDK version: !DOTNET_VERSION!
)

:: Change to source directory
echo [2/4] Navigating to source directory...
cd /d "%~dp0\..\src"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to navigate to source directory
    exit /b 1
)
echo   - Current directory: %CD%

:: Check project file
echo [3/4] Validating project structure...
if not exist "ChronoPara.csproj" (
    echo ERROR: ChronoPara.csproj not found in source directory
    exit /b 1
)
echo   - Project file found

:: Check References directory
if not exist "References" (
    echo   - Creating References directory...
    mkdir References
)
echo   - References directory ready

:: Restore NuGet packages
echo [4/4] Restoring NuGet packages...
dotnet restore --verbosity quiet

if !ERRORLEVEL! EQU 0 (
    echo.
    echo ========================================
    echo  SETUP COMPLETE
    echo ========================================
    echo.
    echo Next steps:
    echo   1. Place required API DLLs in src\References\
    echo   2. Update Assembly-CSharp reference path in .csproj if needed
    echo   3. Run build.bat to compile the mod
    echo.
    echo Available commands:
    echo   - tools\build.bat   : Build the mod
    echo   - tools\clean.bat   : Clean build artifacts
    echo   - tools\setup.bat   : Re-run this setup
) else (
    echo.
    echo ========================================
    echo  SETUP FAILED
    echo ========================================
    echo Package restore failed. Check your internet connection and try again.
    exit /b 1
)

endlocal