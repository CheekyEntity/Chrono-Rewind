using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChronoPara.Modules;

/// <summary>
/// Advanced diagnostic logging utility for the Chronomancer's Paradox mod
/// Provides comprehensive logging, debugging information, and error tracking
/// </summary>
public static class DiagnosticLogger
{
    #region Constants
    
    private const int MAX_LOG_ENTRIES = 1000; // Maximum number of log entries to keep in memory
    private const int MAX_ERROR_HISTORY = 100; // Maximum number of error entries to track
    
    #endregion
    
    #region Private Fields
    
    private static readonly Queue<LogEntry> recentLogs = new Queue<LogEntry>();
    private static readonly Queue<ErrorEntry> errorHistory = new Queue<ErrorEntry>();
    private static readonly Dictionary<string, int> errorCounts = new Dictionary<string, int>();
    private static readonly object logLock = new object();
    
    #endregion
    
    #region Public Logging Methods
    
    /// <summary>
    /// Logs detailed information about a network operation
    /// </summary>
    /// <param name="operation">Name of the network operation</param>
    /// <param name="success">Whether the operation succeeded</param>
    /// <param name="details">Additional details about the operation</param>
    /// <param name="duration">Duration of the operation in milliseconds</param>
    public static void LogNetworkOperation(string operation, bool success, string details = "", float duration = -1f)
    {
        try
        {
            var message = new StringBuilder();
            message.Append($"[NETWORK] {operation}: {(success ? "SUCCESS" : "FAILED")}");
            
            if (duration >= 0f)
            {
                message.Append($" ({duration:F2}ms)");
            }
            
            if (!string.IsNullOrEmpty(details))
            {
                message.Append($" - {details}");
            }
            
            if (success)
            {
                LogInfo(message.ToString(), LogCategory.Network);
            }
            else
            {
                LogError(message.ToString(), LogCategory.Network);
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging network operation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs detailed information about a recall operation
    /// </summary>
    /// <param name="playerName">Name of the player performing the recall</param>
    /// <param name="success">Whether the recall succeeded</param>
    /// <param name="fromPosition">Starting position</param>
    /// <param name="toPosition">Target position</param>
    /// <param name="healthBefore">Health before recall</param>
    /// <param name="healthAfter">Health after recall</param>
    /// <param name="reason">Reason for failure (if applicable)</param>
    public static void LogRecallOperation(string playerName, bool success, Vector3 fromPosition, Vector3 toPosition, 
        float healthBefore, float healthAfter, string reason = "")
    {
        try
        {
            var message = new StringBuilder();
            message.Append($"[RECALL] Player {playerName}: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success)
            {
                float distance = Vector3.Distance(fromPosition, toPosition);
                float healthChange = healthAfter - healthBefore;
                
                message.Append($" - Teleported {distance:F2}m, Health: {healthBefore:F1} -> {healthAfter:F1} ({healthChange:+F1;-F1;+0})");
            }
            else if (!string.IsNullOrEmpty(reason))
            {
                message.Append($" - {reason}");
            }
            
            if (success)
            {
                LogInfo(message.ToString(), LogCategory.Recall);
            }
            else
            {
                LogWarning(message.ToString(), LogCategory.Recall);
            }
            
            // Log detailed position information in debug mode
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                LogDebug($"Recall details - From: {fromPosition}, To: {toPosition}", LogCategory.Recall);
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging recall operation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs performance metrics for position tracking
    /// </summary>
    /// <param name="historySize">Current history size</param>
    /// <param name="recordingInterval">Recording interval</param>
    /// <param name="cleanupCount">Number of entries cleaned up</param>
    /// <param name="processingTime">Time taken for processing in milliseconds</param>
    public static void LogPerformanceMetrics(int historySize, float recordingInterval, int cleanupCount, float processingTime)
    {
        try
        {
            if (ChronoParaPlugin.DebugMode?.Value != true)
                return; // Only log performance metrics in debug mode
            
            var message = $"[PERFORMANCE] History: {historySize} entries, Interval: {recordingInterval:F3}s, " +
                         $"Cleanup: {cleanupCount} removed, Processing: {processingTime:F2}ms";
            
            LogDebug(message, LogCategory.Performance);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging performance metrics: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs configuration changes with before/after values
    /// </summary>
    /// <param name="configName">Name of the configuration setting</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    /// <param name="source">Source of the change</param>
    public static void LogConfigurationChange(string configName, object oldValue, object newValue, string source = "Unknown")
    {
        try
        {
            var message = $"[CONFIG] {configName} changed: {oldValue} -> {newValue} (Source: {source})";
            LogInfo(message, LogCategory.Configuration);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging configuration change: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs component lifecycle events (creation, destruction, etc.)
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <param name="gameObjectName">Name of the GameObject</param>
    /// <param name="event">The lifecycle event</param>
    /// <param name="details">Additional details</param>
    public static void LogComponentLifecycle(string componentName, string gameObjectName, ComponentLifecycleEvent @event, string details = "")
    {
        try
        {
            var message = $"[COMPONENT] {componentName} on {gameObjectName}: {@event}";
            
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            
            LogDebug(message, LogCategory.Component);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging component lifecycle: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Core Logging Methods
    
    /// <summary>
    /// Logs an informational message with category tracking
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="category">The log category</param>
    public static void LogInfo(string message, LogCategory category = LogCategory.General)
    {
        try
        {
            ChronoParaPlugin.Logger?.LogInfo(message);
            AddLogEntry(message, LogLevel.Info, category);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in LogInfo: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a warning message with category tracking
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="category">The log category</param>
    public static void LogWarning(string message, LogCategory category = LogCategory.General)
    {
        try
        {
            ChronoParaPlugin.Logger?.LogWarning(message);
            AddLogEntry(message, LogLevel.Warning, category);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in LogWarning: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs an error message with category tracking and error history
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="category">The log category</param>
    /// <param name="exception">Optional exception details</param>
    public static void LogError(string message, LogCategory category = LogCategory.General, Exception exception = null)
    {
        try
        {
            ChronoParaPlugin.Logger?.LogError(message);
            AddLogEntry(message, LogLevel.Error, category);
            AddErrorEntry(message, category, exception);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in LogError: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a debug message (only in debug mode)
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="category">The log category</param>
    public static void LogDebug(string message, LogCategory category = LogCategory.General)
    {
        try
        {
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug(message);
                AddLogEntry(message, LogLevel.Debug, category);
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in LogDebug: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Error Tracking
    
    /// <summary>
    /// Gets the count of errors for a specific category
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>Number of errors in the category</returns>
    public static int GetErrorCount(LogCategory category)
    {
        try
        {
            lock (logLock)
            {
                string key = category.ToString();
                return errorCounts.ContainsKey(key) ? errorCounts[key] : 0;
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error getting error count: {ex.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Gets the total error count across all categories
    /// </summary>
    /// <returns>Total number of errors</returns>
    public static int GetTotalErrorCount()
    {
        try
        {
            lock (logLock)
            {
                int total = 0;
                foreach (var count in errorCounts.Values)
                {
                    total += count;
                }
                return total;
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error getting total error count: {ex.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Resets error counts for all categories
    /// </summary>
    public static void ResetErrorCounts()
    {
        try
        {
            lock (logLock)
            {
                errorCounts.Clear();
                errorHistory.Clear();
                LogInfo("Error counts and history reset", LogCategory.System);
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error resetting error counts: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Diagnostic Reports
    
    /// <summary>
    /// Generates a comprehensive diagnostic report
    /// </summary>
    /// <returns>Formatted diagnostic report</returns>
    public static string GenerateDiagnosticReport()
    {
        try
        {
            var report = new StringBuilder();
            report.AppendLine("=== Chronomancer's Paradox Diagnostic Report ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            
            // System information
            report.AppendLine("=== System Information ===");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine($"Platform: {Application.platform}");
            report.AppendLine($"Game Time: {Time.time:F2}s");
            report.AppendLine($"Frame Count: {Time.frameCount}");
            report.AppendLine();
            
            // Configuration status
            report.AppendLine("=== Configuration Status ===");
            report.AppendLine(ConfigManager.GetConfigurationReport());
            report.AppendLine();
            
            // Error summary
            report.AppendLine("=== Error Summary ===");
            report.AppendLine($"Total Errors: {GetTotalErrorCount()}");
            
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                int count = GetErrorCount(category);
                if (count > 0)
                {
                    report.AppendLine($"  {category}: {count} errors");
                }
            }
            report.AppendLine();
            
            // Recent errors
            report.AppendLine("=== Recent Errors ===");
            lock (logLock)
            {
                var recentErrors = new List<ErrorEntry>(errorHistory);
                recentErrors.Reverse(); // Show most recent first
                
                int errorCount = Math.Min(10, recentErrors.Count); // Show last 10 errors
                for (int i = 0; i < errorCount; i++)
                {
                    var error = recentErrors[i];
                    report.AppendLine($"[{error.Timestamp:HH:mm:ss}] {error.Category}: {error.Message}");
                }
            }
            report.AppendLine();
            
            // Network status
            report.AppendLine("=== Network Status ===");
            report.AppendLine(NetworkErrorHandler.GetNetworkStatusReport());
            report.AppendLine();
            
            // Performance metrics
            report.AppendLine("=== Performance Metrics ===");
            report.AppendLine($"Log Entries in Memory: {recentLogs.Count}/{MAX_LOG_ENTRIES}");
            report.AppendLine($"Error Entries in Memory: {errorHistory.Count}/{MAX_ERROR_HISTORY}");
            
            return report.ToString();
        }
        catch (Exception ex)
        {
            return $"Error generating diagnostic report: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Generates a summary of recent log activity
    /// </summary>
    /// <param name="minutes">Number of minutes to look back</param>
    /// <returns>Formatted activity summary</returns>
    public static string GenerateActivitySummary(int minutes = 5)
    {
        try
        {
            var cutoffTime = DateTime.Now.AddMinutes(-minutes);
            var summary = new StringBuilder();
            summary.AppendLine($"=== Activity Summary (Last {minutes} minutes) ===");
            
            var categoryCounts = new Dictionary<LogCategory, Dictionary<LogLevel, int>>();
            
            lock (logLock)
            {
                foreach (var entry in recentLogs)
                {
                    if (entry.Timestamp >= cutoffTime)
                    {
                        if (!categoryCounts.ContainsKey(entry.Category))
                        {
                            categoryCounts[entry.Category] = new Dictionary<LogLevel, int>();
                        }
                        
                        if (!categoryCounts[entry.Category].ContainsKey(entry.Level))
                        {
                            categoryCounts[entry.Category][entry.Level] = 0;
                        }
                        
                        categoryCounts[entry.Category][entry.Level]++;
                    }
                }
            }
            
            foreach (var categoryPair in categoryCounts)
            {
                summary.AppendLine($"{categoryPair.Key}:");
                foreach (var levelPair in categoryPair.Value)
                {
                    summary.AppendLine($"  {levelPair.Key}: {levelPair.Value}");
                }
            }
            
            return summary.ToString();
        }
        catch (Exception ex)
        {
            return $"Error generating activity summary: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Adds a log entry to the internal tracking system
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="level">The log level</param>
    /// <param name="category">The log category</param>
    private static void AddLogEntry(string message, LogLevel level, LogCategory category)
    {
        try
        {
            lock (logLock)
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Category = category,
                    Message = message
                };
                
                recentLogs.Enqueue(entry);
                
                // Maintain size limit
                while (recentLogs.Count > MAX_LOG_ENTRIES)
                {
                    recentLogs.Dequeue();
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error adding log entry: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Adds an error entry to the error tracking system
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="category">The error category</param>
    /// <param name="exception">Optional exception details</param>
    private static void AddErrorEntry(string message, LogCategory category, Exception exception)
    {
        try
        {
            lock (logLock)
            {
                // Update error counts
                string key = category.ToString();
                if (!errorCounts.ContainsKey(key))
                {
                    errorCounts[key] = 0;
                }
                errorCounts[key]++;
                
                // Add to error history
                var errorEntry = new ErrorEntry
                {
                    Timestamp = DateTime.Now,
                    Category = category,
                    Message = message,
                    Exception = exception
                };
                
                errorHistory.Enqueue(errorEntry);
                
                // Maintain size limit
                while (errorHistory.Count > MAX_ERROR_HISTORY)
                {
                    errorHistory.Dequeue();
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error adding error entry: {ex.Message}");
        }
    }
    
    #endregion
}

#region Data Structures

/// <summary>
/// Represents a log entry in the diagnostic system
/// </summary>
internal struct LogEntry
{
    public DateTime Timestamp;
    public LogLevel Level;
    public LogCategory Category;
    public string Message;
}

/// <summary>
/// Represents an error entry in the diagnostic system
/// </summary>
internal struct ErrorEntry
{
    public DateTime Timestamp;
    public LogCategory Category;
    public string Message;
    public Exception Exception;
}

/// <summary>
/// Log levels for diagnostic tracking
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Categories for organizing log messages
/// </summary>
public enum LogCategory
{
    General,
    Network,
    Recall,
    Component,
    Configuration,
    Performance,
    System
}

/// <summary>
/// Component lifecycle events for tracking
/// </summary>
public enum ComponentLifecycleEvent
{
    Created,
    Initialized,
    Started,
    Enabled,
    Disabled,
    Destroyed,
    Error
}

#endregion