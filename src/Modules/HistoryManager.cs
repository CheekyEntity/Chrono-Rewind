using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChronoPara.Modules;

/// <summary>
/// Utility class for managing timestamp-based position history with comprehensive error handling
/// Provides methods for maintaining and querying position snapshot collections safely
/// </summary>
public static class HistoryManager
{
    #region Constants
    
    private const int MAX_SAFE_HISTORY_SIZE = 2000; // Absolute maximum to prevent memory issues
    private const float TIMESTAMP_TOLERANCE = 0.1f; // Tolerance for timestamp comparisons
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Adds a new snapshot to the history queue and removes expired entries with comprehensive error handling
    /// </summary>
    /// <param name="history">The history queue to manage</param>
    /// <param name="snapshot">The new snapshot to add</param>
    /// <param name="maxDuration">Maximum duration to keep snapshots (in seconds)</param>
    /// <returns>True if the snapshot was successfully added</returns>
    public static bool AddSnapshot(Queue<PositionSnapshot> history, PositionSnapshot snapshot, float maxDuration)
    {
        try
        {
            if (history == null)
            {
                ChronoParaPlugin.Logger?.LogError("Cannot add snapshot - history queue is null");
                return false;
            }
            
            // Validate the snapshot before adding
            if (!snapshot.IsValid())
            {
                ChronoParaPlugin.Logger?.LogWarning("Attempting to add invalid snapshot, sanitizing first");
                snapshot = snapshot.Sanitize();
                
                if (!snapshot.IsValid())
                {
                    ChronoParaPlugin.Logger?.LogError("Failed to sanitize invalid snapshot, skipping");
                    return false;
                }
            }
            
            // Validate maxDuration parameter
            if (float.IsNaN(maxDuration) || maxDuration <= 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid maxDuration: {maxDuration}, using default");
                maxDuration = ConfigManager.RewindDuration;
            }
            
            // Check for memory safety before adding
            if (history.Count >= MAX_SAFE_HISTORY_SIZE)
            {
                ChronoParaPlugin.Logger?.LogWarning($"History queue at maximum safe size ({MAX_SAFE_HISTORY_SIZE}), forcing cleanup");
                ForceCleanupOldestEntries(history, MAX_SAFE_HISTORY_SIZE / 2); // Remove half the entries
            }
            
            // Add the new snapshot
            history.Enqueue(snapshot);
            
            // Remove expired snapshots
            int removedCount = CleanupExpiredSnapshots(history, maxDuration);
            
            if (ChronoParaPlugin.DebugMode?.Value == true && removedCount > 0)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Added snapshot and removed {removedCount} expired entries. Queue size: {history.Count}");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error adding snapshot to history: {ex.Message}");
            
            // Attempt recovery by clearing corrupted history
            try
            {
                if (history != null && history.Count > 0)
                {
                    ChronoParaPlugin.Logger?.LogWarning("Attempting to recover by clearing potentially corrupted history");
                    history.Clear();
                }
            }
            catch (Exception recoveryEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Failed to recover from history corruption: {recoveryEx.Message}");
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Removes expired snapshots from the history queue with error handling
    /// </summary>
    /// <param name="history">The history queue to clean</param>
    /// <param name="maxDuration">Maximum duration to keep snapshots (in seconds)</param>
    /// <returns>Number of snapshots removed</returns>
    public static int CleanupExpiredSnapshots(Queue<PositionSnapshot> history, float maxDuration)
    {
        if (history == null)
        {
            ChronoParaPlugin.Logger?.LogWarning("Cannot cleanup expired snapshots - history queue is null");
            return 0;
        }
        
        try
        {
            float currentTime = Time.time;
            int removedCount = 0;
            int maxIterations = Math.Max(history.Count * 2, 1000); // Allow for more iterations when cleaning invalid snapshots
            int iterations = 0;
            
            // Remove snapshots that are too old
            while (history.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                var oldest = history.Peek();
                
                // Validate the oldest snapshot
                if (!oldest.IsValid())
                {
                    ChronoParaPlugin.Logger?.LogWarning("Found invalid snapshot during cleanup, removing");
                    history.Dequeue();
                    removedCount++;
                    continue;
                }
                
                // Check if snapshot is expired
                float age = currentTime - oldest.timestamp;
                if (age > maxDuration + TIMESTAMP_TOLERANCE)
                {
                    history.Dequeue();
                    removedCount++;
                }
                else
                {
                    break; // Queue is ordered by time, so we can stop here
                }
            }
            
            if (iterations >= maxIterations)
            {
                ChronoParaPlugin.Logger?.LogError($"Cleanup loop exceeded maximum iterations ({maxIterations}), clearing queue to prevent corruption");
                history.Clear();
                removedCount = iterations; // Approximate count of removed items
            }
            
            return removedCount;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during snapshot cleanup: {ex.Message}");
            
            // Attempt emergency cleanup
            try
            {
                ChronoParaPlugin.Logger?.LogWarning("Attempting emergency cleanup due to error");
                history.Clear();
                return history.Count; // Return the number that were cleared
            }
            catch (Exception emergencyEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Emergency cleanup failed: {emergencyEx.Message}");
                return 0;
            }
        }
    }
    
    /// <summary>
    /// Gets the snapshot from the specified duration ago, or the oldest available, with comprehensive error handling
    /// </summary>
    /// <param name="history">The history queue to search</param>
    /// <param name="rewindDuration">How far back to look (in seconds)</param>
    /// <returns>The snapshot to recall to, or null if no history available</returns>
    public static PositionSnapshot? GetRecallSnapshot(Queue<PositionSnapshot> history, float rewindDuration)
    {
        try
        {
            if (history == null)
            {
                ChronoParaPlugin.Logger?.LogWarning("Cannot get recall snapshot - history queue is null");
                return null;
            }
            
            if (history.Count == 0)
            {
                ChronoParaPlugin.Logger?.LogDebug("No snapshots available for recall");
                return null;
            }
            
            // Validate rewindDuration parameter
            if (float.IsNaN(rewindDuration) || rewindDuration < 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid rewind duration: {rewindDuration}, using default");
                rewindDuration = ConfigManager.RewindDuration;
            }
            
            float currentTime = Time.time;
            float targetTime = currentTime - rewindDuration;
            PositionSnapshot? bestSnapshot = null;
            float bestTimeDifference = float.MaxValue;
            
            // Find the snapshot closest to the target time
            var snapshots = history.ToArray();
            for (int i = 0; i < snapshots.Length; i++)
            {
                var snapshot = snapshots[i];
                
                // Validate each snapshot
                if (!snapshot.IsValid())
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Found invalid snapshot at index {i} during recall search, skipping");
                    continue;
                }
                
                // Calculate how close this snapshot is to our target time
                float timeDifference = Mathf.Abs(snapshot.timestamp - targetTime);
                
                // If this snapshot is closer to our target time, use it
                if (timeDifference < bestTimeDifference)
                {
                    bestSnapshot = snapshot;
                    bestTimeDifference = timeDifference;
                }
                
                // If we've found a snapshot that's exactly at or before our target time, we can stop
                if (snapshot.timestamp <= targetTime)
                {
                    break;
                }
            }
            
            // If no suitable snapshot was found, use the oldest available valid snapshot
            if (!bestSnapshot.HasValue)
            {
                for (int i = 0; i < snapshots.Length; i++)
                {
                    if (snapshots[i].IsValid())
                    {
                        bestSnapshot = snapshots[i];
                        ChronoParaPlugin.Logger?.LogDebug("No snapshot found at target time, using oldest valid snapshot");
                        break;
                    }
                }
            }
            
            if (bestSnapshot.HasValue)
            {
                float actualAge = currentTime - bestSnapshot.Value.timestamp;
                ChronoParaPlugin.Logger?.LogDebug($"Found recall snapshot: target age {rewindDuration:F2}s, actual age {actualAge:F2}s");
            }
            else
            {
                ChronoParaPlugin.Logger?.LogWarning("No valid snapshots found for recall");
            }
            
            return bestSnapshot;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error getting recall snapshot: {ex.Message}");
            
            // Attempt to return the oldest valid snapshot as a fallback
            try
            {
                if (history != null && history.Count > 0)
                {
                    var oldest = history.Peek();
                    if (oldest.IsValid())
                    {
                        ChronoParaPlugin.Logger?.LogInfo("Returning oldest snapshot as fallback after error");
                        return oldest;
                    }
                }
            }
            catch (Exception fallbackEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Fallback snapshot retrieval failed: {fallbackEx.Message}");
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Validates that a history queue is in a consistent state with comprehensive error reporting
    /// </summary>
    /// <param name="history">The history queue to validate</param>
    /// <param name="maxDuration">Expected maximum duration</param>
    /// <returns>True if the history is valid</returns>
    public static bool ValidateHistoryIntegrity(Queue<PositionSnapshot> history, float maxDuration)
    {
        try
        {
            if (history == null)
            {
                ChronoParaPlugin.Logger?.LogError("History validation failed - queue is null");
                return false;
            }
            
            if (history.Count == 0)
            {
                return true; // Empty history is valid
            }
            
            float currentTime = Time.time;
            float previousTimestamp = float.MinValue;
            int invalidSnapshots = 0;
            int expiredSnapshots = 0;
            int outOfOrderSnapshots = 0;
            
            var snapshots = history.ToArray();
            
            for (int i = 0; i < snapshots.Length; i++)
            {
                var snapshot = snapshots[i];
                
                // Check individual snapshot validity
                if (!snapshot.IsValid())
                {
                    invalidSnapshots++;
                    ChronoParaPlugin.Logger?.LogWarning($"Invalid snapshot found at index {i}: {snapshot}");
                    continue;
                }
                
                // Check timestamp ordering (should be ascending - oldest to newest)
                if (snapshot.timestamp < previousTimestamp - TIMESTAMP_TOLERANCE)
                {
                    outOfOrderSnapshots++;
                    ChronoParaPlugin.Logger?.LogWarning($"Out-of-order timestamp at index {i}: {snapshot.timestamp} < {previousTimestamp}");
                }
                
                // Check if snapshot is too old
                float age = currentTime - snapshot.timestamp;
                if (age > maxDuration + TIMESTAMP_TOLERANCE)
                {
                    expiredSnapshots++;
                    ChronoParaPlugin.Logger?.LogWarning($"Expired snapshot found at index {i}: age {age:F2}s > max {maxDuration:F2}s");
                }
                
                previousTimestamp = snapshot.timestamp;
            }
            
            // Report validation results
            bool isValid = invalidSnapshots == 0 && expiredSnapshots == 0 && outOfOrderSnapshots == 0;
            
            if (!isValid)
            {
                ChronoParaPlugin.Logger?.LogError($"History validation failed - Invalid: {invalidSnapshots}, Expired: {expiredSnapshots}, Out-of-order: {outOfOrderSnapshots}");
                
                // Provide recovery suggestions
                if (invalidSnapshots > 0)
                {
                    ChronoParaPlugin.Logger?.LogInfo("Recovery suggestion: Clear and rebuild position history");
                }
                if (expiredSnapshots > 0)
                {
                    ChronoParaPlugin.Logger?.LogInfo("Recovery suggestion: Run cleanup to remove expired snapshots");
                }
                if (outOfOrderSnapshots > 0)
                {
                    ChronoParaPlugin.Logger?.LogInfo("Recovery suggestion: Clear history to fix timestamp ordering");
                }
            }
            else
            {
                ChronoParaPlugin.Logger?.LogDebug($"History validation passed - {snapshots.Length} snapshots are valid");
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during history validation: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets comprehensive statistics about the history queue for debugging
    /// </summary>
    /// <param name="history">The history queue to analyze</param>
    /// <returns>A detailed string with history statistics</returns>
    public static string GetHistoryStats(Queue<PositionSnapshot> history)
    {
        try
        {
            if (history == null)
            {
                return "History: NULL";
            }
            
            if (history.Count == 0)
            {
                return "History: Empty";
            }
            
            var snapshots = history.ToArray();
            float currentTime = Time.time;
            
            var oldest = snapshots[0];
            var newest = snapshots[snapshots.Length - 1];
            
            float totalDuration = newest.timestamp - oldest.timestamp;
            float oldestAge = currentTime - oldest.timestamp;
            float newestAge = currentTime - newest.timestamp;
            
            // Calculate validity statistics
            int validSnapshots = 0;
            int invalidSnapshots = 0;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.IsValid())
                    validSnapshots++;
                else
                    invalidSnapshots++;
            }
            
            // Calculate memory usage estimate
            int estimatedMemoryBytes = snapshots.Length * (sizeof(float) * 5); // 3 floats for position + 1 for health + 1 for timestamp
            
            return $"History Stats:\n" +
                   $"  Count: {history.Count} snapshots\n" +
                   $"  Valid: {validSnapshots}, Invalid: {invalidSnapshots}\n" +
                   $"  Duration: {totalDuration:F2}s\n" +
                   $"  Oldest: {oldestAge:F2}s ago\n" +
                   $"  Newest: {newestAge:F2}s ago\n" +
                   $"  Memory: ~{estimatedMemoryBytes} bytes\n" +
                   $"  Integrity: {(ValidateHistoryIntegrity(history, ConfigManager.RewindDuration) ? "Valid" : "Invalid")}";
        }
        catch (Exception ex)
        {
            return $"History Stats: Error - {ex.Message}";
        }
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Forces removal of the oldest entries when the queue becomes too large
    /// </summary>
    /// <param name="history">The history queue to clean</param>
    /// <param name="targetSize">The target size to reduce to</param>
    private static void ForceCleanupOldestEntries(Queue<PositionSnapshot> history, int targetSize)
    {
        try
        {
            if (history == null || targetSize < 0)
                return;
            
            int toRemove = history.Count - targetSize;
            int removed = 0;
            
            while (history.Count > targetSize && removed < toRemove)
            {
                history.Dequeue();
                removed++;
            }
            
            ChronoParaPlugin.Logger?.LogWarning($"Force-removed {removed} oldest entries to prevent memory issues");
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during force cleanup: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Recovery Methods
    
    /// <summary>
    /// Attempts to repair a corrupted history queue by removing invalid entries
    /// </summary>
    /// <param name="history">The history queue to repair</param>
    /// <returns>Number of entries removed during repair</returns>
    public static int RepairHistory(Queue<PositionSnapshot> history)
    {
        try
        {
            if (history == null)
            {
                ChronoParaPlugin.Logger?.LogError("Cannot repair null history queue");
                return 0;
            }
            
            var validSnapshots = new Queue<PositionSnapshot>();
            int removedCount = 0;
            
            // Extract all valid snapshots
            while (history.Count > 0)
            {
                var snapshot = history.Dequeue();
                if (snapshot.IsValid())
                {
                    validSnapshots.Enqueue(snapshot);
                }
                else
                {
                    removedCount++;
                }
            }
            
            // Restore valid snapshots back to original queue
            while (validSnapshots.Count > 0)
            {
                history.Enqueue(validSnapshots.Dequeue());
            }
            
            if (removedCount > 0)
            {
                ChronoParaPlugin.Logger?.LogInfo($"History repair completed - removed {removedCount} invalid entries, {history.Count} valid entries remain");
            }
            
            return removedCount;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during history repair: {ex.Message}");
            
            // Emergency clear if repair fails
            try
            {
                history?.Clear();
                ChronoParaPlugin.Logger?.LogWarning("Emergency history clear performed due to repair failure");
            }
            catch (Exception clearEx)
            {
                ChronoParaPlugin.Logger?.LogError($"Emergency clear failed: {clearEx.Message}");
            }
            
            return 0;
        }
    }
    
    #endregion
}