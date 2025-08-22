using System;
using System.Collections.Generic;
using UnityEngine;
using BlackMagicAPI.Managers;

namespace ChronoPara.Modules
{
    /// <summary>
    /// Handles death tracking and custom kill attribution for recall-related deaths
    /// Integrates with the game's death system to provide custom death icons and messages
    /// </summary>
    public static class DeathTracker
    {
        #region Constants
        
        /// <summary>
        /// Death reason string for recall-related kills
        /// This must match the string used when triggering player deaths
        /// </summary>
        public const string RECALL_DEATH_REASON = "temporal_recall";
        
        /// <summary>
        /// Custom death message for recall-related kills
        /// </summary>
        public const string RECALL_DEATH_MESSAGE = "fell victim to temporal manipulation";
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Dictionary tracking recent recall executions by player
        /// Key: Player GameObject instance ID, Value: Recall timestamp
        /// </summary>
        private static Dictionary<int, DateTime> recentRecalls = new Dictionary<int, DateTime>();
        
        /// <summary>
        /// Statistics tracking for recall-related deaths
        /// </summary>
        private static Dictionary<string, int> killStatistics = new Dictionary<string, int>();
        
        /// <summary>
        /// Flag indicating if the death icon has been registered
        /// </summary>
        private static bool isDeathIconRegistered = false;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets the total number of recall-related kills tracked
        /// </summary>
        public static int TotalRecallKills => killStatistics.ContainsKey(RECALL_DEATH_REASON) ? killStatistics[RECALL_DEATH_REASON] : 0;
        
        /// <summary>
        /// Gets whether the death tracking system is properly initialized
        /// </summary>
        public static bool IsInitialized => isDeathIconRegistered;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the death tracking system and register custom death icon
        /// </summary>
        public static void Initialize()
        {
            try
            {
                ChronoParaPlugin.Logger?.LogInfo("Initializing death tracking system...");
                
                // Register custom death icon with BlackMagicAPI
                RegisterCustomDeathIcon();
                
                // Initialize statistics dictionary
                InitializeStatistics();
                
                ChronoParaPlugin.Logger?.LogInfo("Death tracking system initialized successfully");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to initialize death tracking system: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register the custom death icon for recall-related kills
        /// </summary>
        private static void RegisterCustomDeathIcon()
        {
            try
            {
                // Get the recall kill icon from AssetManager
                Texture2D recallKillIcon = AssetManager.GetRecallKillIcon();
                
                if (recallKillIcon != null)
                {
                    // Register the death icon with BlackMagicAPI
                    BlackMagicManager.RegisterDeathIcon(
                        ChronoParaPlugin.Instance, 
                        RECALL_DEATH_REASON, 
                        recallKillIcon
                    );
                    
                    isDeathIconRegistered = true;
                    ChronoParaPlugin.Logger?.LogInfo($"Successfully registered custom death icon for '{RECALL_DEATH_REASON}'");
                }
                else
                {
                    ChronoParaPlugin.Logger?.LogWarning("Failed to get recall kill icon from AssetManager - using fallback registration");
                    
                    // Try to register with fallback icon
                    RegisterFallbackDeathIcon();
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to register custom death icon: {ex.Message}");
                
                // Try fallback registration
                RegisterFallbackDeathIcon();
            }
        }
        
        /// <summary>
        /// Register a fallback death icon if the custom asset is not available
        /// </summary>
        private static void RegisterFallbackDeathIcon()
        {
            try
            {
                // Create a simple fallback icon programmatically
                Texture2D fallbackIcon = CreateFallbackDeathIcon();
                
                BlackMagicManager.RegisterDeathIcon(
                    ChronoParaPlugin.Instance, 
                    RECALL_DEATH_REASON, 
                    fallbackIcon
                );
                
                isDeathIconRegistered = true;
                ChronoParaPlugin.Logger?.LogInfo($"Registered fallback death icon for '{RECALL_DEATH_REASON}'");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to register fallback death icon: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create a simple fallback death icon
        /// </summary>
        private static Texture2D CreateFallbackDeathIcon()
        {
            try
            {
                Texture2D icon = new Texture2D(32, 32);
                Color[] pixels = new Color[32 * 32];
                
                // Create a simple hourglass/time symbol
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        // Create hourglass shape
                        bool isHourglass = false;
                        
                        // Top and bottom rectangles
                        if ((y >= 26 && y <= 30 && x >= 8 && x <= 24) || 
                            (y >= 2 && y <= 6 && x >= 8 && x <= 24))
                        {
                            isHourglass = true;
                        }
                        // Middle constriction
                        else if (y >= 14 && y <= 18 && x >= 14 && x <= 18)
                        {
                            isHourglass = true;
                        }
                        // Sides
                        else if ((x == 8 || x == 24) && y >= 6 && y <= 26)
                        {
                            isHourglass = true;
                        }
                        
                        if (isHourglass)
                        {
                            pixels[y * 32 + x] = new Color(0.8f, 0.2f, 0.2f, 1.0f); // Red color
                        }
                        else
                        {
                            pixels[y * 32 + x] = Color.clear;
                        }
                    }
                }
                
                icon.SetPixels(pixels);
                icon.Apply();
                
                return icon;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to create fallback death icon: {ex.Message}");
                return Texture2D.whiteTexture;
            }
        }
        
        /// <summary>
        /// Initialize the kill statistics tracking system
        /// </summary>
        private static void InitializeStatistics()
        {
            try
            {
                // Initialize statistics counters
                killStatistics[RECALL_DEATH_REASON] = 0;
                killStatistics["total_deaths"] = 0;
                
                ChronoParaPlugin.Logger?.LogDebug("Kill statistics system initialized");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to initialize statistics: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Recall Tracking
        
        /// <summary>
        /// Record that a player has executed a recall
        /// This starts the kill window timer for death attribution
        /// </summary>
        /// <param name="player">The player GameObject that executed the recall</param>
        public static void RecordRecallExecution(GameObject player)
        {
            if (player == null)
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot record recall execution - player is null");
                return;
            }
            
            try
            {
                int playerId = player.GetInstanceID();
                DateTime recallTime = DateTime.Now;
                
                // Record the recall timestamp
                recentRecalls[playerId] = recallTime;
                
                ChronoParaPlugin.Logger?.LogDebug($"Recorded recall execution for player {player.name} (ID: {playerId}) at {recallTime}");
                
                // Clean up old recall records to prevent memory bloat
                CleanupOldRecallRecords();
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to record recall execution: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if a player death should be attributed to recall mechanics
        /// </summary>
        /// <param name="player">The player GameObject that died</param>
        /// <returns>True if the death is within the recall kill window</returns>
        public static bool IsRecallRelatedDeath(GameObject player)
        {
            if (player == null)
            {
                return false;
            }
            
            try
            {
                int playerId = player.GetInstanceID();
                
                // Check if this player has a recent recall record
                if (!recentRecalls.ContainsKey(playerId))
                {
                    return false;
                }
                
                DateTime recallTime = recentRecalls[playerId];
                double timeSinceRecall = (DateTime.Now - recallTime).TotalSeconds;
                double killWindow = ConfigManager.RecallKillWindow;
                
                bool isRecallRelated = timeSinceRecall <= killWindow;
                
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Death attribution check for {player.name}: Time since recall: {timeSinceRecall:F2}s, Kill window: {killWindow:F2}s, Is recall-related: {isRecallRelated}");
                }
                
                return isRecallRelated;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error checking recall-related death: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process a player death and handle recall-related attribution
        /// </summary>
        /// <param name="player">The player GameObject that died</param>
        /// <param name="killer">The killer's name (if applicable)</param>
        /// <returns>The death reason string to use for the kill feed</returns>
        public static string ProcessPlayerDeath(GameObject player, string killer = "")
        {
            if (player == null)
            {
                return "none"; // Default death reason
            }
            
            try
            {
                // Check if this death is recall-related
                if (IsRecallRelatedDeath(player))
                {
                    // Record the recall-related kill
                    RecordRecallKill(player, killer);
                    
                    // Clean up the recall record since it's been processed
                    int playerId = player.GetInstanceID();
                    recentRecalls.Remove(playerId);
                    
                    ChronoParaPlugin.Logger?.LogInfo($"Processed recall-related death for {player.name}");
                    
                    return RECALL_DEATH_REASON;
                }
                
                // Not a recall-related death, return default
                return "none";
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error processing player death: {ex.Message}");
                return "none";
            }
        }
        
        /// <summary>
        /// Record a recall-related kill in the statistics
        /// </summary>
        /// <param name="victim">The player who died</param>
        /// <param name="killer">The killer's name</param>
        private static void RecordRecallKill(GameObject victim, string killer)
        {
            try
            {
                // Increment recall kill counter
                if (killStatistics.ContainsKey(RECALL_DEATH_REASON))
                {
                    killStatistics[RECALL_DEATH_REASON]++;
                }
                else
                {
                    killStatistics[RECALL_DEATH_REASON] = 1;
                }
                
                // Increment total deaths counter
                if (killStatistics.ContainsKey("total_deaths"))
                {
                    killStatistics["total_deaths"]++;
                }
                else
                {
                    killStatistics["total_deaths"] = 1;
                }
                
                ChronoParaPlugin.Logger?.LogInfo($"Recorded recall kill - Victim: {victim.name}, Killer: {killer}, Total recall kills: {killStatistics[RECALL_DEATH_REASON]}");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to record recall kill: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up old recall records that are outside the kill window
        /// </summary>
        private static void CleanupOldRecallRecords()
        {
            try
            {
                DateTime cutoffTime = DateTime.Now.AddSeconds(-ConfigManager.RecallKillWindow * 2); // Double the kill window for safety
                List<int> expiredKeys = new List<int>();
                
                foreach (var kvp in recentRecalls)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
                
                foreach (int key in expiredKeys)
                {
                    recentRecalls.Remove(key);
                }
                
                if (expiredKeys.Count > 0 && ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Cleaned up {expiredKeys.Count} expired recall records");
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to cleanup old recall records: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Statistics and Reporting
        
        /// <summary>
        /// Get kill statistics for a specific death reason
        /// </summary>
        /// <param name="deathReason">The death reason to query</param>
        /// <returns>Number of kills for the specified reason</returns>
        public static int GetKillCount(string deathReason)
        {
            try
            {
                return killStatistics.ContainsKey(deathReason) ? killStatistics[deathReason] : 0;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error getting kill count for '{deathReason}': {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get a summary of all kill statistics
        /// </summary>
        /// <returns>Dictionary containing all kill statistics</returns>
        public static Dictionary<string, int> GetAllStatistics()
        {
            try
            {
                return new Dictionary<string, int>(killStatistics);
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error getting all statistics: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }
        
        /// <summary>
        /// Reset all kill statistics
        /// </summary>
        public static void ResetStatistics()
        {
            try
            {
                killStatistics.Clear();
                InitializeStatistics();
                ChronoParaPlugin.Logger?.LogInfo("Kill statistics reset");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to reset statistics: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a formatted string with current statistics
        /// </summary>
        /// <returns>Formatted statistics string</returns>
        public static string GetStatisticsReport()
        {
            try
            {
                var stats = GetAllStatistics();
                var report = "=== Chronomancer's Paradox Kill Statistics ===\n";
                
                foreach (var kvp in stats)
                {
                    string displayName = kvp.Key switch
                    {
                        RECALL_DEATH_REASON => "Recall-Related Deaths",
                        "total_deaths" => "Total Deaths Tracked",
                        _ => kvp.Key
                    };
                    
                    report += $"{displayName}: {kvp.Value}\n";
                }
                
                return report;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error generating statistics report: {ex.Message}");
                return "Error generating statistics report";
            }
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clean up resources when the mod is unloaded
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                recentRecalls?.Clear();
                killStatistics?.Clear();
                isDeathIconRegistered = false;
                
                ChronoParaPlugin.Logger?.LogDebug("DeathTracker cleanup completed");
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error during DeathTracker cleanup: {ex.Message}");
            }
        }
        
        #endregion
    }
}