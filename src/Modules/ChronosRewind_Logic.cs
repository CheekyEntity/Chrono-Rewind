using UnityEngine;
using BlackMagicAPI.Modules.Spells;

namespace ChronoPara.Modules
{
    /// <summary>
    /// Spell logic implementation for the Chronos Rewind spell
    /// Handles spell casting behavior and player interaction
    /// </summary>
    internal class ChronosRewind_Logic : SpellLogic
    {
        /// <summary>
        /// Executes the spell casting logic when the player casts Chronos Rewind
        /// </summary>
        /// <param name="playerObj">The player GameObject casting the spell</param>
        /// <param name="page">The spell page controller</param>
        /// <param name="spawnPos">The spawn position for the spell</param>
        /// <param name="viewDirectionVector">The player's view direction</param>
        /// <param name="castingLevel">The casting level of the spell</param>
        public override void CastSpell(GameObject playerObj, PageController page, 
                                     Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
        {
            try
            {
                // Validate player object
                if (playerObj == null)
                {
                    ChronoParaPlugin.Logger.LogError("Cannot cast Chronos Rewind: player object is null");
                    return;
                }
                
                // Get PlayerMovement component to check ownership
                var playerMovement = playerObj.GetComponent<PlayerMovement>();
                if (playerMovement == null)
                {
                    ChronoParaPlugin.Logger.LogError("Cannot cast Chronos Rewind: PlayerMovement component not found");
                    return;
                }
                
                // Only process for local player to prevent duplicate requests
                if (!playerMovement.IsOwner)
                {
                    ChronoParaPlugin.Logger.LogDebug("Ignoring Chronos Rewind cast from non-owner player");
                    return;
                }
                
                // Get PositionTracker component
                var positionTracker = playerObj.GetComponent<PositionTracker>();
                if (positionTracker == null)
                {
                    ChronoParaPlugin.Logger.LogError("Cannot cast Chronos Rewind: PositionTracker component not found on player");
                    return;
                }
                
                // Delegate recall logic to PositionTracker
                ChronoParaPlugin.Logger.LogDebug($"Casting Chronos Rewind for player {playerObj.name}");
                positionTracker.RequestRecall();
            }
            catch (System.Exception ex)
            {
                ChronoParaPlugin.Logger.LogError($"Error during Chronos Rewind cast: {ex.Message}");
            }
        }
    }
}