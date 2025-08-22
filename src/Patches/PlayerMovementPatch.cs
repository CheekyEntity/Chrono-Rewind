using HarmonyLib;
using UnityEngine;
using ChronoPara.Modules;
using System;
using System.Collections;

namespace ChronoPara.Patches
{
    /// <summary>
    /// Harmony patches for PlayerMovement to automatically attach PositionTracker components
    /// This ensures all players have position tracking enabled for Temporal Recall functionality
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement))]
    public static class PlayerMovementPatch
    {
        /// <summary>
        /// Postfix patch for OnStartClient to automatically attach PositionTracker component
        /// This runs after the original OnStartClient method completes successfully
        /// </summary>
        /// <param name="__instance">The PlayerMovement instance being patched</param>
        [HarmonyPatch("OnStartClient")]
        [HarmonyPostfix]
        public static void OnStartClient_Postfix(PlayerMovement __instance)
        {
            try
            {
                if (__instance == null || __instance.gameObject == null)
                {
                    ChronoParaPlugin.Logger?.LogWarning("OnStartClient_Postfix called with null PlayerMovement instance");
                    return;
                }

                ChronoParaPlugin.Logger?.LogDebug($"OnStartClient_Postfix called for player: {__instance.gameObject.name}");

                // Check if PositionTracker is already attached to prevent duplicates
                PositionTracker existingTracker = __instance.gameObject.GetComponent<PositionTracker>();
                if (existingTracker != null)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"PositionTracker already exists on {__instance.gameObject.name}");
                    return;
                }

                // Ensure we don't interfere with other mods by checking for conflicting components
                if (HasConflictingComponents(__instance.gameObject))
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Conflicting components detected on {__instance.gameObject.name} - skipping PositionTracker attachment");
                    return;
                }

                // Attempt to attach PositionTracker component
                AttachPositionTracker(__instance.gameObject);
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error in OnStartClient_Postfix: {ex.Message}");
                
                // Schedule retry attempt to handle transient failures
                if (__instance != null && __instance.gameObject != null)
                {
                    ScheduleRetryAttachment(__instance.gameObject);
                }
            }
        }

        /// <summary>
        /// Attempts to attach a PositionTracker component to the specified player GameObject
        /// Includes validation and error handling for component attachment
        /// </summary>
        /// <param name="playerObject">The player GameObject to attach the component to</param>
        /// <returns>True if attachment was successful, false otherwise</returns>
        private static bool AttachPositionTracker(GameObject playerObject)
        {
            try
            {
                if (playerObject == null)
                {
                    ChronoParaPlugin.Logger?.LogError("Cannot attach PositionTracker - player GameObject is null");
                    return false;
                }

                // Validate that this is actually a player object
                PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
                if (playerMovement == null)
                {
                    ChronoParaPlugin.Logger?.LogWarning($"GameObject {playerObject.name} does not have PlayerMovement component - skipping PositionTracker attachment");
                    return false;
                }

                // Check if PositionTracker is already attached
                PositionTracker existingTracker = playerObject.GetComponent<PositionTracker>();
                if (existingTracker != null)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"PositionTracker already attached to {playerObject.name}");
                    return true; // Already attached, consider this success
                }

                // Attempt to add PositionTracker component
                PositionTracker newTracker = playerObject.AddComponent<PositionTracker>();
                if (newTracker == null)
                {
                    ChronoParaPlugin.Logger?.LogError($"Failed to add PositionTracker component to {playerObject.name}");
                    return false;
                }

                ChronoParaPlugin.Logger?.LogInfo($"Successfully attached PositionTracker to player: {playerObject.name}");
                
                // Validate that the component was properly initialized
                if (!ValidatePositionTracker(newTracker))
                {
                    ChronoParaPlugin.Logger?.LogWarning($"PositionTracker attached but validation failed for {playerObject.name}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Exception while attaching PositionTracker to {playerObject?.name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks for components that might conflict with PositionTracker functionality
        /// This helps prevent mod conflicts and ensures compatibility
        /// </summary>
        /// <param name="playerObject">The player GameObject to check</param>
        /// <returns>True if conflicting components are detected</returns>
        private static bool HasConflictingComponents(GameObject playerObject)
        {
            try
            {
                // Check for other temporal/recall-related components from other mods
                // This is a placeholder for future compatibility checks
                // Currently, we don't know of any specific conflicting components
                
                // Log all components for debugging if in debug mode
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    Component[] allComponents = playerObject.GetComponents<Component>();
                    string componentList = string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name));
                    ChronoParaPlugin.Logger?.LogDebug($"Components on {playerObject.name}: {componentList}");
                }

                // For now, no known conflicts - return false
                return false;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error checking for conflicting components: {ex.Message}");
                return false; // Assume no conflicts on error
            }
        }

        /// <summary>
        /// Validates that a PositionTracker component is properly initialized and functional
        /// </summary>
        /// <param name="tracker">The PositionTracker component to validate</param>
        /// <returns>True if the component is valid and functional</returns>
        private static bool ValidatePositionTracker(PositionTracker tracker)
        {
            try
            {
                if (tracker == null)
                {
                    ChronoParaPlugin.Logger?.LogError("PositionTracker validation failed - component is null");
                    return false;
                }

                if (tracker.gameObject == null)
                {
                    ChronoParaPlugin.Logger?.LogError("PositionTracker validation failed - GameObject is null");
                    return false;
                }

                // Check if the component has the required PlayerMovement reference
                PlayerMovement playerMovement = tracker.gameObject.GetComponent<PlayerMovement>();
                if (playerMovement == null)
                {
                    ChronoParaPlugin.Logger?.LogError($"PositionTracker validation failed - no PlayerMovement component on {tracker.gameObject.name}");
                    return false;
                }

                // Additional validation can be added here as needed
                // For now, basic component existence is sufficient

                ChronoParaPlugin.Logger?.LogDebug($"PositionTracker validation passed for {tracker.gameObject.name}");
                return true;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Exception during PositionTracker validation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Schedules a retry attempt for PositionTracker attachment after a delay
        /// This handles cases where initial attachment fails due to timing or initialization issues
        /// </summary>
        /// <param name="playerObject">The player GameObject to retry attachment for</param>
        private static void ScheduleRetryAttachment(GameObject playerObject)
        {
            try
            {
                if (playerObject == null)
                {
                    ChronoParaPlugin.Logger?.LogWarning("Cannot schedule retry - player GameObject is null");
                    return;
                }

                ChronoParaPlugin.Logger?.LogDebug($"Scheduling retry attachment for {playerObject.name}");

                // Use a coroutine to retry after a short delay
                MonoBehaviour coroutineRunner = playerObject.GetComponent<PlayerMovement>();
                if (coroutineRunner != null)
                {
                    coroutineRunner.StartCoroutine(RetryAttachmentCoroutine(playerObject));
                }
                else
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Cannot schedule retry - no MonoBehaviour found on {playerObject.name}");
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error scheduling retry attachment: {ex.Message}");
            }
        }

        /// <summary>
        /// Coroutine that retries PositionTracker attachment after a delay
        /// Implements exponential backoff for multiple retry attempts
        /// </summary>
        /// <param name="playerObject">The player GameObject to retry attachment for</param>
        /// <returns>IEnumerator for coroutine execution</returns>
        private static IEnumerator RetryAttachmentCoroutine(GameObject playerObject)
        {
            const int maxRetries = 3;
            const float baseDelay = 1.0f;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // Wait with exponential backoff
                float delay = baseDelay * Mathf.Pow(2, attempt - 1);
                yield return new WaitForSeconds(delay);

                // Check if the object still exists
                if (playerObject == null)
                {
                    ChronoParaPlugin.Logger?.LogDebug("Retry attachment cancelled - player object was destroyed");
                    yield break;
                }

                // Check if PositionTracker was already attached by another process
                PositionTracker existingTracker = playerObject.GetComponent<PositionTracker>();
                if (existingTracker != null)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Retry attachment cancelled - PositionTracker already exists on {playerObject.name}");
                    yield break;
                }

                // Attempt attachment (handle exceptions without try-catch in coroutine)
                bool success = false;
                try
                {
                    success = AttachPositionTracker(playerObject);
                }
                catch (Exception ex)
                {
                    ChronoParaPlugin.Logger?.LogError($"Exception during retry attachment attempt {attempt}: {ex.Message}");
                    success = false;
                }

                if (success)
                {
                    ChronoParaPlugin.Logger?.LogInfo($"Retry attachment successful for {playerObject.name} (attempt {attempt})");
                    yield break;
                }
                else
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Retry attachment failed for {playerObject.name} (attempt {attempt}/{maxRetries})");
                }
            }

            // All retries exhausted
            ChronoParaPlugin.Logger?.LogError($"All retry attempts exhausted for PositionTracker attachment on {playerObject?.name}");
        }

        /// <summary>
        /// Prefix patch to log OnStartClient method calls for debugging
        /// This helps track when players are being initialized
        /// </summary>
        /// <param name="__instance">The PlayerMovement instance being patched</param>
        [HarmonyPatch("OnStartClient")]
        [HarmonyPrefix]
        public static void OnStartClient_Prefix(PlayerMovement __instance)
        {
            try
            {
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    string playerName = __instance?.gameObject?.name ?? "Unknown";
                    ChronoParaPlugin.Logger?.LogDebug($"PlayerMovement.OnStartClient called for: {playerName}");
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error in OnStartClient_Prefix: {ex.Message}");
            }
        }

        /// <summary>
        /// Utility method to get statistics about PositionTracker attachment across all players
        /// Useful for debugging and monitoring
        /// </summary>
        /// <returns>String containing attachment statistics</returns>
        public static string GetAttachmentStatistics()
        {
            try
            {
                PlayerMovement[] allPlayers = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                int totalPlayers = allPlayers.Length;
                int playersWithTracker = 0;
                int playersWithoutTracker = 0;

                foreach (PlayerMovement player in allPlayers)
                {
                    if (player != null && player.gameObject != null)
                    {
                        PositionTracker tracker = player.gameObject.GetComponent<PositionTracker>();
                        if (tracker != null)
                        {
                            playersWithTracker++;
                        }
                        else
                        {
                            playersWithoutTracker++;
                        }
                    }
                }

                return $"PositionTracker Attachment Statistics:\n" +
                       $"  Total Players: {totalPlayers}\n" +
                       $"  With PositionTracker: {playersWithTracker}\n" +
                       $"  Without PositionTracker: {playersWithoutTracker}\n" +
                       $"  Attachment Rate: {(totalPlayers > 0 ? (playersWithTracker * 100.0f / totalPlayers):0):F1}%";
            }
            catch (Exception ex)
            {
                return $"Error generating attachment statistics: {ex.Message}";
            }
        }
    }
}