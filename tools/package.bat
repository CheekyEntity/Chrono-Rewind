@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Creating Thunderstore Package
echo ========================================
echo.

:: Change to project root
cd /d "%~dp0\.."

:: Check if DLL exists
if not exist "src\bin\Release\netstandard2.1\ChronosRewind.dll" (
    echo ERROR: Mod DLL not found. Run build.bat first.
    echo Expected: src\bin\Release\netstandard2.1\ChronosRewind.dll
    exit /b 1
)

:: Read version from manifest.json
echo [1/4] Reading package information...
for /f "tokens=2 delims=:, " %%a in ('findstr "version_number" packageFiles\manifest.json') do (
    set VERSION=%%a
    set VERSION=!VERSION:"=!
)
for /f "tokens=2 delims=:, " %%a in ('findstr "name" packageFiles\manifest.json') do (
    set PACKAGE_NAME=%%a
    set PACKAGE_NAME=!PACKAGE_NAME:"=!
)

echo   - Package: !PACKAGE_NAME!
echo   - Version: !VERSION!

:: Create release directory
echo [2/4] Preparing release directory...
if exist "release" (
    rmdir /s /q "release"
    if !ERRORLEVEL! NEQ 0 (
        echo ERROR: Failed to clean existing release directory
        exit /b 1
    )
)
mkdir "release"
echo   - Release directory created

:: Copy package files
echo [3/4] Copying package files...
copy "packageFiles\manifest.json" "release\" >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy manifest.json
    exit /b 1
)
echo   - manifest.json copied

copy "packageFiles\README.md" "release\" >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy README.md
    exit /b 1
)
echo   - README.md copied

copy "packageFiles\icon.png" "release\" >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy icon.png
    exit /b 1
)
echo   - icon.png copied

:: Copy built DLL
copy "src\bin\Release\netstandard2.1\ChronosRewind.dll" "release\" >nul
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to copy ChronosRewind.dll
    exit /b 1
)
echo   - ChronosRewind.dll copied

:: Copy asset bundle
if exist "src\Resources\chronomancer.bundle" (
    copy "src\Resources\chronomancer.bundle" "release\" >nul
    if !ERRORLEVEL! NEQ 0 (
        echo ERROR: Failed to copy chronomancer.bundle
        exit /b 1
    )
    echo   - chronomancer.bundle copied
) else (
    echo WARNING: chronomancer.bundle not found - custom assets will not be available
    echo   - Continuing without asset bundle (fallback mode)
)

:: Create zip package
echo [4/4] Creating zip package...
set PACKAGE_FILE=ChronosRewind-!VERSION!.zip
cd release
powershell -Command "Compress-Archive -Path * -DestinationPath '../!PACKAGE_FILE!' -Force"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to create zip package
    cd ..
    exit /b 1
)
cd ..

:: Verify package contents
echo   - Verifying package contents...
powershell -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip = [System.IO.Compression.ZipFile]::OpenRead('!PACKAGE_FILE!'); $zip.Entries | ForEach-Object { Write-Host ('     - ' + $_.Name) }; $zip.Dispose()"

echo.
echo ========================================
echo  PACKAGE CREATED SUCCESSFULLY
echo ========================================
echo Package: !PACKAGE_FILE!
echo.

:: Show file sizes and details
if exist "!PACKAGE_FILE!" (
    for %%A in ("!PACKAGE_FILE!") do (
        echo Package size: %%~zA bytes
        echo Created: %%~tA
    )
)

echo.
echo Ready for Thunderstore upload!
echo Upload the !PACKAGE_FILE! file to Thunderstore.

endlocal