using HarmonyLib;
using UnityEngine;
using ChronoPara.Modules;

namespace ChronoPara.Patches
{
    /// <summary>
    /// Harmony patches for PlayerRespawnManager to intercept death messages and apply custom kill attribution
    /// </summary>
    [HarmonyPatch(typeof(PlayerRespawnManager))]
    public static class PlayerRespawnManagerPatch
    {
        /// <summary>
        /// Prefix patch for summonDeathMessage to intercept and modify death attribution
        /// This allows us to detect recall-related deaths and apply custom death reasons
        /// </summary>
        /// <param name="name">The name of the player who died</param>
        /// <param name="causeofdeath">The original cause of death (will be modified if recall-related)</param>
        /// <param name="killer">The name of the killer (if applicable)</param>
        /// <returns>True to continue with original method, false to skip it</returns>
        [HarmonyPatch("summonDeathMessage")]
        [HarmonyPrefix]
        public static bool SummonDeathMessage_Prefix(ref string name, ref string causeofdeath, ref string killer)
        {
            try
            {
                ChronoParaPlugin.Logger?.LogDebug($"Death message intercepted - Player: {name}, Cause: {causeofdeath}, Killer: {killer}");
                
                // Find the player GameObject by name to check for recall-related death
                GameObject playerObject = FindPlayerByName(name);
                
                if (playerObject != null)
                {
                    // Check if this death should be attributed to recall mechanics
                    string processedCause = DeathTracker.ProcessPlayerDeath(playerObject, killer);
                    
                    if (processedCause != "none" && processedCause != causeofdeath)
                    {
                        // This is a recall-related death, update the cause
                        string originalCause = causeofdeath;
                        causeofdeath = processedCause;
                        
                        ChronoParaPlugin.Logger?.LogInfo($"Death attribution changed for {name}: {originalCause} -> {causeofdeath}");
                    }
                }
                else
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Could not find player GameObject for death attribution: {name}");
                }
                
                // Continue with the original method using the potentially modified causeofdeath
                return true;
            }
            catch (System.Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error in summonDeathMessage patch: {ex.Message}");
                
                // Continue with original method on error to prevent breaking the game
                return true;
            }
        }
        
        /// <summary>
        /// Find a player GameObject by their display name
        /// This is needed to correlate death messages with actual player objects for recall tracking
        /// </summary>
        /// <param name="playerName">The display name of the player</param>
        /// <returns>The player GameObject, or null if not found</returns>
        private static GameObject FindPlayerByName(string playerName)
        {
            try
            {
                // Find all PlayerMovement components in the scene
                PlayerMovement[] allPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                
                foreach (PlayerMovement player in allPlayers)
                {
                    if (player != null && player.gameObject != null)
                    {
                        // Check if the player name matches
                        // PlayerMovement has a 'playername' field that should match the death message name
                        try
                        {
                            // Use reflection to access the playername field
                            var nameField = typeof(PlayerMovement).GetField("playername", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            if (nameField != null)
                            {
                                string playerMovementName = (string)nameField.GetValue(player);
                                
                                if (string.Equals(playerMovementName, playerName, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    ChronoParaPlugin.Logger?.LogDebug($"Found player GameObject for {playerName}");
                                    return player.gameObject;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"Error accessing player name field: {ex.Message}");
                        }
                        
                        // Fallback: check GameObject name
                        if (player.gameObject.name.Contains(playerName) || playerName.Contains(player.gameObject.name))
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"Found player GameObject by name matching: {playerName} -> {player.gameObject.name}");
                            return player.gameObject;
                        }
                    }
                }
                
                ChronoParaPlugin.Logger?.LogDebug($"Player GameObject not found for name: {playerName}");
                return null;
            }
            catch (System.Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error finding player by name '{playerName}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Postfix patch for summonDeathMessage to log successful death message processing
        /// This helps with debugging and statistics tracking
        /// </summary>
        /// <param name="name">The name of the player who died</param>
        /// <param name="causeofdeath">The final cause of death used</param>
        /// <param name="killer">The name of the killer</param>
        [HarmonyPatch("summonDeathMessage")]
        [HarmonyPostfix]
        public static void SummonDeathMessage_Postfix(string name, string causeofdeath, string killer)
        {
            try
            {
                // If this was a recall-related death, log it for statistics
                if (causeofdeath == DeathTracker.RECALL_DEATH_REASON)
                {
                    ChronoParaPlugin.Logger?.LogInfo($"Recall-related death processed: {name} killed by {killer}");
                }
            }
            catch (System.Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error in summonDeathMessage postfix: {ex.Message}");
            }
        }
    }
}