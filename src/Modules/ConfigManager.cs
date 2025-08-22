using UnityEngine;
using System;

namespace ChronoPara.Modules;

/// <summary>
/// Centralized configuration access point for the Chronomancer's Paradox mod
/// Provides safe access to configuration values with fallback defaults and error handling
/// </summary>
public static class ConfigManager
{
    #region Configuration Properties
    
    /// <summary>
    /// Gets the current recall cooldown in seconds with error handling and validation
    /// </summary>
    public static float RecallCooldown
    {
        get
        {
            try
            {
                float value = ChronoParaPlugin.RecallCooldown?.Value ?? 45f;
                return ValidateAndClampCooldown(value);
            }
            catch (Exception ex)
            {
                LogConfigurationError("RecallCooldown", ex);
                return 45f; // Safe default
            }
        }
    }
    
    /// <summary>
    /// Gets the current rewind duration in seconds with error handling and validation
    /// </summary>
    public static float RewindDuration
    {
        get
        {
            try
            {
                float value = ChronoParaPlugin.RewindDuration?.Value ?? 3f;
                return ValidateAndClampRewindDuration(value);
            }
            catch (Exception ex)
            {
                LogConfigurationError("RewindDuration", ex);
                return 3f; // Safe default
            }
        }
    }
    
    /// <summary>
    /// Gets the current recall kill window in seconds with error handling and validation
    /// </summary>
    public static float RecallKillWindow
    {
        get
        {
            try
            {
                float value = ChronoParaPlugin.RecallKillWindow?.Value ?? 3f;
                return ValidateAndClampKillWindow(value);
            }
            catch (Exception ex)
            {
                LogConfigurationError("RecallKillWindow", ex);
                return 3f; // Safe default
            }
        }
    }
    
    /// <summary>
    /// Calculates the maximum history size based on rewind duration and recording interval
    /// Includes safety bounds to prevent excessive memory usage
    /// </summary>
    public static int MaxHistorySize
    {
        get
        {
            try
            {
                float duration = RewindDuration;
                float interval = RecordingInterval;
                
                if (interval <= 0f)
                {
                    ChronoParaPlugin.Logger?.LogWarning("Invalid recording interval, using default");
                    interval = 0.1f;
                }
                
                int size = Mathf.CeilToInt(duration / interval);
                
                // Apply safety bounds to prevent excessive memory usage
                const int MIN_SIZE = 10;   // Minimum 10 snapshots
                const int MAX_SIZE = 1000; // Maximum 1000 snapshots (100 seconds at 0.1s interval)
                
                size = Mathf.Clamp(size, MIN_SIZE, MAX_SIZE);
                
                // MaxHistorySize calculated successfully (removed debug spam)
                
                return size;
            }
            catch (Exception ex)
            {
                LogConfigurationError("MaxHistorySize", ex);
                return 30; // Safe default (3 seconds at 0.1s interval)
            }
        }
    }
    
    /// <summary>
    /// Gets whether the Chronos Rewind spell can spawn in team chests
    /// </summary>
    public static bool CanSpawnInChests
    {
        get
        {
            try
            {
                return ChronoParaPlugin.CanSpawnInChests?.Value ?? false;
            }
            catch (Exception ex)
            {
                LogConfigurationError("CanSpawnInChests", ex);
                return false; // Safe default - disabled
            }
        }
    }
    
    /// <summary>
    /// Gets the position recording interval in seconds (10 times per second)
    /// </summary>
    public static float RecordingInterval => 0.1f;
    
    #endregion
    
    #region Validation Methods
    
    /// <summary>
    /// Validates and clamps the recall cooldown value to acceptable bounds
    /// </summary>
    /// <param name="value">The cooldown value to validate</param>
    /// <returns>The validated and clamped cooldown value</returns>
    private static float ValidateAndClampCooldown(float value)
    {
        const float MIN_COOLDOWN = 5f;   // Minimum 5 seconds
        const float MAX_COOLDOWN = 300f; // Maximum 5 minutes
        
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            ChronoParaPlugin.Logger?.LogWarning($"Invalid cooldown value detected: {value}, using default");
            return 45f;
        }
        
        float clampedValue = Mathf.Clamp(value, MIN_COOLDOWN, MAX_COOLDOWN);
        
        if (clampedValue != value)
        {
            ChronoParaPlugin.Logger?.LogWarning($"Cooldown value {value} was outside acceptable range, clamped to {clampedValue}");
        }
        
        return clampedValue;
    }
    
    /// <summary>
    /// Validates and clamps the rewind duration value to acceptable bounds
    /// </summary>
    /// <param name="value">The rewind duration value to validate</param>
    /// <returns>The validated and clamped rewind duration value</returns>
    private static float ValidateAndClampRewindDuration(float value)
    {
        const float MIN_DURATION = 0.5f; // Minimum 0.5 seconds
        const float MAX_DURATION = 30f;  // Maximum 30 seconds
        
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            ChronoParaPlugin.Logger?.LogWarning($"Invalid rewind duration value detected: {value}, using default");
            return 3f;
        }
        
        float clampedValue = Mathf.Clamp(value, MIN_DURATION, MAX_DURATION);
        
        if (clampedValue != value)
        {
            ChronoParaPlugin.Logger?.LogWarning($"Rewind duration value {value} was outside acceptable range, clamped to {clampedValue}");
        }
        
        return clampedValue;
    }
    
    /// <summary>
    /// Validates and clamps the kill window value to acceptable bounds
    /// </summary>
    /// <param name="value">The kill window value to validate</param>
    /// <returns>The validated and clamped kill window value</returns>
    private static float ValidateAndClampKillWindow(float value)
    {
        const float MIN_WINDOW = 0.5f; // Minimum 0.5 seconds
        const float MAX_WINDOW = 15f;  // Maximum 15 seconds
        
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            ChronoParaPlugin.Logger?.LogWarning($"Invalid kill window value detected: {value}, using default");
            return 3f;
        }
        
        float clampedValue = Mathf.Clamp(value, MIN_WINDOW, MAX_WINDOW);
        
        if (clampedValue != value)
        {
            ChronoParaPlugin.Logger?.LogWarning($"Kill window value {value} was outside acceptable range, clamped to {clampedValue}");
        }
        
        return clampedValue;
    }
    
    #endregion
    
    #region Error Handling and Logging
    
    /// <summary>
    /// Logs configuration-related errors with detailed information
    /// </summary>
    /// <param name="configName">The name of the configuration property that failed</param>
    /// <param name="exception">The exception that occurred</param>
    private static void LogConfigurationError(string configName, Exception exception)
    {
        ChronoParaPlugin.Logger?.LogError($"Error accessing configuration '{configName}': {exception.Message}");
        
        if (ChronoParaPlugin.DebugMode?.Value == true)
        {
            ChronoParaPlugin.Logger?.LogDebug($"Configuration error stack trace: {exception.StackTrace}");
        }
        
        // Provide user-friendly recovery instructions
        ChronoParaPlugin.Logger?.LogInfo($"Using default value for '{configName}'. " +
            "If this persists, try resetting your configuration file or reinstalling the mod.");
    }
    
    /// <summary>
    /// Validates the overall configuration state and logs any issues
    /// </summary>
    /// <returns>True if configuration is valid, false if there are issues</returns>
    public static bool ValidateConfiguration()
    {
        bool isValid = true;
        
        try
        {
            // Test access to all configuration properties
            float cooldown = RecallCooldown;
            float duration = RewindDuration;
            float killWindow = RecallKillWindow;
            bool canSpawn = CanSpawnInChests;
            int historySize = MaxHistorySize;
            
            // Log current configuration state
            ChronoParaPlugin.Logger?.LogInfo($"Configuration validation - " +
                $"Cooldown: {cooldown}s, Duration: {duration}s, Kill Window: {killWindow}s, Can Spawn: {canSpawn}, History Size: {historySize}");
            
            // Validate relationships between configuration values
            if (killWindow > duration)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Kill window ({killWindow}s) is longer than rewind duration ({duration}s). " +
                    "This may cause unexpected behavior.");
                isValid = false;
            }
            
            if (cooldown < duration)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Cooldown ({cooldown}s) is shorter than rewind duration ({duration}s). " +
                    "Players may be able to chain recalls rapidly.");
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Configuration validation failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets a detailed configuration report for debugging purposes
    /// </summary>
    /// <returns>A formatted string containing all configuration values and their sources</returns>
    public static string GetConfigurationReport()
    {
        try
        {
            var report = "=== Chronomancer's Paradox Configuration Report ===\n";
            
            // Core configuration values
            report += $"Recall Cooldown: {RecallCooldown}s\n";
            report += $"Rewind Duration: {RewindDuration}s\n";
            report += $"Recall Kill Window: {RecallKillWindow}s\n";
            report += $"Can Spawn In Chests: {CanSpawnInChests}\n";
            report += $"Recording Interval: {RecordingInterval}s\n";
            report += $"Max History Size: {MaxHistorySize} snapshots\n";
            
            // Configuration source information
            report += "\n=== Configuration Sources ===\n";
            report += $"RecallCooldown Source: {(ChronoParaPlugin.RecallCooldown != null ? "Config File" : "Default")}\n";
            report += $"RewindDuration Source: {(ChronoParaPlugin.RewindDuration != null ? "Config File" : "Default")}\n";
            report += $"RecallKillWindow Source: {(ChronoParaPlugin.RecallKillWindow != null ? "Config File" : "Default")}\n";
            report += $"CanSpawnInChests Source: {(ChronoParaPlugin.CanSpawnInChests != null ? "Config File" : "Default")}\n";
            
            // Debug mode status
            report += $"\nDebug Mode: {(ChronoParaPlugin.DebugMode?.Value == true ? "Enabled" : "Disabled")}\n";
            
            return report;
        }
        catch (Exception ex)
        {
            return $"Error generating configuration report: {ex.Message}";
        }
    }
    
    #endregion
}