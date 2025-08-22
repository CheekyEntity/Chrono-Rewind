@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Chronomancer's Paradox Dev Workflow
echo ========================================
echo.

if "%1"=="" (
    echo Usage: dev.bat [command]
    echo.
    echo Available commands:
    echo   setup    - Set up development environment
    echo   build    - Build the mod and create Thunderstore package
    echo   package  - Create Thunderstore package from existing build
    echo   clean    - Clean build artifacts and packages
    echo   rebuild  - Clean and build
    echo   test     - Show test information
    echo   info     - Show project information
    echo   help     - Show this help
    echo.
    echo Examples:
    echo   dev.bat setup
    echo   dev.bat rebuild
    echo   dev.bat package
    goto :end
)

set COMMAND=%1

if "%COMMAND%"=="setup" (
    call "%~dp0setup.bat"
) else if "%COMMAND%"=="build" (
    call "%~dp0build.bat"
) else if "%COMMAND%"=="package" (
    call "%~dp0package.bat"
) else if "%COMMAND%"=="clean" (
    call "%~dp0clean.bat"
) else if "%COMMAND%"=="rebuild" (
    echo ========================================
    echo  REBUILD: Clean + Build
    echo ========================================
    call "%~dp0clean.bat"
    if !ERRORLEVEL! EQU 0 (
        echo.
        call "%~dp0build.bat"
    )
) else if "%COMMAND%"=="test" (
    call "%~dp0test.bat"
) else if "%COMMAND%"=="info" (
    echo ========================================
    echo  Project Information
    echo ========================================
    echo.
    echo Project: Chronomancer's Paradox
    echo Type: BepInEx Plugin for Mage Arena
    echo Target: .NET Standard 2.1
    echo.
    cd /d "%~dp0\..\src"
    if exist "ChronoPara.csproj" (
        echo Project file: ChronoPara.csproj
        for %%A in ("ChronoPara.csproj") do echo Last modified: %%~tA
    )
    echo.
    if exist "bin\Release\netstandard2.1\ChronoPara.dll" (
        echo Built DLL: bin\Release\netstandard2.1\ChronoPara.dll
        for %%A in ("bin\Release\netstandard2.1\ChronoPara.dll") do (
            echo Size: %%~zA bytes
            echo Last built: %%~tA
        )
    ) else (
        echo Built DLL: Not found - run build.bat
    )
) else if "%COMMAND%"=="help" (
    call "%~dp0dev.bat"
) else (
    echo ERROR: Unknown command '%COMMAND%'
    echo Run 'dev.bat help' for available commands
    exit /b 1
)

:end
endlocal