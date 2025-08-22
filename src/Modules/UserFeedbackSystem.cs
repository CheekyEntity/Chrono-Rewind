using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChronoPara.Modules;

/// <summary>
/// User feedback system for the Chronomancer's Paradox mod
/// Provides clear, actionable guidance for configuration and performance optimization
/// </summary>
public static class UserFeedbackSystem
{
    #region Feedback Types
    
    /// <summary>
    /// Types of user feedback messages
    /// </summary>
    public enum FeedbackType
    {
        Information,
        Success,
        Warning,
        Error,
        Performance,
        Configuration
    }
    
    /// <summary>
    /// Feedback message structure
    /// </summary>
    public struct FeedbackMessage
    {
        public FeedbackType Type;
        public string Title;
        public string Message;
        public List<string> Recommendations;
        public DateTime Timestamp;
        
        public FeedbackMessage(FeedbackType type, string title, string message, List<string> recommendations = null)
        {
            Type = type;
            Title = title;
            Message = message;
            Recommendations = recommendations ?? new List<string>();
            Timestamp = DateTime.Now;
        }
    }
    
    #endregion
    
    #region Configuration Feedback
    
    /// <summary>
    /// Provides feedback on configuration optimization
    /// </summary>
    public static void ProvideConfigurationFeedback()
    {
        try
        {
            var validation = ConfigurationValidator.ValidateAllConfiguration();
            
            if (validation.IsValid)
            {
                ShowFeedback(new FeedbackMessage(
                    FeedbackType.Success,
                    "Configuration Optimized",
                    "Your Chronomancer's Paradox configuration is optimized for good performance and gameplay balance.",
                    new List<string> { "No changes needed - enjoy your temporal magic!" }
                ));
            }
            else
            {
                var recommendations = new List<string>();
                
                foreach (var issue in validation.Issues)
                {
                    if (issue.Severity == ConfigurationValidator.ValidationSeverity.Error ||
                        issue.Severity == ConfigurationValidator.ValidationSeverity.Warning)
                    {
                        recommendations.Add($"{issue.ConfigurationName}: {issue.Recommendation}");
                    }
                }
                
                ShowFeedback(new FeedbackMessage(
                    FeedbackType.Configuration,
                    "Configuration Needs Attention",
                    "Your configuration has some issues that could affect performance or gameplay.",
                    recommendations
                ));
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing configuration feedback: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Provides specific feedback for performance issues
    /// </summary>
    /// <param name="status">Current performance status</param>
    public static void ProvidePerformanceFeedback(PerformanceManager.PerformanceStatus status)
    {
        try
        {
            switch (status)
            {
                case PerformanceManager.PerformanceStatus.Excellent:
                    ShowFeedback(new FeedbackMessage(
                        FeedbackType.Success,
                        "Excellent Performance",
                        "The Chronomancer's Paradox mod is running at optimal performance!",
                        new List<string> { "Your system is handling the temporal magic perfectly." }
                    ));
                    break;
                    
                case PerformanceManager.PerformanceStatus.Good:
                    ShowFeedback(new FeedbackMessage(
                        FeedbackType.Information,
                        "Good Performance",
                        "The mod is performing well with minor resource usage.",
                        new List<string> { "Performance is good - no immediate action needed." }
                    ));
                    break;
                    
                case PerformanceManager.PerformanceStatus.Fair:
                    ShowFeedback(new FeedbackMessage(
                        FeedbackType.Warning,
                        "Fair Performance",
                        "The mod is using moderate resources. Consider optimization for better experience.",
                        new List<string>
                        {
                            "Reduce Rewind Duration to 2-3 seconds",
                            "Increase Recording Interval to 0.15 seconds",
                            "Monitor other running applications"
                        }
                    ));
                    break;
                    
                case PerformanceManager.PerformanceStatus.Poor:
                    ShowFeedback(new FeedbackMessage(
                        FeedbackType.Performance,
                        "Performance Warning",
                        "The mod is using significant resources and may impact game performance.",
                        new List<string>
                        {
                            "Reduce Rewind Duration to 2 seconds",
                            "Increase Recording Interval to 0.2 seconds",
                            "Close unnecessary applications",
                            "Consider restarting the game"
                        }
                    ));
                    break;
                    
                case PerformanceManager.PerformanceStatus.Critical:
                    ShowFeedback(new FeedbackMessage(
                        FeedbackType.Error,
                        "Critical Performance Issue",
                        "The mod is using excessive resources! Immediate action required.",
                        new List<string>
                        {
                            "URGENT: Reduce Rewind Duration to 1-2 seconds",
                            "URGENT: Increase Recording Interval to 0.3 seconds",
                            "Close all other applications",
                            "Restart the game immediately",
                            "Consider temporarily disabling the mod if issues persist"
                        }
                    ));
                    break;
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing performance feedback: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Installation and Setup Feedback
    
    /// <summary>
    /// Provides feedback on mod installation and setup
    /// </summary>
    public static void ProvideInstallationFeedback()
    {
        try
        {
            var issues = new List<string>();
            var successes = new List<string>();
            
            // Check asset bundle
            if (AssetManager.IsBundleLoaded)
            {
                successes.Add("✅ Custom assets loaded successfully");
            }
            else
            {
                issues.Add("⚠️ Custom asset bundle not found - some effects may be missing");
            }
            
            // Check death tracker
            if (DeathTracker.IsInitialized)
            {
                successes.Add("✅ Death tracking system initialized");
            }
            else
            {
                issues.Add("⚠️ Death tracking system failed to initialize");
            }
            
            // Check configuration
            var configValidation = ConfigurationValidator.ValidateAllConfiguration();
            if (configValidation.IsValid)
            {
                successes.Add("✅ Configuration is valid");
            }
            else
            {
                issues.Add($"⚠️ Configuration has {configValidation.Issues.Count} issues");
            }
            
            // Provide feedback
            if (issues.Count == 0)
            {
                ShowFeedback(new FeedbackMessage(
                    FeedbackType.Success,
                    "Installation Complete",
                    "Chronomancer's Paradox is fully installed and ready to use!",
                    successes
                ));
            }
            else
            {
                var recommendations = new List<string>();
                recommendations.AddRange(issues);
                recommendations.Add("Check the mod installation guide for troubleshooting");
                recommendations.Add("Verify all dependencies are installed");
                
                ShowFeedback(new FeedbackMessage(
                    FeedbackType.Warning,
                    "Installation Issues Detected",
                    "The mod is installed but some components have issues.",
                    recommendations
                ));
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing installation feedback: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Gameplay Feedback
    
    /// <summary>
    /// Provides feedback on gameplay mechanics and usage
    /// </summary>
    public static void ProvideGameplayFeedback()
    {
        try
        {
            var tips = new List<string>
            {
                "🕐 Temporal Recall rewinds your position and health by " + ConfigManager.RewindDuration + " seconds",
                "⏱️ The spell has a " + ConfigManager.RecallCooldown + " second cooldown",
                "💀 Deaths within " + ConfigManager.RecallKillWindow + " seconds after recall count as recall-related",
                "🎯 Use recall strategically to escape danger or reposition for attacks",
                "⚡ The spell can be found in team chests like other spells",
                "🔊 Recall creates audio and visual effects that enemies can see and hear"
            };
            
            ShowFeedback(new FeedbackMessage(
                FeedbackType.Information,
                "Temporal Magic Guide",
                "Master the art of time manipulation with these tips:",
                tips
            ));
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing gameplay feedback: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Troubleshooting Feedback
    
    /// <summary>
    /// Provides troubleshooting guidance for common issues
    /// </summary>
    /// <param name="issueType">Type of issue encountered</param>
    public static void ProvideTroubleshootingFeedback(string issueType)
    {
        try
        {
            var recommendations = new List<string>();
            string title = "Troubleshooting Guide";
            string message = "Here's how to resolve common issues:";
            
            switch (issueType.ToLower())
            {
                case "spell_not_found":
                    title = "Spell Not Appearing";
                    message = "If Temporal Recall isn't appearing in team chests:";
                    recommendations.AddRange(new[]
                    {
                        "Verify BlackMagicAPI is installed and up to date",
                        "Check that the mod loaded successfully in the console",
                        "Try restarting the game",
                        "Ensure no other mods are conflicting with spell registration"
                    });
                    break;
                    
                case "performance_issues":
                    title = "Performance Problems";
                    message = "If you're experiencing lag or stuttering:";
                    recommendations.AddRange(new[]
                    {
                        "Reduce Rewind Duration in mod settings",
                        "Increase Recording Interval in mod settings",
                        "Close other resource-intensive applications",
                        "Lower game graphics settings",
                        "Check for mod conflicts"
                    });
                    break;
                    
                case "recall_not_working":
                    title = "Recall Not Working";
                    message = "If Temporal Recall isn't functioning properly:";
                    recommendations.AddRange(new[]
                    {
                        "Ensure you're not on cooldown (check spell icon)",
                        "Verify you have position history (move around for a few seconds first)",
                        "Check that FishUtilities is installed for networking",
                        "Try reloading the game",
                        "Enable debug mode to see detailed error messages"
                    });
                    break;
                    
                case "network_issues":
                    title = "Multiplayer Issues";
                    message = "If recall isn't working properly in multiplayer:";
                    recommendations.AddRange(new[]
                    {
                        "Ensure all players have the mod installed",
                        "Check that FishUtilities is working correctly",
                        "Verify network connectivity",
                        "Try hosting a new game session",
                        "Check for mod version mismatches between players"
                    });
                    break;
                    
                default:
                    title = "General Troubleshooting";
                    message = "For general issues with the mod:";
                    recommendations.AddRange(new[]
                    {
                        "Check the console for error messages",
                        "Verify all dependencies are installed",
                        "Try disabling other mods to check for conflicts",
                        "Reinstall the mod if files may be corrupted",
                        "Check the mod's documentation for known issues"
                    });
                    break;
            }
            
            ShowFeedback(new FeedbackMessage(
                FeedbackType.Information,
                title,
                message,
                recommendations
            ));
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing troubleshooting feedback: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Feedback Display
    
    /// <summary>
    /// Shows feedback message to the user
    /// </summary>
    /// <param name="feedback">The feedback message to display</param>
    private static void ShowFeedback(FeedbackMessage feedback)
    {
        try
        {
            // Format the message for logging
            string logMessage = FormatFeedbackForLog(feedback);
            
            // Log with appropriate level based on feedback type
            switch (feedback.Type)
            {
                case FeedbackType.Success:
                case FeedbackType.Information:
                    ChronoParaPlugin.Logger?.LogInfo(logMessage);
                    break;
                    
                case FeedbackType.Warning:
                case FeedbackType.Performance:
                case FeedbackType.Configuration:
                    ChronoParaPlugin.Logger?.LogWarning(logMessage);
                    break;
                    
                case FeedbackType.Error:
                    ChronoParaPlugin.Logger?.LogError(logMessage);
                    break;
            }
            
            // In debug mode, also log detailed recommendations
            if (ChronoParaPlugin.DebugMode?.Value == true && feedback.Recommendations.Count > 0)
            {
                ChronoParaPlugin.Logger?.LogDebug("Detailed recommendations:");
                foreach (var recommendation in feedback.Recommendations)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"  • {recommendation}");
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error showing feedback: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Formats feedback message for logging
    /// </summary>
    /// <param name="feedback">The feedback to format</param>
    /// <returns>Formatted log message</returns>
    private static string FormatFeedbackForLog(FeedbackMessage feedback)
    {
        try
        {
            string emoji = feedback.Type switch
            {
                FeedbackType.Success => "✅",
                FeedbackType.Information => "ℹ️",
                FeedbackType.Warning => "⚠️",
                FeedbackType.Error => "❌",
                FeedbackType.Performance => "⚡",
                FeedbackType.Configuration => "⚙️",
                _ => "•"
            };
            
            string message = $"{emoji} {feedback.Title}: {feedback.Message}";
            
            if (feedback.Recommendations.Count > 0)
            {
                message += " | Recommendations: " + string.Join(", ", feedback.Recommendations);
            }
            
            return message;
        }
        catch (Exception ex)
        {
            return $"Error formatting feedback: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Provides comprehensive feedback on mod status
    /// </summary>
    public static void ProvideComprehensiveFeedback()
    {
        try
        {
            // Installation feedback
            ProvideInstallationFeedback();
            
            // Configuration feedback
            ProvideConfigurationFeedback();
            
            // Performance feedback
            if (PerformanceManager.IsInitialized)
            {
                ProvidePerformanceFeedback(PerformanceManager.CurrentStatus);
            }
            
            // Gameplay tips (only in debug mode to avoid spam)
            if (ChronoParaPlugin.DebugMode?.Value == true)
            {
                ProvideGameplayFeedback();
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing comprehensive feedback: {ex.Message}");
        }
    }
    
    #endregion
}