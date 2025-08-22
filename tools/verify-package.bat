@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Package Verification Script
echo ========================================
echo.

:: Change to project root
cd /d "%~dp0\.."

:: Check if package exists
if not exist "ChronosRewind-1.0.0.zip" (
    echo ERROR: Package file not found. Run package.bat first.
    echo Expected: ChronosRewind-1.0.0.zip
    exit /b 1
)

echo [1/5] Verifying package structure...

:: Extract package to temp directory for verification
if exist "temp_verify" (
    rmdir /s /q "temp_verify"
)
mkdir "temp_verify"

:: Extract package
powershell -Command "Expand-Archive -Path 'ChronosRewind-1.0.0.zip' -DestinationPath 'temp_verify' -Force"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Failed to extract package for verification
    exit /b 1
)

:: Verify required files exist
echo [2/5] Checking required files...

set MISSING_FILES=0

if not exist "temp_verify\manifest.json" (
    echo   ERROR: manifest.json missing
    set MISSING_FILES=1
) else (
    echo   ✓ manifest.json found
)

if not exist "temp_verify\README.md" (
    echo   ERROR: README.md missing
    set MISSING_FILES=1
) else (
    echo   ✓ README.md found
)

if not exist "temp_verify\icon.png" (
    echo   ERROR: icon.png missing
    set MISSING_FILES=1
) else (
    echo   ✓ icon.png found
)

if not exist "temp_verify\ChronoPara.dll" (
    echo   ERROR: ChronoPara.dll missing
    set MISSING_FILES=1
) else (
    echo   ✓ ChronoPara.dll found
)

if not exist "temp_verify\chronomancer.bundle" (
    echo   WARNING: chronomancer.bundle missing (optional asset)
    echo   ⚠ Custom visual/audio effects will not be available
) else (
    echo   ✓ chronomancer.bundle found
)

if !MISSING_FILES! EQU 1 (
    echo.
    echo ERROR: Required files are missing from the package
    rmdir /s /q "temp_verify"
    exit /b 1
)

:: Verify manifest.json structure
echo [3/5] Validating manifest.json...

powershell -Command "try { $manifest = Get-Content 'temp_verify\manifest.json' | ConvertFrom-Json; if ($manifest.name -and $manifest.version_number -and $manifest.dependencies) { Write-Host '   ✓ Manifest structure valid' } else { Write-Host '   ERROR: Invalid manifest structure'; exit 1 } } catch { Write-Host '   ERROR: Invalid JSON format'; exit 1 }"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: manifest.json validation failed
    rmdir /s /q "temp_verify"
    exit /b 1
)

:: Check DLL dependencies
echo [4/5] Checking DLL dependencies...

:: Use PowerShell to check if the DLL can be loaded (basic validation)
powershell -Command "try { [System.Reflection.Assembly]::LoadFile((Resolve-Path 'temp_verify\ChronoPara.dll').Path) | Out-Null; Write-Host '   ✓ DLL loads successfully' } catch { Write-Host '   ERROR: DLL failed to load -' $_.Exception.Message; exit 1 }"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: DLL validation failed
    rmdir /s /q "temp_verify"
    exit /b 1
)

:: Verify file sizes (basic sanity check)
echo [5/5] Checking file sizes...

for %%F in ("temp_verify\ChronoPara.dll") do (
    if %%~zF LSS 10000 (
        echo   ERROR: ChronoPara.dll is suspiciously small (%%~zF bytes)
        rmdir /s /q "temp_verify"
        exit /b 1
    ) else (
        echo   ✓ ChronoPara.dll size: %%~zF bytes
    )
)

for %%F in ("temp_verify\README.md") do (
    if %%~zF LSS 1000 (
        echo   WARNING: README.md is quite small (%%~zF bytes)
    ) else (
        echo   ✓ README.md size: %%~zF bytes
    )
)

:: Clean up temp directory
rmdir /s /q "temp_verify"

echo.
echo ========================================
echo  PACKAGE VERIFICATION SUCCESSFUL
echo ========================================
echo.
echo Package Contents Summary:
echo   - manifest.json: Valid structure and dependencies
echo   - README.md: Comprehensive installation guide
echo   - icon.png: Mod icon for Thunderstore
echo   - ChronoPara.dll: Main mod assembly
if exist "release\chronomancer.bundle" (
    echo   - chronomancer.bundle: Custom visual/audio assets
) else (
    echo   - chronomancer.bundle: Not included (fallback mode)
)

echo.
echo The package is ready for distribution!
echo.
echo Next Steps:
echo   1. Upload ChronoParadox-1.0.0.zip to Thunderstore
echo   2. Test installation in a clean game environment
echo   3. Verify multiplayer synchronization with other players

endlocal