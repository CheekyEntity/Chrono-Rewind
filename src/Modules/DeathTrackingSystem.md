# Death Tracking System Documentation

## Overview

The Death Tracking System provides custom kill attribution for recall-related deaths in the Chronomancer's Paradox mod. When a player dies within a configurable time window after using Temporal Recall, the death is attributed to the recall mechanic and displays a custom death icon.

## Components

### 1. DeathTracker (Static Class)
**Location:** `ChronoPara.Modules.DeathTracker`

**Responsibilities:**
- Register custom death icon with BlackMagicAPI
- Track recent recall executions by player
- Determine if deaths are recall-related based on timing
- Maintain kill statistics and categorization
- Provide fallback death icon if custom asset unavailable

**Key Methods:**
- `Initialize()` - Sets up death icon registration and statistics
- `RecordRecallExecution(GameObject player)` - Records when a player uses recall
- `IsRecallRelatedDeath(GameObject player)` - Checks if death is within kill window
- `ProcessPlayerDeath(GameObject player, string killer)` - Processes death and returns appropriate death reason
- `GetStatisticsReport()` - Returns formatted statistics string

### 2. PlayerRespawnManagerPatch (Harmony Patch)
**Location:** `ChronoPara.Patches.PlayerRespawnManagerPatch`

**Responsibilities:**
- Intercept death messages before they're displayed
- Apply custom death attribution for recall-related deaths
- Find player GameObjects by name for correlation
- Log death processing for debugging

**Patched Method:**
- `PlayerRespawnManager.summonDeathMessage()` - Prefix and Postfix patches

### 3. Integration with PositionTracker
**Location:** `ChronoPara.Modules.PositionTracker.ExecuteRecall()`

**Integration Point:**
When a recall is successfully executed, `DeathTracker.RecordRecallExecution()` is called to start the kill window timer.

## Configuration

The death tracking system uses the following configuration values:

- **Recall Kill Window** (`RecallKillWindow`): Time in seconds after recall during which deaths are considered recall-related
  - Default: 3.0 seconds
  - Range: 1.0 - 10.0 seconds
  - Configurable via MageConfigurationAPI

## Death Attribution Flow

1. **Recall Execution:**
   - Player casts Temporal Recall spell
   - PositionTracker executes recall successfully
   - DeathTracker records recall timestamp for player

2. **Death Detection:**
   - Player dies from any cause
   - PlayerRespawnManagerPatch intercepts `summonDeathMessage()`
   - DeathTracker checks if death occurred within kill window
   - If yes, death reason is changed to `"temporal_recall"`

3. **Death Display:**
   - Game displays death message with custom icon
   - Statistics are updated
   - Kill feed shows recall-related death

## Custom Death Icon

**Asset:** `RecallKill_Icon.png` (64x64 pixels)
**Death Reason:** `"temporal_recall"`
**Fallback:** Programmatically generated hourglass icon

The custom death icon is registered with BlackMagicAPI during mod initialization. If the custom asset is not available, a fallback icon is created programmatically.

## Statistics Tracking

The system tracks the following statistics:

- **Recall-Related Deaths:** Number of deaths attributed to temporal recall
- **Total Deaths Tracked:** Total number of deaths processed by the system

Statistics can be accessed via:
- `DeathTracker.GetKillCount(string deathReason)`
- `DeathTracker.GetAllStatistics()`
- `DeathTracker.GetStatisticsReport()`

## Error Handling

The system includes comprehensive error handling:

- **Null Player Objects:** Gracefully handled, returns default death reason
- **Missing Components:** Logged warnings, continues with fallback behavior
- **Asset Loading Failures:** Falls back to programmatically generated icons
- **Patch Failures:** Continues with original game behavior to prevent crashes

## Testing

**Test Class:** `ChronoPara.Modules.Tests.DeathTrackerTests`

**Test Coverage:**
- Initialization and cleanup
- Recall tracking functionality
- Kill attribution logic
- Statistics tracking
- Edge cases and error handling
- Kill window timing

Tests run automatically when debug mode is enabled in the mod configuration.

## Debug Information

When debug mode is enabled, the system logs:
- Death message interceptions
- Kill attribution decisions
- Statistics updates
- Player object correlations
- Error conditions

## Integration Notes

### For Other Mod Developers

If you need to integrate with the death tracking system:

1. **Check if a death is recall-related:**
   ```csharp
   bool isRecallDeath = DeathTracker.IsRecallRelatedDeath(playerObject);
   ```

2. **Get current statistics:**
   ```csharp
   int recallKills = DeathTracker.GetKillCount(DeathTracker.RECALL_DEATH_REASON);
   ```

3. **Listen for recall executions:**
   The system automatically tracks recalls when `PositionTracker.ExecuteRecall()` is called.

### Performance Considerations

- Recall tracking uses a Dictionary with automatic cleanup
- Statistics are stored in memory only (not persisted)
- Harmony patches have minimal performance impact
- Player object lookups are optimized with caching

## Troubleshooting

**Common Issues:**

1. **Custom death icon not showing:**
   - Check if AssetBundle is loaded correctly
   - Verify RecallKill_Icon.png is in the bundle
   - Check BepInEx logs for asset loading errors

2. **Deaths not being attributed to recall:**
   - Verify kill window configuration
   - Check if PositionTracker is properly attached to players
   - Enable debug mode to see attribution decisions

3. **Statistics not updating:**
   - Ensure DeathTracker.Initialize() was called successfully
   - Check for exceptions in death processing
   - Verify Harmony patches are applied correctly

**Debug Commands:**

Enable debug mode in configuration to get detailed logging of the death tracking system operation.