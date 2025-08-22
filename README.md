# Chronos Rewind

A BepInEx mod for Mage Arena that introduces temporal manipulation mechanics through the "Chronos Rewind" spell. Harness the power of Chronos to rewind your position and health, escaping danger or repositioning strategically!

## Features

- **Chronos Rewind Spell**: Rewind your position and health to a previous state (default: 3 seconds ago)
- **Configurable Settings**: Adjust cooldown, rewind duration, and kill window timing through the game's settings menu
- **Visual & Audio Effects**: Distinctive effects that alert nearby players when you cast the spell
- **Custom Death Tracking**: Special attribution and icons for rewind-related kills
- **Network Synchronized**: Works seamlessly in multiplayer with server-authoritative position tracking

## Installation

### Prerequisites
1. **BepInEx 5.4.21+** - The mod framework for Unity games
2. **ModSync 1.0.6+** - For mod synchronization in multiplayer
3. **BlackMagicAPI 2.4.0+** - Core API for Mage Arena spell mods
4. **FishUtilities 1.2.4+** - Networking utilities for multiplayer functionality
5. **MageConfigurationAPI 1.3.1+** - Configuration system for mod settings

### Installation Steps
1. **Install BepInEx** for Mage Arena if you haven't already
2. **Install all required dependencies** listed above (available on Thunderstore)
3. **Download this mod** and extract the contents to your `BepInEx/plugins/ChronosRewind/` folder
4. **Verify installation** by checking that the following files are present:
   - `BepInEx/plugins/ChronosRewind/ChronosRewind.dll`
   - `BepInEx/plugins/ChronosRewind/chronomancer.bundle`
   - `BepInEx/plugins/ChronosRewind/manifest.json`
5. **Launch the game** - the mod will initialize automatically

### Multiplayer Requirements

**⚠️ IMPORTANT: ALL PLAYERS must have this mod installed for multiplayer compatibility.**

- **Server Host**: Must have the mod installed
- **All Clients**: Must have the mod installed with the same version
- **Reason**: The mod adds new spell types, network synchronization, and custom assets that all players need to see and interact with
- **ModSync Dependency**: Helps ensure all players have compatible mod versions
- **Consequences of Mismatch**: Players without the mod may experience crashes, missing spells, or desync issues

### Troubleshooting Installation
- **Spell not appearing**: Ensure all dependencies are installed and up to date
- **Missing effects**: Verify that `chronomancer.bundle` is in the mod directory
- **Configuration issues**: Check BepInEx console for error messages
- **Multiplayer sync issues**: Ensure all players have the same mod version

## Configuration

Access these settings through the game's configuration menu:

### Chronos Rewind Settings
- **Rewind Cooldown** (10-120 seconds, default: 45s)
  - Time between spell casts
  - Lower values make the spell more frequent but potentially overpowered
  - Higher values emphasize strategic timing

- **Rewind Duration** (1-10 seconds, default: 3s)
  - How far back in time to rewind your position and health
  - Longer durations provide more escape potential but use more memory
  - Shorter durations require more precise timing

- **Kill Window** (1-10 seconds, default: 3s)
  - Time window after casting recall for kills to count as "recall-related"
  - Affects death attribution and statistics tracking
  - Used for custom death messages and icons

- **Can Spawn In Chests** (default: false)
  - Controls whether Chronos Rewind spell can spawn naturally in team chests
  - Disabled by default to prevent the spell from being too common
  - Enable this if you want the spell to appear alongside other spells in chests

### Debug Settings
- **Enable Debug Mode** (default: false)
  - Enables detailed logging for troubleshooting
  - Only enable if experiencing issues or for development

## Usage

### Finding the Spell
- **By default**, the **Chronos Rewind** spell does **NOT** spawn in team chests
- **Enable "Can Spawn In Chests"** in the configuration if you want it to appear naturally in team chests
- When enabled, look for the distinctive cyan-blue glow when the spell appears in your inventory
- The spell follows standard Mage Arena spell mechanics for equipping and casting
- **Alternative**: Server admins can spawn the spell manually or through other mods

### Casting the Spell
1. **Equip** the Chronos Rewind spell in your spell slot
2. **Cast** the spell by saying "Chronos Rewind"
3. **Observe** the spell page, once it light up again you can recast
4. **Listen** for the distinctive audio cue that plays when casting

### Strategic Usage
- **Escape Tool**: Cast when low on health to return to a safer position with restored health
- **Repositioning**: Use to return to advantageous positions after aggressive plays
- **Baiting**: Advanced players can use recall to bait enemies into unfavorable positions
- **Team Fights**: Recall can help you re-engage fights from better angles

### Counterplay
- **Audio Cues**: Listen for the distinctive recall sound to know when enemies use the spell
- **Timing**: The spell has a significant cooldown - track when enemies use it
- **Positioning**: Be aware that recalled players return to their previous positions
- **Kill Window**: You have a brief window after someone rewinds to get "rewind-related" kills

## Technical Details

### Performance
- **Memory Usage**: Approximately 1-2MB per player for position history tracking
- **CPU Impact**: <1% additional CPU usage during normal gameplay
- **Network Traffic**: Minimal additional network overhead for synchronization

### Compatibility
- **Mod Compatibility**: Designed to work alongside other BlackMagicAPI spell mods
- **Game Version**: Compatible with current Mage Arena versions
- **Multiplayer**: Full multiplayer support with server-authoritative state management
- **Installation Requirement**: **ALL PLAYERS** must have the mod installed (not server-only)

## Known Issues

- **Rare Network Desync**: In extreme lag conditions, position tracking may briefly desync (auto-recovers)
- **Asset Loading**: If custom assets fail to load, the mod falls back to default game effects
- **Memory Usage**: Extended play sessions with many players may gradually increase memory usage

## Support

If you encounter issues:

1. **Check the BepInEx console** for error messages
2. **Verify all dependencies** are installed and up to date
3. **Try disabling other mods** to check for conflicts
4. **Enable debug mode** in the configuration for detailed logging
5. **Report issues** on the mod's GitHub page with console logs

## Version History

### 1.0.0 - Initial Release
- Core Chronos Rewind spell functionality
- Server-authoritative position tracking system
- Configurable cooldown, rewind duration, and kill window
- Visual and audio effects for spell casting
- Custom death tracking and attribution system
- Comprehensive error handling and fallback systems
- Performance optimization and memory management
- Full multiplayer synchronization support

## Credits

- **CheekyEntity** - Mod development
- **BlackMagicAPI Team** - Core spell framework
- **FishUtilities Team** - Networking framework
- **MageConfigurationAPI Team** - Configuration system
- **Mage Arena Community** - Testing and feedback

## License

This mod is released under the MIT License. See the included LICENSE file for details.