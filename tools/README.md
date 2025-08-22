# Chronomancer's Paradox - Development Tools

This directory contains batch scripts for building and managing the Chronomancer's Paradox mod development workflow.

## Available Scripts

### Core Scripts

- **`build.bat`** - Build the mod and create Thunderstore package
- **`package.bat`** - Create Thunderstore package from existing build
- **`clean.bat`** - Clean all build artifacts and packages
- **`setup.bat`** - Set up the development environment and restore packages
- **`test.bat`** - Show information about the integrated test system

### Workflow Scripts

- **`dev.bat`** - Unified development workflow script with multiple commands

## Usage

### Individual Scripts
```cmd
# Build the mod
tools\build.bat

# Clean build artifacts
tools\clean.bat

# Set up development environment
tools\setup.bat

# Show test information
tools\test.bat
```

### Unified Workflow (dev.bat)
```cmd
# Show available commands
tools\dev.bat help

# Set up development environment
tools\dev.bat setup

# Build the mod and create Thunderstore package
tools\dev.bat build

# Create Thunderstore package only
tools\dev.bat package

# Clean build artifacts and packages
tools\dev.bat clean

# Clean and rebuild
tools\dev.bat rebuild

# Show test information
tools\dev.bat test

# Show project information
tools\dev.bat info
```

## Features

### Improved User Experience
- ✅ **No "Press any key to continue" prompts** - Scripts run and exit cleanly
- ✅ **Clear progress indicators** - Shows [1/3], [2/3], etc. for multi-step operations
- ✅ **Consistent formatting** - Professional output with clear headers and sections
- ✅ **Better error handling** - Proper exit codes and error messages

### Enhanced Functionality
- ✅ **Thunderstore packaging** - Automatically creates ready-to-upload zip packages
- ✅ **File size reporting** - Shows built DLL and package sizes after successful builds
- ✅ **Package verification** - Lists contents of created packages for validation
- ✅ **Verbose status messages** - Clear indication of what each step is doing
- ✅ **Robust error detection** - Checks for common issues and provides helpful messages
- ✅ **Directory validation** - Ensures scripts run from correct locations

### Development Workflow
- ✅ **Unified dev.bat script** - Single entry point for all development tasks
- ✅ **Project information** - Quick access to build status and file information
- ✅ **Integrated test information** - Documentation about the runtime test system

## Requirements

- .NET SDK 6.0 or later
- Windows command prompt or PowerShell
- BepInEx development dependencies (handled by setup.bat)

## Output Locations

- **Built DLL**: `src\bin\Release\netstandard2.1\ChronoPara.dll`
- **Debug builds**: `src\bin\Debug\netstandard2.1\ChronoPara.dll`
- **Thunderstore package**: `ChronoParadox-1.0.0.zip` (root directory)
- **Package staging**: `release\` (temporary, cleaned by clean.bat)
- **Temporary files**: `src\obj\` (cleaned by clean.bat)

## Thunderstore Package

The build process automatically creates a Thunderstore-ready zip package containing:
- `ChronoPara.dll` - The compiled mod
- `manifest.json` - Package metadata and dependencies
- `README.md` - Package documentation
- `icon.png` - Package icon

The package is named using the format: `{PackageName}-{Version}.zip` (e.g., `ChronoParadox-1.0.0.zip`)

## Error Handling

All scripts use proper exit codes:
- `0` - Success
- `1` - Error occurred

This allows for integration with CI/CD systems and other automation tools.