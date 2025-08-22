@echo off
echo Chronomancer's Paradox - Development Settings
echo.

set /p STEAM_PATH="Enter your Steam library path (e.g., C:\SteamLibrary): "
set /p MAGE_ARENA_PATH="Enter Mage Arena install path (or press Enter for default): "

if "%MAGE_ARENA_PATH%"=="" (
    set "MAGE_ARENA_PATH=%STEAM_PATH%\steamapps\common\Mage Arena"
)

echo.
echo Updating project file with Assembly-CSharp reference...

set "ASSEMBLY_PATH=%MAGE_ARENA_PATH%\MageArena_Data\Managed\Assembly-CSharp.dll"

if not exist "%ASSEMBLY_PATH%" (
    echo ERROR: Assembly-CSharp.dll not found at: %ASSEMBLY_PATH%
    echo Please verify your Mage Arena installation path.
    pause
    exit /b 1
)

echo Assembly-CSharp found at: %ASSEMBLY_PATH%
echo.
echo Settings configured successfully!
echo You can now run tools\setup.bat to initialize the development environment.

pause