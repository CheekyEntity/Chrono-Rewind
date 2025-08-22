using System;
using UnityEngine;
using FishNet.Connection;

namespace ChronoPara.Modules;

/// <summary>
/// Centralized network error handling and recovery utilities for the Chronomancer's Paradox mod
/// Provides comprehensive error handling, logging, and recovery mechanisms for network operations
/// </summary>
public static class NetworkErrorHandler
{
    #region Constants
    
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const float RETRY_DELAY_BASE = 1.0f; // Base delay for exponential backoff
    private const float NETWORK_TIMEOUT = 10.0f; // Network operation timeout in seconds
    
    #endregion
    
    #region Network Validation
    
    /// <summary>
    /// Validates that a network connection is valid and ready for operations
    /// </summary>
    /// <param name="connection">The network connection to validate</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    /// <returns>True if the connection is valid</returns>
    public static bool ValidateConnection(NetworkConnection connection, string operationName = "network operation")
    {
        try
        {
            if (connection == null)
            {
                LogNetworkError($"{operationName} failed - connection is null", NetworkErrorType.NullConnection);
                return false;
            }
            
            if (!connection.IsValid)
            {
                LogNetworkError($"{operationName} failed - connection is invalid", NetworkErrorType.InvalidConnection);
                return false;
            }
            
            if (!connection.IsActive)
            {
                LogNetworkError($"{operationName} failed - connection is not active", NetworkErrorType.InactiveConnection);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogNetworkError($"Error validating connection for {operationName}: {ex.Message}", NetworkErrorType.ValidationError);
            return false;
        }
    }
    
    /// <summary>
    /// Validates that network components are properly initialized
    /// </summary>
    /// <param name="networkBehaviour">The network behaviour to validate</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    /// <returns>True if the network behaviour is ready</returns>
    public static bool ValidateNetworkBehaviour(FishUtilities.Network.CustomNetworkBehaviour networkBehaviour, string operationName = "network operation")
    {
        try
        {
            if (networkBehaviour == null)
            {
                LogNetworkError($"{operationName} failed - network behaviour is null", NetworkErrorType.NullComponent);
                return false;
            }
            
            // Check if network behaviour has the IsNetworkReady method via reflection
            var isNetworkReadyMethod = networkBehaviour.GetType().GetMethod("IsNetworkReady");
            if (isNetworkReadyMethod != null)
            {
                bool isReady = (bool)isNetworkReadyMethod.Invoke(networkBehaviour, null);
                if (!isReady)
                {
                    LogNetworkError($"{operationName} failed - network not ready", NetworkErrorType.NetworkNotReady);
                    return false;
                }
            }
            else
            {
                // If IsNetworkReady method doesn't exist, assume network is ready
                // This provides compatibility with different network behaviour implementations
                LogNetworkError($"{operationName} - IsNetworkReady method not found, assuming network is ready", NetworkErrorType.ValidationError);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogNetworkError($"Error validating network behaviour for {operationName}: {ex.Message}", NetworkErrorType.ValidationError);
            return false;
        }
    }
    
    #endregion
    
    #region Error Handling
    
    /// <summary>
    /// Handles network command errors with appropriate logging and recovery
    /// </summary>
    /// <param name="cmdId">The command ID that failed</param>
    /// <param name="sender">The sender connection</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="context">Additional context information</param>
    public static void HandleCommandError(uint cmdId, NetworkConnection sender, Exception exception, string context = "")
    {
        try
        {
            string senderInfo = sender != null ? $"Client {sender.ClientId}" : "Unknown sender";
            string errorMessage = $"Network command {cmdId} failed from {senderInfo}: {exception.Message}";
            
            if (!string.IsNullOrEmpty(context))
            {
                errorMessage += $" (Context: {context})";
            }
            
            LogNetworkError(errorMessage, NetworkErrorType.CommandError);
            
            // Log stack trace in debug mode
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Command error stack trace: {exception.StackTrace}");
            }
            
            // Provide recovery suggestions
            ProvideRecoveryGuidance(NetworkErrorType.CommandError, cmdId.ToString());
        }
        catch (Exception loggingEx)
        {
            // Fallback logging if error handling itself fails
            ChronoParaPlugin.Logger?.LogError($"Critical error in network error handler: {loggingEx.Message}");
        }
    }
    
    /// <summary>
    /// Handles RPC errors with appropriate logging and recovery
    /// </summary>
    /// <param name="rpcId">The RPC ID that failed</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="context">Additional context information</param>
    public static void HandleRpcError(uint rpcId, Exception exception, string context = "")
    {
        try
        {
            string errorMessage = $"Network RPC {rpcId} failed: {exception.Message}";
            
            if (!string.IsNullOrEmpty(context))
            {
                errorMessage += $" (Context: {context})";
            }
            
            LogNetworkError(errorMessage, NetworkErrorType.RpcError);
            
            // Log stack trace in debug mode
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"RPC error stack trace: {exception.StackTrace}");
            }
            
            // Provide recovery suggestions
            ProvideRecoveryGuidance(NetworkErrorType.RpcError, rpcId.ToString());
        }
        catch (Exception loggingEx)
        {
            // Fallback logging if error handling itself fails
            ChronoParaPlugin.Logger?.LogError($"Critical error in RPC error handler: {loggingEx.Message}");
        }
    }
    
    /// <summary>
    /// Handles general network operation errors
    /// </summary>
    /// <param name="operationName">Name of the operation that failed</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="severity">The severity level of the error</param>
    public static void HandleNetworkError(string operationName, Exception exception, NetworkErrorSeverity severity = NetworkErrorSeverity.Error)
    {
        try
        {
            string errorMessage = $"Network operation '{operationName}' failed: {exception.Message}";
            
            NetworkErrorType errorType = ClassifyException(exception);
            
            switch (severity)
            {
                case NetworkErrorSeverity.Warning:
                    ChronoParaPlugin.Logger?.LogWarning(errorMessage);
                    break;
                case NetworkErrorSeverity.Error:
                    LogNetworkError(errorMessage, errorType);
                    break;
                case NetworkErrorSeverity.Critical:
                    ChronoParaPlugin.Logger?.LogError($"CRITICAL: {errorMessage}");
                    break;
            }
            
            // Log stack trace for errors and critical issues
            if (severity >= NetworkErrorSeverity.Error && ChronoParaPlugin.DebugMode?.Value == true)
            {
                ChronoParaPlugin.Logger?.LogDebug($"Network error stack trace: {exception.StackTrace}");
            }
            
            // Provide recovery suggestions for errors and critical issues
            if (severity >= NetworkErrorSeverity.Error)
            {
                ProvideRecoveryGuidance(errorType, operationName);
            }
        }
        catch (Exception loggingEx)
        {
            // Fallback logging if error handling itself fails
            ChronoParaPlugin.Logger?.LogError($"Critical error in network error handler: {loggingEx.Message}");
        }
    }
    
    #endregion
    
    #region Recovery and Guidance
    
    /// <summary>
    /// Provides user-friendly recovery guidance based on the error type
    /// </summary>
    /// <param name="errorType">The type of network error that occurred</param>
    /// <param name="context">Additional context for the error</param>
    private static void ProvideRecoveryGuidance(NetworkErrorType errorType, string context = "")
    {
        try
        {
            string guidance = errorType switch
            {
                NetworkErrorType.NullConnection => "Check your network connection and try reconnecting to the server.",
                NetworkErrorType.InvalidConnection => "Your connection to the server may have been lost. Try reconnecting.",
                NetworkErrorType.InactiveConnection => "Connection is inactive. Please check your network status and reconnect.",
                NetworkErrorType.NetworkNotReady => "Network is not ready. Wait a moment and try again.",
                NetworkErrorType.CommandError => "Network command failed. This may be temporary - try the action again.",
                NetworkErrorType.RpcError => "Network synchronization failed. Other players may not see the effect immediately.",
                NetworkErrorType.Timeout => "Network operation timed out. Check your connection and try again.",
                NetworkErrorType.ValidationError => "Network validation failed. This may indicate a mod conflict or corruption.",
                NetworkErrorType.ComponentMissing => "Required network component is missing. Try restarting the game.",
                NetworkErrorType.Unknown => "An unknown network error occurred. Check logs for more details.",
                _ => "A network error occurred. Check your connection and try again."
            };
            
            ChronoParaPlugin.Logger?.LogInfo($"Recovery guidance: {guidance}");
            
            // Add context-specific guidance
            if (!string.IsNullOrEmpty(context))
            {
                string contextGuidance = GetContextSpecificGuidance(context);
                if (!string.IsNullOrEmpty(contextGuidance))
                {
                    ChronoParaPlugin.Logger?.LogInfo($"Additional guidance for {context}: {contextGuidance}");
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing recovery guidance: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets context-specific recovery guidance
    /// </summary>
    /// <param name="context">The context or operation name</param>
    /// <returns>Context-specific guidance string</returns>
    private static string GetContextSpecificGuidance(string context)
    {
        return context.ToLower() switch
        {
            "recall" or "temporal recall" => "If Temporal Recall is not working, check that you have position history and are not on cooldown.",
            "position tracking" => "Position tracking issues may resolve themselves. If persistent, try moving to reset tracking.",
            "1001" => "Recall command failed. Check that you own the player and are not on cooldown.",
            "1002" => "Recall effects failed to synchronize. The recall may have worked but effects didn't display.",
            _ => ""
        };
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Classifies an exception to determine the appropriate error type
    /// </summary>
    /// <param name="exception">The exception to classify</param>
    /// <returns>The classified error type</returns>
    private static NetworkErrorType ClassifyException(Exception exception)
    {
        try
        {
            return exception switch
            {
                ArgumentNullException => NetworkErrorType.NullConnection,
                TimeoutException => NetworkErrorType.Timeout,
                InvalidOperationException => NetworkErrorType.InvalidConnection,
                NullReferenceException => NetworkErrorType.ComponentMissing,
                _ => NetworkErrorType.Unknown
            };
        }
        catch
        {
            return NetworkErrorType.Unknown;
        }
    }
    
    /// <summary>
    /// Logs a network error with consistent formatting
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorType">The type of error</param>
    private static void LogNetworkError(string message, NetworkErrorType errorType)
    {
        try
        {
            string formattedMessage = $"[NETWORK ERROR - {errorType}] {message}";
            ChronoParaPlugin.Logger?.LogError(formattedMessage);
        }
        catch (Exception ex)
        {
            // Fallback to basic logging if formatted logging fails
            ChronoParaPlugin.Logger?.LogError($"Network error: {message} (Logging error: {ex.Message})");
        }
    }
    
    /// <summary>
    /// Checks if a network operation should be retried based on the error type
    /// </summary>
    /// <param name="errorType">The type of error that occurred</param>
    /// <returns>True if the operation should be retried</returns>
    public static bool ShouldRetry(NetworkErrorType errorType)
    {
        return errorType switch
        {
            NetworkErrorType.Timeout => true,
            NetworkErrorType.NetworkNotReady => true,
            NetworkErrorType.InactiveConnection => true,
            NetworkErrorType.CommandError => true,
            NetworkErrorType.RpcError => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Calculates retry delay using exponential backoff
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based)</param>
    /// <returns>Delay in seconds before next retry</returns>
    public static float CalculateRetryDelay(int attemptNumber)
    {
        try
        {
            if (attemptNumber <= 0) return 0f;
            
            // Exponential backoff: base * 2^(attempt-1)
            float delay = RETRY_DELAY_BASE * Mathf.Pow(2f, attemptNumber - 1);
            
            // Cap the maximum delay at 30 seconds
            return Mathf.Min(delay, 30f);
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error calculating retry delay: {ex.Message}");
            return RETRY_DELAY_BASE; // Fallback to base delay
        }
    }
    
    /// <summary>
    /// Gets a comprehensive network status report for debugging
    /// </summary>
    /// <returns>Formatted network status report</returns>
    public static string GetNetworkStatusReport()
    {
        try
        {
            var report = "=== Network Status Report ===\n";
            
            // Add basic network information
            report += $"Unity Network Time: {Time.time:F3}\n";
            report += $"Fixed Time: {Time.fixedTime:F3}\n";
            report += $"Frame Count: {Time.frameCount}\n";
            
            // Add configuration information
            report += "\n=== Configuration ===\n";
            report += $"Max Retry Attempts: {MAX_RETRY_ATTEMPTS}\n";
            report += $"Retry Base Delay: {RETRY_DELAY_BASE}s\n";
            report += $"Network Timeout: {NETWORK_TIMEOUT}s\n";
            
            return report;
        }
        catch (Exception ex)
        {
            return $"Error generating network status report: {ex.Message}";
        }
    }
    
    #endregion
}

#region Enums

/// <summary>
/// Types of network errors that can occur
/// </summary>
public enum NetworkErrorType
{
    Unknown,
    NullConnection,
    InvalidConnection,
    InactiveConnection,
    NetworkNotReady,
    CommandError,
    RpcError,
    Timeout,
    ValidationError,
    ComponentMissing,
    NullComponent
}

/// <summary>
/// Severity levels for network errors
/// </summary>
public enum NetworkErrorSeverity
{
    Warning,
    Error,
    Critical
}

#endregion