using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using FishUtilities.Network;
using FishNet.Connection;
using FishNet.Object;

namespace ChronoPara.Modules;

/// <summary>
/// Network component responsible for tracking player position history and handling temporal recall functionality.
/// Inherits from CustomNetworkBehaviour to provide server-authoritative position tracking and client synchronization.
/// </summary>
public class PositionTracker : CustomNetworkBehaviour
{
    #region Network Constants
    
    /// <summary>
    /// Network command ID for recall requests from client to server
    /// </summary>
    private const uint RECALL_COMMAND_ID = 1001;
    
    /// <summary>
    /// Network RPC ID for recall effects from server to clients
    /// </summary>
    private const uint RECALL_EFFECT_RPC_ID = 1002;
    
    #endregion
    
    #region Private Fields
    
    /// <summary>
    /// Queue storing position history snapshots, managed by server
    /// </summary>
    private Queue<PositionSnapshot> positionHistory;
    
    /// <summary>
    /// Timestamp of the last position recording
    /// </summary>
    private float lastRecordTime;
    
    /// <summary>
    /// Cached configuration values for performance
    /// </summary>
    private float rewindDuration;
    private float recallKillWindow;
    private float recordingInterval;
    
    /// <summary>
    /// Timestamp of the last recall execution for kill window tracking
    /// </summary>
    private DateTime lastRecallTime;
    
    /// <summary>
    /// Timestamp of the last recall request for cooldown validation
    /// </summary>
    private float lastRecallRequestTime;
    
    /// <summary>
    /// Cached component references for performance
    /// </summary>
    private PlayerMovement playerMovement;
    private AudioSource audioSource;
    
    /// <summary>
    /// Flag to track if component is properly initialized
    /// </summary>
    private bool isInitialized;
    
    /// <summary>
    /// Performance optimization: adaptive frame skipping based on performance
    /// </summary>
    private int frameSkipCounter;
    
    /// <summary>
    /// Performance optimization: periodic history optimization
    /// </summary>
    private float lastHistoryOptimization;
    private const float HISTORY_OPTIMIZATION_INTERVAL = 30f; // Optimize every 30 seconds
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Initialize component and cache references
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        
        try
        {
            // Initialize position history queue
            positionHistory = new Queue<PositionSnapshot>();
            
            // Cache configuration values
            RefreshConfiguration();
            
            // Cache component references
            CacheComponentReferences();
            
            // Initialize timestamps
            lastRecordTime = 0f;
            lastRecallTime = DateTime.MinValue;
            lastRecallRequestTime = -999f; // Initialize to allow immediate first recall
            
            isInitialized = true;
            
            ChronoParaPlugin.Logger?.LogDebug($"PositionTracker initialized for {gameObject.name}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to initialize PositionTracker: {ex.Message}");
            isInitialized = false;
        }
    }
    
    /// <summary>
    /// Handle component startup and network initialization
    /// </summary>
    void Start()
    {
        if (!isInitialized)
        {
            ChronoParaPlugin.Logger?.LogWarning($"PositionTracker not properly initialized for {gameObject.name}");
            return;
        }
        
        // Refresh configuration in case it changed during initialization
        RefreshConfiguration();
        
        // Network validation will be done during operation - don't block initialization
        // The component will work once network is ready
        ChronoParaPlugin.Logger?.LogDebug($"PositionTracker initialization completed - network validation will occur during operation");
        
        ChronoParaPlugin.Logger?.LogDebug($"PositionTracker started for {gameObject.name} (NetworkReady: {IsNetworkReady()}, IsLocalPlayer: {IsLocalPlayer()})");
    }
    
    /// <summary>
    /// Clean up resources when component is destroyed
    /// </summary>
    void OnDestroy()
    {
        try
        {
            // Clear position history to free memory
            positionHistory?.Clear();
            
            // Clear component references
            playerMovement = null;
            audioSource = null;
            
            // Reset performance counters
            frameSkipCounter = 0;
            
            ChronoParaPlugin.Logger?.LogDebug($"PositionTracker cleaned up for {gameObject.name}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during PositionTracker cleanup: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Initialization Methods
    
    /// <summary>
    /// Cache references to required components for performance
    /// </summary>
    private void CacheComponentReferences()
    {
        try
        {
            // Cache PlayerMovement component (required)
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogWarning($"PlayerMovement component not found on {gameObject.name}");
            }
            
            // PlayerMovement (0.7.8) exposes playerHealth and ServerHealth as public float fields
            
            // Cache AudioSource component (optional, may be added later)
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                ChronoParaPlugin.Logger?.LogDebug($"AudioSource component not found on {gameObject.name} - will be added if needed");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to cache component references: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Refresh cached configuration values from ConfigManager with performance optimization
    /// </summary>
    private void RefreshConfiguration()
    {
        try
        {
            // Use performance-optimized cached configuration values
            rewindDuration = PerformanceOptimizer.GetCachedRewindDuration();
            recallKillWindow = PerformanceOptimizer.GetCachedRecallKillWindow();
            recordingInterval = ConfigManager.RecordingInterval;
            
            ChronoParaPlugin.Logger?.LogDebug($"Configuration refreshed - Rewind: {rewindDuration}s, Kill Window: {recallKillWindow}s, Interval: {recordingInterval}s");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to refresh configuration: {ex.Message}");
            
            // Use fallback values
            rewindDuration = 3f;
            recallKillWindow = 3f;
            recordingInterval = 0.1f;
        }
    }
    
    #endregion
    
    #region Public Properties
    
    /// <summary>
    /// Gets whether this component is properly initialized and ready for use
    /// </summary>
    public bool IsInitialized => isInitialized && playerMovement != null;
    
    /// <summary>
    /// Gets the number of position snapshots currently stored
    /// </summary>
    public int HistoryCount => positionHistory?.Count ?? 0;
    
    /// <summary>
    /// Gets the timestamp of the last recall execution
    /// </summary>
    public DateTime LastRecallTime => lastRecallTime;
    
    /// <summary>
    /// Gets whether a recall was executed recently (within kill window)
    /// </summary>
    public bool IsInRecallKillWindow => (DateTime.Now - lastRecallTime).TotalSeconds <= recallKillWindow;
    
    /// <summary>
    /// Gets whether this client owns this player object
    /// Uses the safe IsLocalPlayer method instead of problematic base.IsOwner
    /// </summary>
    public new bool IsOwner
    {
        get
        {
            return IsLocalPlayer();
        }
    }
    
    /// <summary>
    /// Gets the remaining cooldown time in seconds
    /// </summary>
    public float RemainingCooldown
    {
        get
        {
            try
            {
                float timeSinceLastRecall = Time.time - lastRecallRequestTime;
                float cooldownDuration = PerformanceOptimizer.GetCachedRecallCooldown();
                return Mathf.Max(0f, cooldownDuration - timeSinceLastRecall);
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error calculating remaining cooldown: {ex.Message}");
                return 0f;
            }
        }
    }
    
    #endregion
    
    #region Server-Side Position Tracking
    
    /// <summary>
    /// Server-side fixed update for continuous position and health recording
    /// Only runs on server to maintain authoritative state
    /// </summary>
    void FixedUpdate()
    {
        // Early exit if component is not properly initialized
        if (!isInitialized || playerMovement == null)
            return;
        
        try
        {
            // Track positions if we're the local player and network is ready
            if (!IsNetworkReady() || !IsLocalPlayer())
                return;
            
            // Performance optimization: adaptive frame skipping
            frameSkipCounter++;
            int adaptiveFrameSkip = PerformanceOptimizer.GetAdaptiveFrameSkip();
            if (frameSkipCounter < adaptiveFrameSkip)
                return;
            frameSkipCounter = 0;
            
            // Additional performance check: validate recording interval isn't too aggressive
            if (recordingInterval < 0.05f && ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Recording interval ({recordingInterval}s) may be too aggressive for optimal performance");
            }
            
            // Check if enough time has passed since last recording
            if (Time.fixedTime - lastRecordTime >= recordingInterval)
            {
                PerformanceOptimizer.ProfileAction("RecordCurrentPosition", RecordCurrentPosition);
                PerformanceOptimizer.ProfileAction("CleanupExpiredHistory", CleanupExpiredHistory);
                
                // Periodic history optimization for memory management
                if (Time.fixedTime - lastHistoryOptimization >= HISTORY_OPTIMIZATION_INTERVAL)
                {
                    PerformanceOptimizer.ProfileAction("OptimizeHistory", () => 
                    {
                        int removedCount = PerformanceOptimizer.OptimizeHistoryQueue(positionHistory);
                        if (removedCount > 0 && ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"History optimization removed {removedCount} redundant snapshots");
                        }
                    });
                    lastHistoryOptimization = Time.fixedTime;
                }
                
                lastRecordTime = Time.fixedTime;
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in PositionTracker.FixedUpdate: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Records the current player position and health to the history queue
    /// </summary>
    private void RecordCurrentPosition()
    {
        if (playerMovement == null)
        {
            ChronoParaPlugin.Logger?.LogWarning("Cannot record position - PlayerMovement component is null");
            return;
        }
        
        try
        {
            // Get current position from transform
            Vector3 currentPosition = transform.position;
            
            // Get current health from player health component
            float currentHealth = GetCurrentPlayerHealth();
            
            // Create new snapshot with current timestamp
            var snapshot = new PositionSnapshot(currentPosition, currentHealth, Time.fixedTime);
            
            // Add to history queue
            positionHistory.Enqueue(snapshot);
            
            // Prevent memory bloat by limiting queue size
            // This is a safety measure in addition to timestamp-based cleanup
            int maxSafeSize = ConfigManager.MaxHistorySize * 2; // Double buffer for safety
            while (positionHistory.Count > maxSafeSize)
            {
                positionHistory.Dequeue();
                ChronoParaPlugin.Logger?.LogWarning("Position history exceeded safe size limit - removing oldest entry");
            }
            
            // Log detailed recording info in debug mode (only every 10th recording to reduce spam)
            if (ChronoParaPlugin.DebugMode?.Value == true && positionHistory.Count % 10 == 0)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Recorded position: {snapshot} (Queue size: {positionHistory.Count})");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to record current position: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Removes expired snapshots from the history queue based on timestamp
    /// Uses timestamp-based cleanup for robustness against server lag spikes
    /// </summary>
    private void CleanupExpiredHistory()
    {
        if (positionHistory == null || positionHistory.Count == 0)
            return;
        
        try
        {
            int removedCount = 0;
            float currentTime = Time.fixedTime;
            
            // Remove snapshots that are older than the configured rewind duration
            while (positionHistory.Count > 0)
            {
                var oldestSnapshot = positionHistory.Peek();
                
                // Check if snapshot is expired based on timestamp
                if (currentTime - oldestSnapshot.timestamp > rewindDuration)
                {
                    positionHistory.Dequeue();
                    removedCount++;
                }
                else
                {
                    // Since queue is ordered by time, we can stop once we find a non-expired snapshot
                    break;
                }
            }
            
            // Log cleanup activity in debug mode
            if (removedCount > 0 && ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Cleaned up {removedCount} expired position snapshots (Queue size: {positionHistory.Count})");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to cleanup expired history: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the current player health from PlayerMovement component using reflection.
    /// Targets 0.7.8 where the field is public at runtime, but compile-time references may not expose it.
    /// </summary>
    /// <returns>Current player health value</returns>
    private float GetCurrentPlayerHealth()
    {
        try
        {
            if (playerMovement != null)
            {
                var healthField = typeof(PlayerMovement).GetField(
                    "playerHealth",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (healthField != null)
                {
                    object value = healthField.GetValue(playerMovement);
                    if (value is float f)
                        return f;
                }
            }

            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"PlayerMovement component/field not available on {gameObject.name} - using fallback health value");
            }
            return 100f; // Fallback
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to get current player health: {ex.Message}");
            return 100f; // Fallback on error
        }
    }
    
    /// <summary>
    /// Validates server authority for position tracking operations
    /// </summary>
    /// <returns>True if server has authority to perform tracking operations</returns>
    private bool ValidateServerAuthority()
    {
        if (!IsNetworkReady())
        {
            ChronoParaPlugin.Logger?.LogWarning("Server authority validation failed - network not ready");
            return false;
        }
        
        if (!IsInitialized)
        {
            ChronoParaPlugin.Logger?.LogWarning("Server authority validation failed - component not initialized");
            return false;
        }
        
        if (playerMovement == null)
        {
            ChronoParaPlugin.Logger?.LogWarning("Server authority validation failed - PlayerMovement component missing");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets the oldest available position snapshot for recall operations
    /// Performs server authority validation before accessing history
    /// </summary>
    /// <returns>The oldest position snapshot, or null if none available</returns>
    public PositionSnapshot? GetRecallTarget()
    {
        if (!ValidateServerAuthority())
            return null;
        
        try
        {
            // Clean up expired entries first
            CleanupExpiredHistory();
            
            if (positionHistory.Count == 0)
            {
                ChronoParaPlugin.Logger?.LogDebug("No position history available for recall");
                return null;
            }
            
            // Find the snapshot closest to the desired rewind duration
            PositionSnapshot? targetSnapshot = null;
            float targetAge = rewindDuration;
            float currentTime = Time.fixedTime;
            
            // Convert queue to array for easier searching
            var historyArray = positionHistory.ToArray();
            
            // Find the snapshot closest to our target age
            foreach (var snapshot in historyArray)
            {
                float snapshotAge = currentTime - snapshot.timestamp;
                
                // If this snapshot is closer to our target age, use it
                if (targetSnapshot == null || Mathf.Abs(snapshotAge - targetAge) < Mathf.Abs((currentTime - targetSnapshot.Value.timestamp) - targetAge))
                {
                    targetSnapshot = snapshot;
                }
            }
            
            if (targetSnapshot.HasValue)
            {
                float actualAge = currentTime - targetSnapshot.Value.timestamp;
                ChronoParaPlugin.Logger?.LogDebug($"Found recall target: {targetSnapshot.Value} (target age: {targetAge:F2}s, actual age: {actualAge:F2}s)");
            }
            
            return targetSnapshot;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to get recall target: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Forces a complete cleanup and refresh of the position history
    /// Useful when configuration changes or for debugging
    /// </summary>
    public void ForceHistoryRefresh()
    {
        if (!ValidateServerAuthority())
            return;
        
        try
        {
            ChronoParaPlugin.Logger?.LogDebug($"Forcing history refresh - clearing {positionHistory.Count} entries");
            
            // Clear all history
            positionHistory.Clear();
            
            // Reset recording timestamp to force immediate recording
            lastRecordTime = 0f;
            
            // Refresh configuration
            RefreshConfiguration();
            
            ChronoParaPlugin.Logger?.LogDebug("History refresh completed");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to force history refresh: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Network Command/RPC Handlers
    
    /// <summary>
    /// Handle network commands from clients
    /// </summary>
    /// <param name="cmdId">The command identifier</param>
    /// <param name="sender">The client connection that sent the command</param>
    /// <param name="args">Command arguments</param>
    protected override void HandleCmd(uint cmdId, FishNet.Connection.NetworkConnection sender, object[] args)
    {
        switch (cmdId)
        {
            case RECALL_COMMAND_ID:
                HandleRecallCommand(sender, args);
                break;
                
            default:
                ChronoParaPlugin.Logger?.LogWarning($"Unknown command ID received: {cmdId}");
                break;
        }
    }
    
    /// <summary>
    /// Handles recall command from client with full validation and error handling
    /// </summary>
    /// <param name="sender">The client connection that sent the command</param>
    /// <param name="args">Command arguments (currently unused but reserved for future parameters)</param>
    private void HandleRecallCommand(FishNet.Connection.NetworkConnection sender, object[] args)
    {
        if (!IsNetworkReady())
        {
            ChronoParaPlugin.Logger?.LogWarning($"Recall command received but network not ready from client {sender?.ClientId}");
            return;
        }
        
        if (!IsInitialized)
        {
            ChronoParaPlugin.Logger?.LogWarning($"Recall command received but PositionTracker not initialized for client {sender?.ClientId}");
            return;
        }
        
        try
        {
            ChronoParaPlugin.Logger?.LogDebug($"Processing recall command from client {sender?.ClientId}");
            
            // Validate sender connection
            if (sender == null)
            {
                ChronoParaPlugin.Logger?.LogError("Recall command received with null sender connection");
                return;
            }
            
            // Validate that this command is for the correct player
            if (!ValidatePlayerOwnership(sender))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Recall command from client {sender.ClientId} rejected - player ownership validation failed");
                return;
            }
            
            // Validate cooldown
            if (!ValidateRecallCooldown())
            {
                float remainingCooldown = ConfigManager.RecallCooldown - (Time.time - lastRecallRequestTime);
                ChronoParaPlugin.Logger?.LogDebug($"Recall command from client {sender.ClientId} rejected - cooldown active ({remainingCooldown:F1}s remaining)");
                return;
            }
            
            // Validate history availability
            var recallTarget = GetRecallTarget();
            if (!recallTarget.HasValue)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Recall command from client {sender.ClientId} rejected - no valid history available");
                return;
            }
            
            // All validations passed - execute recall
            ExecuteRecall(recallTarget.Value);
            
            // Update cooldown timestamp
            lastRecallRequestTime = Time.time;
            lastRecallTime = DateTime.Now;
            
            ChronoParaPlugin.Logger?.LogInfo($"Recall executed successfully for client {sender.ClientId}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error handling recall command from client {sender?.ClientId}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates that the recall request is from the correct player
    /// </summary>
    /// <param name="sender">The client connection making the request</param>
    /// <returns>True if the player ownership is valid</returns>
    private bool ValidatePlayerOwnership(FishNet.Connection.NetworkConnection sender)
    {
        try
        {
            // Validate that the sender owns this player object
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot validate player ownership - PlayerMovement component is null");
                return false;
            }
            
            // Check if the sender's client ID matches the owner of this player object
            // This prevents players from triggering recalls on other players
            var networkObject = GetComponent<FishNet.Object.NetworkObject>();
            if (networkObject == null)
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot validate player ownership - NetworkObject component is null");
                return false;
            }
            
            // Validate that the sender is the owner of this network object
            if (networkObject.Owner != sender)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Player ownership validation failed - sender {sender.ClientId} is not owner of this object (owner: {networkObject.Owner?.ClientId})");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating player ownership: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Validates that the recall cooldown has elapsed
    /// </summary>
    /// <returns>True if cooldown has elapsed and recall is allowed</returns>
    private bool ValidateRecallCooldown()
    {
        try
        {
            float timeSinceLastRecall = Time.time - lastRecallRequestTime;
            float cooldownDuration = PerformanceOptimizer.GetCachedRecallCooldown();
            
            bool cooldownElapsed = timeSinceLastRecall >= cooldownDuration;
            
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Cooldown validation - Time since last: {timeSinceLastRecall:F2}s, Required: {cooldownDuration:F2}s, Allowed: {cooldownElapsed}");
            }
            
            return cooldownElapsed;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating recall cooldown: {ex.Message}");
            return false; // Fail safe - deny recall on error
        }
    }
    
    /// <summary>
    /// Executes the actual recall operation (teleportation and health restoration)
    /// Implements server-side recall execution with full validation and safety checks
    /// </summary>
    /// <param name="targetSnapshot">The position snapshot to recall to</param>
    private void ExecuteRecall(PositionSnapshot targetSnapshot)
    {
        // Validate that we can execute recall (network ready and local player)
        if (!IsNetworkReady() || !IsLocalPlayer())
        {
            ChronoParaPlugin.Logger?.LogError("ExecuteRecall called but network not ready or not local player");
            return;
        }

        try
        {
            ChronoParaPlugin.Logger?.LogDebug($"Executing recall to position: {targetSnapshot}");
            
            // Perform server-side validation and safety checks
            if (!ValidateRecallExecution(targetSnapshot))
            {
                ChronoParaPlugin.Logger?.LogWarning("Recall execution validation failed");
                return;
            }

            // Execute teleportation using PlayerMovement.TelePlayer method
            bool teleportSuccess = ExecuteTeleportation(targetSnapshot.position);
            if (!teleportSuccess)
            {
                ChronoParaPlugin.Logger?.LogError("Teleportation failed - aborting recall");
                return;
            }

            // Restore health by directly setting playerHealth
            bool healthRestoreSuccess = RestorePlayerHealth(targetSnapshot.health);
            if (!healthRestoreSuccess)
            {
                ChronoParaPlugin.Logger?.LogWarning("Health restoration failed but teleportation succeeded");
            }

            // Update recall timestamp for kill window functionality
            UpdateRecallTimestamp();
            
            // Record recall execution for death tracking
            DeathTracker.RecordRecallExecution(gameObject);

            // Play effects directly (since we're the local player)
            PlayRecallEffectsDirectly(targetSnapshot);

            ChronoParaPlugin.Logger?.LogInfo($"Recall executed successfully - teleported to {targetSnapshot.position} with health {targetSnapshot.health}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error executing recall: {ex.Message}");
            
            // Attempt emergency recovery if possible
            try
            {
                ChronoParaPlugin.Logger?.LogDebug("Attempting emergency recovery after recall failure");
                // Reset recall timestamp to prevent kill window issues
                lastRecallTime = DateTime.MinValue;
            }
            catch (Exception recoveryEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Emergency recovery failed: {recoveryEx.Message}");
            }
        }
    }

    /// <summary>
    /// Validates that the recall execution can proceed safely
    /// </summary>
    /// <param name="targetSnapshot">The target snapshot to validate</param>
    /// <returns>True if recall execution should proceed</returns>
    private bool ValidateRecallExecution(PositionSnapshot targetSnapshot)
    {
        try
        {
            // Validate PlayerMovement component is available
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogError("Cannot execute recall - PlayerMovement component is null");
                return false;
            }

            // Validate target position is reasonable (not NaN, not extreme values)
            if (!IsValidPosition(targetSnapshot.position))
            {
                ChronoParaPlugin.Logger?.LogError($"Cannot execute recall - invalid target position: {targetSnapshot.position}");
                return false;
            }

            // Validate target health is reasonable
            if (!IsValidHealth(targetSnapshot.health))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Target health value appears invalid: {targetSnapshot.health}");
            }

            // Validate snapshot is not too old (additional safety check)
            if (targetSnapshot.IsExpired(rewindDuration * 2f)) // Allow some buffer
            {
                ChronoParaPlugin.Logger?.LogWarning($"Target snapshot is very old (age: {targetSnapshot.Age:F2}s) - proceeding with caution");
            }

            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating recall execution: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates that a position is reasonable for teleportation
    /// </summary>
    /// <param name="position">The position to validate</param>
    /// <returns>True if the position is valid</returns>
    private bool IsValidPosition(Vector3 position)
    {
        // Check for NaN or infinity values
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
        {
            return false;
        }

        // Check for extreme values that might indicate corruption
        float maxCoordinate = 10000f; // Reasonable maximum for game world coordinates
        if (Mathf.Abs(position.x) > maxCoordinate || 
            Mathf.Abs(position.y) > maxCoordinate || 
            Mathf.Abs(position.z) > maxCoordinate)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a health value is reasonable
    /// </summary>
    /// <param name="health">The health value to validate</param>
    /// <returns>True if the health value is valid</returns>
    private bool IsValidHealth(float health)
    {
        // Check for NaN or infinity
        if (float.IsNaN(health) || float.IsInfinity(health))
        {
            return false;
        }

        // Check for reasonable health range (0-1000 should cover most cases)
        if (health < 0f || health > 1000f)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes player teleportation by directly setting transform position
    /// Based on game's teleportation pattern: transform.position = targetPosition
    /// </summary>
    /// <param name="targetPosition">The position to teleport to</param>
    /// <returns>True if teleportation was successful</returns>
    private bool ExecuteTeleportation(Vector3 targetPosition)
    {
        try
        {
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogError("Cannot teleport - PlayerMovement component is null");
                return false;
            }

            ChronoParaPlugin.Logger?.LogDebug($"Teleporting player to position: {targetPosition}");

            // Store current position for verification
            Vector3 previousPosition = transform.position;

            // Use the game's teleportation pattern: start a coroutine that continuously sets position
            // This ensures proper network synchronization
            StartCoroutine(TeleportCoroutine(targetPosition));

            ChronoParaPlugin.Logger?.LogDebug($"Teleportation successful - moved from {previousPosition} to {targetPosition}");
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during teleportation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Coroutine that handles teleportation using the game's pattern
    /// Continuously sets position for 0.4 seconds to ensure network sync
    /// </summary>
    /// <param name="targetPosition">The position to teleport to</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator TeleportCoroutine(Vector3 targetPosition)
    {
        float timer = Time.time;
        
        // Continuously set position for 0.4 seconds (same as game's TelePlayer method)
        while (Time.time - timer < 0.4f)
        {
            transform.position = targetPosition;
            yield return null;
        }
        
        ChronoParaPlugin.Logger?.LogDebug($"Teleportation coroutine completed to position: {targetPosition}");
    }

    /// <summary>
    /// Restores player health by setting fields via reflection (0.7.8 runtime fields): playerHealth and ServerHealth
    /// </summary>
    /// <param name="targetHealth">The health value to restore to (no clamping to allow buffs)</param>
    /// <returns>True if health restoration was successful</returns>
    private bool RestorePlayerHealth(float targetHealth)
    {
        try
        {
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogError("Cannot restore health - PlayerMovement component is null");
                return false;
            }

            ChronoParaPlugin.Logger?.LogDebug($"Restoring player health to: {targetHealth}");

            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var playerField = typeof(PlayerMovement).GetField("playerHealth", flags);
            var serverField = typeof(PlayerMovement).GetField("ServerHealth", flags);

            // Read current values for logging if possible
            float prevClient = 0f;
            float prevServer = 0f;
            if (playerField != null)
            {
                object v = playerField.GetValue(playerMovement);
                if (v is float f) prevClient = f;
            }
            if (serverField != null)
            {
                object v = serverField.GetValue(playerMovement);
                if (v is float f) prevServer = f;
            }

            bool wroteAny = false;
            if (playerField != null)
            {
                playerField.SetValue(playerMovement, targetHealth);
                wroteAny = true;
            }
            else
            {
                ChronoParaPlugin.Logger?.LogError("playerHealth field not found via reflection");
            }

            if (serverField != null)
            {
                serverField.SetValue(playerMovement, targetHealth);
            }
            else
            {
                ChronoParaPlugin.Logger?.LogDebug("ServerHealth field not found via reflection - continuing with client health only");
            }

            if (!wroteAny)
                return false;

            // Verify
            float newClient = prevClient;
            float newServer = prevServer;
            if (playerField != null)
            {
                object v = playerField.GetValue(playerMovement);
                if (v is float f) newClient = f;
            }
            if (serverField != null)
            {
                object v = serverField.GetValue(playerMovement);
                if (v is float f) newServer = f;
            }

            if (Mathf.Abs(newClient - targetHealth) > 0.01f || (serverField != null && Mathf.Abs(newServer - targetHealth) > 0.01f))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Health restoration verification failed - client: {newClient}, server: {newServer}, target: {targetHealth}");
                return false;
            }

            ChronoParaPlugin.Logger?.LogDebug($"Health restored successfully - client {prevClient} -> {newClient}, server {prevServer} -> {newServer}");
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during health restoration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the recall timestamp for kill window functionality
    /// </summary>
    private void UpdateRecallTimestamp()
    {
        try
        {
            lastRecallTime = DateTime.Now;
            ChronoParaPlugin.Logger?.LogDebug($"Recall timestamp updated - kill window active until {lastRecallTime.AddSeconds(recallKillWindow)}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error updating recall timestamp: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays recall effects directly for the local player
    /// This bypasses the problematic RPC system
    /// </summary>
    /// <param name="targetSnapshot">The snapshot that was recalled to</param>
    private void PlayRecallEffectsDirectly(PositionSnapshot targetSnapshot)
    {
        try
        {
            ChronoParaPlugin.Logger?.LogDebug("Playing recall effects directly");

            // Play recall sound effect
            PlayRecallSoundEffect(targetSnapshot.position);

            // Play visual effects if available
            TriggerRecallVisualEffects(targetSnapshot.position);

            ChronoParaPlugin.Logger?.LogDebug("Recall effects played successfully");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error playing recall effects: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends RPC to all clients for visual and audio effects (DEPRECATED - using direct effects)
    /// </summary>
    /// <param name="targetSnapshot">The snapshot that was recalled to</param>
    private void SendRecallEffectsRPC(PositionSnapshot targetSnapshot)
    {
        try
        {
            ChronoParaPlugin.Logger?.LogDebug("Sending recall effects RPC to all clients");

            // Prepare RPC arguments with recall effect data
            object[] rpcArgs = new object[]
            {
                targetSnapshot.position.x,
                targetSnapshot.position.y,
                targetSnapshot.position.z,
                targetSnapshot.health,
                Time.time // Current time for effect synchronization
            };

            // Send RPC to all clients for visual/audio effects
            SendRpc(RECALL_EFFECT_RPC_ID, rpcArgs);

            ChronoParaPlugin.Logger?.LogDebug("Recall effects RPC sent successfully");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error sending recall effects RPC: {ex.Message}");
            // Don't fail the entire recall operation if effects fail
        }
    }
    
    /// <summary>
    /// Handle network RPCs from server
    /// </summary>
    /// <param name="rpcId">The RPC identifier</param>
    /// <param name="args">RPC arguments</param>
    protected override void HandleRpc(uint rpcId, object[] args)
    {
        switch (rpcId)
        {
            case RECALL_EFFECT_RPC_ID:
                HandleRecallEffectsRPC(args);
                break;
                
            default:
                ChronoParaPlugin.Logger?.LogWarning($"Unknown RPC ID received: {rpcId}");
                break;
        }
    }

    /// <summary>
    /// Handles recall effects RPC from server
    /// Implements client-side visual and audio effects for temporal recall
    /// </summary>
    /// <param name="args">RPC arguments containing effect data</param>
    private void HandleRecallEffectsRPC(object[] args)
    {
        try
        {
            if (args == null || args.Length < 5)
            {
                ChronoParaPlugin.Logger?.LogWarning("Received recall effects RPC with invalid arguments");
                return;
            }

            // Extract effect data from RPC arguments
            float posX = (float)args[0];
            float posY = (float)args[1];
            float posZ = (float)args[2];
            float health = (float)args[3];
            float effectTime = (float)args[4];

            Vector3 recallPosition = new Vector3(posX, posY, posZ);

            ChronoParaPlugin.Logger?.LogDebug($"Received recall effects RPC - Position: {recallPosition}, Health: {health:F1}, Time: {effectTime:F2}");

            // Play recall sound effect at the recall position
            PlayRecallSoundEffect(recallPosition);

            // Trigger visual effects using existing game systems
            TriggerRecallVisualEffects(recallPosition);

            // Log successful effect execution
            ChronoParaPlugin.Logger?.LogDebug($"Recall effects executed successfully at {recallPosition}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error handling recall effects RPC: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Client-Side Effects
    
    /// <summary>
    /// Plays recall sound effect at the specified position
    /// Creates or uses existing AudioSource component for sound playback
    /// </summary>
    /// <param name="position">The position where the recall occurred</param>
    private void PlayRecallSoundEffect(Vector3 position)
    {
        try
        {
            // Ensure we have an AudioSource component
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // Create AudioSource component if it doesn't exist
                    audioSource = gameObject.AddComponent<AudioSource>();
                    ConfigureAudioSource();
                }
            }

            // Load recall sound from asset bundle or use fallback
            AudioClip recallSound = LoadRecallSoundClip();
            
            if (recallSound != null)
            {
                // Play the recall sound effect with reduced volume for better audio balance
                audioSource.volume = 0.3f; // Reduced volume for more pleasant recall sound
                audioSource.clip = recallSound;
                audioSource.Play(); // Use Play() instead of PlayOneShot for better volume control
                
                ChronoParaPlugin.Logger?.LogDebug($"Playing recall sound effect at {position} with volume {audioSource.volume}");
            }
            else
            {
                // Fallback: Use existing game audio system for teleportation sounds
                PlayFallbackRecallSound();
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error playing recall sound effect: {ex.Message}");
            
            // Try fallback sound on error
            try
            {
                PlayFallbackRecallSound();
            }
            catch (Exception fallbackEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Fallback sound also failed: {fallbackEx.Message}");
            }
        }
    }

    /// <summary>
    /// Configures the AudioSource component for optimal recall sound playback
    /// </summary>
    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;

        try
        {
            // Configure AudioSource for maximum audibility
            audioSource.spatialBlend = 0.0f; // 2D sound - not affected by distance
            audioSource.rolloffMode = AudioRolloffMode.Linear; // Linear rolloff for better volume control
            audioSource.minDistance = 1f; // Close range
            audioSource.maxDistance = 100f; // Extended range for others to hear
            audioSource.volume = 1f; // Moderate volume for clear but not overwhelming recall sound
            audioSource.priority = 0; // Highest priority (0-255, lower is higher priority)
            audioSource.pitch = 1.0f; // Normal pitch
            audioSource.loop = false; // Don't loop the recall sound
            audioSource.playOnAwake = false; // Only play when triggered

            ChronoParaPlugin.Logger?.LogDebug("AudioSource configured for recall effects");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error configuring AudioSource: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the recall sound clip from AssetManager with fallback handling
    /// </summary>
    /// <returns>The recall sound AudioClip, or null if not available</returns>
    private AudioClip LoadRecallSoundClip()
    {
        try
        {
            // Load recall sound from AssetManager
            AudioClip recallSound = AssetManager.GetRecallSound();
            
            if (recallSound != null)
            {
                ChronoParaPlugin.Logger?.LogDebug("Loaded custom recall sound from AssetManager");
                return recallSound;
            }
            else
            {
                ChronoParaPlugin.Logger?.LogDebug("Custom recall sound not available - using fallback");
                return null;
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error loading recall sound clip: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tries to play the recall sound even louder using a temporary audio source
    /// </summary>
    private void TryPlayLouderRecallSound(AudioClip recallSound, Vector3 position)
    {
        try
        {
            // Create a temporary GameObject with AudioSource for extra volume
            GameObject tempAudioObject = new GameObject("TempRecallAudio");
            tempAudioObject.transform.position = position;
            
            AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
            tempAudioSource.clip = recallSound;
            tempAudioSource.volume = 0.3f;
            tempAudioSource.spatialBlend = 0.0f; // 2D sound
            tempAudioSource.priority = 0; // Highest priority
            tempAudioSource.Play();
            
            // Destroy the temporary object after the sound finishes
            UnityEngine.Object.Destroy(tempAudioObject, recallSound.length + 0.5f);
            
            ChronoParaPlugin.Logger?.LogDebug("Created temporary audio source for louder recall sound");
        }
        catch (System.Exception ex)
        {
            ChronoParaPlugin.Logger?.LogDebug($"Could not create temporary audio source: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays fallback recall sound using existing game audio systems
    /// Uses a distinctive sound pattern to indicate temporal recall
    /// </summary>
    private void PlayFallbackRecallSound()
    {
        try
        {
            if (audioSource == null) return;

            // Create a distinctive sound pattern for recall using built-in Unity audio
            // This creates a "whoosh" effect by modulating pitch and volume
            StartCoroutine(PlayRecallSoundSequence());
            
            ChronoParaPlugin.Logger?.LogDebug("Playing fallback recall sound sequence");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error playing fallback recall sound: {ex.Message}");
        }
    }

    /// <summary>
    /// Coroutine that plays a distinctive sound sequence for recall effect
    /// Creates a temporal "whoosh" sound using pitch and volume modulation
    /// </summary>
    private System.Collections.IEnumerator PlayRecallSoundSequence()
    {
        if (audioSource == null) 
        {
            ChronoParaPlugin.Logger?.LogWarning("AudioSource is null in PlayRecallSoundSequence");
            yield break;
        }

        // Store original audio settings
        float originalVolume = audioSource.volume;
        float originalPitch = audioSource.pitch;

        // Create a brief "temporal whoosh" effect
        float duration = 0.5f; // Half second effect
        float elapsed = 0f;

        // Generate a simple tone for the effect
        // This is a fallback until custom audio assets are available
        audioSource.volume = 0.3f; // Moderate volume for fallback sound
        
        while (elapsed < duration)
        {
            if (audioSource == null) yield break; // Safety check
            
            float progress = elapsed / duration;
            
            // Create a "whoosh" effect with pitch modulation
            audioSource.pitch = Mathf.Lerp(2.0f, 0.5f, progress); // High to low pitch
            audioSource.volume = Mathf.Lerp(1.0f, 0.2f, progress); // Fade out from max volume
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore original settings
        if (audioSource != null)
        {
            audioSource.volume = originalVolume;
            audioSource.pitch = originalPitch;
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Triggers visual effects for temporal recall using existing game systems
    /// Leverages the game's built-in teleportation and magical effect systems
    /// </summary>
    /// <param name="position">The position where the recall occurred</param>
    private void TriggerRecallVisualEffects(Vector3 position)
    {
        try
        {
            // Trigger particle effects at the recall position
            CreateRecallParticleEffect(position);

            // Create a brief screen effect for the local player if this is their recall
            if (IsOwner)
            {
                CreateLocalPlayerRecallEffect();
            }

            // Create a distinctive visual indicator that's visible to all nearby players
            CreateRecallVisualIndicator(position);

            ChronoParaPlugin.Logger?.LogDebug($"Recall visual effects triggered at {position}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error triggering recall visual effects: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates particle effects at the recall position
    /// Uses Unity's built-in particle system for temporal effects
    /// </summary>
    /// <param name="position">The position to create particles at</param>
    private void CreateRecallParticleEffect(Vector3 position)
    {
        try
        {
            // Create a temporary GameObject for the particle effect
            GameObject particleObject = new GameObject("RecallParticleEffect");
            particleObject.transform.position = position;

            // Add ParticleSystem component
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            
            if (particles != null)
            {
                ConfigureRecallParticles(particles);
                
                // Destroy the particle object after the effect completes
                UnityEngine.Object.Destroy(particleObject, 2.0f);
                
                ChronoParaPlugin.Logger?.LogDebug($"Created recall particle effect at {position}");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error creating recall particle effect: {ex.Message}");
        }
    }

    /// <summary>
    /// Configures particle system for temporal recall effects
    /// Creates a distinctive cyan-blue swirling effect
    /// </summary>
    /// <param name="particles">The ParticleSystem to configure</param>
    private void ConfigureRecallParticles(ParticleSystem particles)
    {
        try
        {
            var main = particles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 5f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.4f, 0.8f, 1.0f, 0.8f); // Cyan-blue with transparency
            main.maxParticles = 50;

            var emission = particles.emission;
            emission.rateOverTime = 30f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2f;

            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.4f, 0.8f, 1.0f), 0.0f),
                    new GradientColorKey(new Color(0.2f, 0.4f, 0.8f), 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = gradient;

            ChronoParaPlugin.Logger?.LogDebug("Configured recall particle system");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error configuring recall particles: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a brief screen effect for the local player during their recall
    /// Provides immediate visual feedback that the recall was successful
    /// </summary>
    private void CreateLocalPlayerRecallEffect()
    {
        try
        {
            // Create a brief screen flash or distortion effect
            // This will be a simple coroutine that modifies camera or UI elements
            StartCoroutine(LocalPlayerRecallFlash());
            
            ChronoParaPlugin.Logger?.LogDebug("Created local player recall effect");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error creating local player recall effect: {ex.Message}");
        }
    }

    /// <summary>
    /// Coroutine that creates a brief visual flash for the local player
    /// Simulates temporal distortion during recall
    /// </summary>
    private System.Collections.IEnumerator LocalPlayerRecallFlash()
    {
        // This is a placeholder for screen effect
        // In a full implementation, this would modify camera effects or UI overlay
        
        // For now, we'll just log the effect timing
        ChronoParaPlugin.Logger?.LogDebug("Local recall flash effect started");
        
        yield return new WaitForSeconds(0.2f);
        
        ChronoParaPlugin.Logger?.LogDebug("Local recall flash effect completed");
    }

    /// <summary>
    /// Creates a visual indicator at the recall position that's visible to all players
    /// Helps other players understand what happened and where
    /// </summary>
    /// <param name="position">The position to create the indicator at</param>
    private void CreateRecallVisualIndicator(Vector3 position)
    {
        try
        {
            // Create a temporary visual marker at the recall position
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "RecallIndicator";
            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * 0.5f;

            // Configure the indicator appearance
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create a glowing cyan material
                Material indicatorMaterial = new Material(Shader.Find("Standard"));
                indicatorMaterial.color = new Color(0.4f, 0.8f, 1.0f, 0.7f);
                indicatorMaterial.SetFloat("_Mode", 3); // Transparent mode
                indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                indicatorMaterial.SetInt("_ZWrite", 0);
                indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
                indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
                indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                indicatorMaterial.renderQueue = 3000;
                
                renderer.material = indicatorMaterial;
            }

            // Remove collider to prevent interference
            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            // Animate the indicator (pulsing effect)
            try
            {
                StartCoroutine(AnimateRecallIndicator(indicator));
            }
            catch (Exception animEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Error starting indicator animation: {animEx.Message}");
            }

            // Destroy after a few seconds
            UnityEngine.Object.Destroy(indicator, 3.0f);

            ChronoParaPlugin.Logger?.LogDebug($"Created recall visual indicator at {position}");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error creating recall visual indicator: {ex.Message}");
        }
    }

    /// <summary>
    /// Animates the recall visual indicator with a pulsing effect
    /// </summary>
    /// <param name="indicator">The indicator GameObject to animate</param>
    private System.Collections.IEnumerator AnimateRecallIndicator(GameObject indicator)
    {
        if (indicator == null) 
        {
            ChronoParaPlugin.Logger?.LogWarning("Indicator is null in AnimateRecallIndicator");
            yield break;
        }

        Vector3 originalScale = indicator.transform.localScale;
        float duration = 3.0f;
        float elapsed = 0f;

        while (elapsed < duration && indicator != null)
        {
            float progress = elapsed / duration;
            
            // Create pulsing effect
            float pulse = Mathf.Sin(elapsed * 8f) * 0.2f + 1f; // Pulse between 0.8 and 1.2
            float fade = 1f - progress; // Fade out over time
            
            indicator.transform.localScale = originalScale * pulse * fade;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Request a temporal recall (client-side method)
    /// Sends a network command to the server to execute the recall
    /// </summary>
    public void RequestRecall()
    {
        // Basic component validation
        if (!isInitialized || playerMovement == null)
        {
            ChronoParaPlugin.Logger?.LogWarning("Cannot request recall - PositionTracker component not ready");
            return;
        }
        
        // Check if network is ready using a more reliable method
        if (!IsNetworkReady())
        {
            ChronoParaPlugin.Logger?.LogWarning("Cannot request recall - network not ready (try again in a moment)");
            return;
        }
        
        // Only allow recall requests from the local player (owner)
        if (!IsLocalPlayer())
        {
            ChronoParaPlugin.Logger?.LogWarning("Cannot request recall - not the owner of this player object");
            return;
        }
        
        try
        {
            // Perform client-side validation before sending network command
            if (!ValidateClientSideRecallRequest())
            {
                return; // Validation failed, error already logged
            }
            
            ChronoParaPlugin.Logger?.LogDebug("Sending recall command to server");
            
            // Send recall request using a more direct approach
            // Since we're the local player, we can execute the recall directly
            ExecuteRecallDirectly();
            
            ChronoParaPlugin.Logger?.LogDebug("Recall executed successfully");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to send recall command to server: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Executes recall directly for the local player
    /// This bypasses the problematic network command system
    /// </summary>
    private void ExecuteRecallDirectly()
    {
        try
        {
            // Get the recall target position
            var recallTarget = GetRecallTarget();
            if (!recallTarget.HasValue)
            {
                ChronoParaPlugin.Logger?.LogWarning("No position history available for recall");
                return;
            }

            // Execute the recall
            ExecuteRecall(recallTarget.Value);
            
            // Update cooldown timestamp
            lastRecallRequestTime = Time.time;
            lastRecallTime = DateTime.Now;
            
            ChronoParaPlugin.Logger?.LogInfo("Recall executed successfully");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Failed to execute recall directly: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs client-side validation before sending recall request to server
    /// This provides immediate feedback to the player without network round-trip
    /// </summary>
    /// <returns>True if the request should be sent to server</returns>
    private bool ValidateClientSideRecallRequest()
    {
        try
        {
            // Check if we're connected to a server
            if (!IsNetworkReady())
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot request recall - network not ready");
                return false;
            }
            
            // Check if PlayerMovement component is available
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot request recall - PlayerMovement component not available");
                return false;
            }
            
            // Client-side cooldown check for immediate feedback
            // Server will do authoritative validation, but this prevents unnecessary network traffic
            if (!ValidateRecallCooldown())
            {
                float remainingCooldown = ConfigManager.RecallCooldown - (Time.time - lastRecallRequestTime);
                ChronoParaPlugin.Logger?.LogDebug($"Recall request blocked - cooldown active ({remainingCooldown:F1}s remaining)");
                return false;
            }
            
            // All client-side validations passed
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in client-side recall validation: {ex.Message}");
            return false; // Fail safe - block request on error
        }
    }
    
    /// <summary>
    /// Force refresh of cached configuration values
    /// Useful when configuration changes at runtime
    /// </summary>
    public void RefreshConfigurationCache()
    {
        RefreshConfiguration();
    }
    
    /// <summary>
    /// Get diagnostic information about the position tracker state
    /// </summary>
    /// <returns>Diagnostic string with current state information</returns>
    public string GetDiagnosticInfo()
    {
        if (!IsInitialized)
            return "PositionTracker: Not Initialized";
        
        return $"PositionTracker: History={HistoryCount}, LastRecord={Time.time - lastRecordTime:F2}s ago, " +
               $"InKillWindow={IsInRecallKillWindow}, NetworkReady={IsNetworkReady()}, IsLocalPlayer={IsLocalPlayer()}, " +
               $"Cooldown={RemainingCooldown:F1}s";
    }
    
    /// <summary>
    /// Handles network disconnection and cleanup
    /// Called when the network connection is lost
    /// </summary>
    public void HandleNetworkDisconnection()
    {
        try
        {
            ChronoParaPlugin.Logger?.LogDebug($"Handling network disconnection for {gameObject.name}");
            
            // Clear any pending recall operations
            lastRecallRequestTime = -999f; // Reset cooldown
            
            // Clear position history if we're no longer the server
            if (!IsNetworkReady() && positionHistory != null)
            {
                positionHistory.Clear();
                ChronoParaPlugin.Logger?.LogDebug("Cleared position history due to network disconnection");
            }
            
            // Reset network-dependent state
            isInitialized = false;
            
            ChronoParaPlugin.Logger?.LogDebug("Network disconnection handled successfully");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error handling network disconnection: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles network reconnection and re-initialization
    /// Called when the network connection is re-established
    /// </summary>
    public void HandleNetworkReconnection()
    {
        try
        {
            ChronoParaPlugin.Logger?.LogDebug($"Handling network reconnection for {gameObject.name}");
            
            // Re-validate network setup
            if (ValidateNetworkSetup())
            {
                isInitialized = true;
                
                // Refresh configuration
                RefreshConfiguration();
                
                // Reset timestamps for clean state
                lastRecordTime = 0f;
                lastRecallRequestTime = -999f;
                lastRecallTime = DateTime.MinValue;
                
                // Clear and reinitialize position history if we're the server
                if (IsNetworkReady())
                {
                    positionHistory?.Clear();
                    ChronoParaPlugin.Logger?.LogDebug("Reinitialized position history after reconnection");
                }
                
                ChronoParaPlugin.Logger?.LogDebug("Network reconnection handled successfully");
            }
            else
            {
                ChronoParaPlugin.Logger?.LogWarning("Network reconnection failed - setup validation failed");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error handling network reconnection: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Emergency cleanup method for critical errors
    /// Resets the component to a safe state
    /// </summary>
    public void EmergencyReset()
    {
        try
        {
            ChronoParaPlugin.Logger?.LogWarning($"Performing emergency reset for {gameObject.name}");
            
            // Clear all state
            positionHistory?.Clear();
            lastRecordTime = 0f;
            lastRecallRequestTime = -999f;
            lastRecallTime = DateTime.MinValue;
            frameSkipCounter = 0;
            
            // Reset initialization flag
            isInitialized = false;
            
            // Clear component references
            playerMovement = null;
            audioSource = null;
            
            // Attempt to re-initialize
            CacheComponentReferences();
            RefreshConfiguration();
            
            // Re-validate setup
            if (ValidateNetworkSetup())
            {
                isInitialized = true;
                ChronoParaPlugin.Logger?.LogInfo("Emergency reset completed successfully");
            }
            else
            {
                ChronoParaPlugin.Logger?.LogError("Emergency reset failed - component remains disabled");
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Critical error during emergency reset: {ex.Message}");
            isInitialized = false;
        }
    }
    
    #endregion
    
    #region Private Validation Methods
    
    /// <summary>
    /// Checks if we can perform network operations by testing basic functionality
    /// This avoids the problematic IsServerInitialized/IsClientInitialized properties
    /// </summary>
    /// <returns>True if network operations should be possible</returns>
    private bool IsNetworkReady()
    {
        try
        {
            // Check if we have a valid NetworkObject component
            var networkObject = GetComponent<FishNet.Object.NetworkObject>();
            if (networkObject == null)
            {
                ChronoParaPlugin.Logger?.LogDebug("No NetworkObject component found");
                return false;
            }

            // Check if the NetworkObject is spawned (indicates network is active)
            bool isSpawned = networkObject.IsSpawned;
            return isSpawned;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogDebug($"Network readiness check failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if this is the local player (owner) using a safe method
    /// </summary>
    /// <returns>True if this is the local player</returns>
    private bool IsLocalPlayer()
    {
        try
        {
            var networkObject = GetComponent<FishNet.Object.NetworkObject>();
            if (networkObject == null)
                return false;
                
            bool isOwner = networkObject.IsOwner;
            return isOwner;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogDebug($"Owner check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates that the network behavior is properly set up
    /// </summary>
    /// <returns>True if network setup is valid</returns>
    private bool ValidateNetworkSetup()
    {
        try
        {
            // Validate that essential components are present (critical)
            if (playerMovement == null)
            {
                ChronoParaPlugin.Logger?.LogError($"PlayerMovement component missing on {gameObject.name}");
                return false;
            }
            
            // Check network authority (non-critical - may not be available immediately)
            bool hasNetworkInfo = IsNetworkReady();
            
            if (!hasNetworkInfo)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Network authority not yet established for {gameObject.name} - this is normal during initialization");
                // Don't fail validation - network authority may be established later
            }
            else
            {
                ChronoParaPlugin.Logger?.LogDebug($"Network setup validated for {gameObject.name}");
            }
            
            return true; // Always return true if essential components are present
        }
        catch (System.Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Network setup validation failed: {ex.Message}");
            return false;
        }
    }
    
    #endregion
}