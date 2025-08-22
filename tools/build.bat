@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Building Chronos Rewind Mod
echo ========================================
echo.

:: Change to source directory
cd /d "%~dp0\..\src"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to navigate to source directory
    exit /b 1
)

:: Clean previous build
echo [1/3] Cleaning previous build...
dotnet clean --verbosity quiet
if !ERRORLEVEL! NEQ 0 (
    echo WARNING: Clean operation had issues, continuing...
)
echo.

:: Restore packages
echo [2/3] Restoring packages...
dotnet restore --verbosity quiet
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Package restore failed
    exit /b 1
)

:: Build project
echo [3/5] Building project...
dotnet build --configuration Release --no-restore --verbosity normal

if !ERRORLEVEL! NEQ 0 (
    echo.
    echo ========================================
    echo  BUILD FAILED
    echo ========================================
    echo Check the output above for error details.
    exit /b 1
)

:: Create Thunderstore package
echo [4/5] Creating Thunderstore package...
cd /d "%~dp0\.."

:: Create release directory
if exist "release" rmdir /s /q "release"
mkdir "release"

:: Copy package files
echo   - Copying package files...
xcopy "packageFiles\*" "release\" /E /I /Q >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy package files
    exit /b 1
)

:: Copy built DLL
echo   - Copying mod DLL...
copy "src\bin\Release\netstandard2.1\ChronosRewind.dll" "release\" >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy mod DLL
    exit /b 1
)

:: Copy AssetBundle if it exists
if exist "src\Resources\chronomancer.bundle" (
    echo   - Copying AssetBundle...
    copy "src\Resources\chronomancer.bundle" "release\" >nul
    if !ERRORLEVEL! NEQ 0 (
        echo WARNING: Failed to copy AssetBundle - mod will use fallback assets
    ) else (
        echo     AssetBundle copied successfully
    )
) else (
    echo   - AssetBundle not found - mod will use fallback assets
)





:: Create zip package
echo [5/5] Creating zip package...
cd release
powershell -Command "Compress-Archive -Path * -DestinationPath '../ChronosRewind-1.0.0.zip' -Force"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to create zip package
    cd ..
    exit /b 1
)
cd ..

echo.
echo ========================================
echo  BUILD SUCCESSFUL
echo ========================================
echo Mod DLL: src\bin\Release\netstandard2.1\ChronosRewind.dll
echo Thunderstore Package: ChronosRewind-1.0.0.zip
echo.

:: Show file sizes
if exist "src\bin\Release\netstandard2.1\ChronosRewind.dll" (
    for %%A in ("src\bin\Release\netstandard2.1\ChronosRewind.dll") do (
        echo DLL size: %%~zA bytes
    )
)
if exist "ChronosRewind-1.0.0.zip" (
    for %%A in ("ChronosRewind-1.0.0.zip") do (
        echo Package size: %%~zA bytes
    )
)

endlocal