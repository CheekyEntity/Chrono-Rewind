using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChronoPara.Modules;

/// <summary>
/// Configuration validation and user feedback system for the Chronomancer's Paradox mod
/// Provides comprehensive validation, user-friendly error messages, and recovery guidance
/// </summary>
public static class ConfigurationValidator
{
    #region Validation Results
    
    /// <summary>
    /// Represents the result of a configuration validation
    /// </summary>
    public struct ValidationResult
    {
        public bool IsValid;
        public List<ValidationIssue> Issues;
        public string Summary;
        
        public ValidationResult(bool isValid)
        {
            IsValid = isValid;
            Issues = new List<ValidationIssue>();
            Summary = string.Empty;
        }
    }
    
    /// <summary>
    /// Represents a specific configuration validation issue
    /// </summary>
    public struct ValidationIssue
    {
        public ValidationSeverity Severity;
        public string ConfigurationName;
        public string Issue;
        public string Recommendation;
        public object CurrentValue;
        public object RecommendedValue;
        
        public ValidationIssue(ValidationSeverity severity, string configName, string issue, string recommendation, object currentValue = null, object recommendedValue = null)
        {
            Severity = severity;
            ConfigurationName = configName;
            Issue = issue;
            Recommendation = recommendation;
            CurrentValue = currentValue;
            RecommendedValue = recommendedValue;
        }
    }
    
    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    #endregion
    
    #region Public Validation Methods
    
    /// <summary>
    /// Performs comprehensive validation of all configuration values
    /// </summary>
    /// <returns>Detailed validation result with issues and recommendations</returns>
    public static ValidationResult ValidateAllConfiguration()
    {
        var result = new ValidationResult(true);
        
        try
        {
            // Validate individual configuration values
            ValidateRecallCooldown(result);
            ValidateRewindDuration(result);
            ValidateRecallKillWindow(result);
            ValidateRecordingInterval(result);
            
            // Validate configuration relationships
            ValidateConfigurationRelationships(result);
            
            // Validate performance implications
            ValidatePerformanceImplications(result);
            
            // Generate summary
            GenerateValidationSummary(result);
            
            // Log validation results
            LogValidationResults(result);
            
            return result;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during configuration validation: {ex.Message}");
            
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Critical,
                "System",
                "Configuration validation system failed",
                "Check mod installation and dependencies",
                null,
                null
            ));
            
            return result;
        }
    }
    
    /// <summary>
    /// Validates the recall cooldown configuration
    /// </summary>
    private static void ValidateRecallCooldown(ValidationResult result)
    {
        try
        {
            float cooldown = ConfigManager.RecallCooldown;
            
            // Check for invalid values
            if (float.IsNaN(cooldown) || float.IsInfinity(cooldown))
            {
                result.IsValid = false;
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Recall Cooldown",
                    "Invalid numeric value detected",
                    "Reset to default value (45 seconds)",
                    cooldown,
                    45f
                ));
                return;
            }
            
            // Check for values outside reasonable bounds
            if (cooldown < 5f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Recall Cooldown",
                    "Very short cooldown may cause gameplay balance issues",
                    "Consider increasing to at least 15 seconds for balanced gameplay",
                    cooldown,
                    15f
                ));
            }
            else if (cooldown > 180f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Recall Cooldown",
                    "Very long cooldown may make the spell rarely useful",
                    "Consider reducing to 60-90 seconds for better gameplay experience",
                    cooldown,
                    60f
                ));
            }
            
            // Optimal range feedback
            if (cooldown >= 30f && cooldown <= 60f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Recall Cooldown",
                    "Configuration is in the optimal range",
                    "No changes needed",
                    cooldown,
                    null
                ));
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "Recall Cooldown",
                $"Failed to validate: {ex.Message}",
                "Check configuration file integrity",
                null,
                45f
            ));
        }
    }
    
    /// <summary>
    /// Validates the rewind duration configuration
    /// </summary>
    private static void ValidateRewindDuration(ValidationResult result)
    {
        try
        {
            float duration = ConfigManager.RewindDuration;
            
            // Check for invalid values
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                result.IsValid = false;
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Rewind Duration",
                    "Invalid or non-positive value detected",
                    "Reset to default value (3 seconds)",
                    duration,
                    3f
                ));
                return;
            }
            
            // Check for values outside reasonable bounds
            if (duration < 1f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Rewind Duration",
                    "Very short rewind duration may not provide meaningful tactical value",
                    "Consider increasing to at least 2 seconds",
                    duration,
                    2f
                ));
            }
            else if (duration > 15f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Rewind Duration",
                    "Very long rewind duration may cause memory and performance issues",
                    "Consider reducing to 5-8 seconds for optimal performance",
                    duration,
                    5f
                ));
            }
            
            // Performance implications
            int estimatedHistorySize = ConfigManager.MaxHistorySize;
            if (estimatedHistorySize > 500)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Rewind Duration",
                    $"Current setting will create large history buffers ({estimatedHistorySize} snapshots)",
                    "Consider reducing duration or increasing recording interval for better performance",
                    duration,
                    Math.Min(duration, 5f)
                ));
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "Rewind Duration",
                $"Failed to validate: {ex.Message}",
                "Check configuration file integrity",
                null,
                3f
            ));
        }
    }
    
    /// <summary>
    /// Validates the recall kill window configuration
    /// </summary>
    private static void ValidateRecallKillWindow(ValidationResult result)
    {
        try
        {
            float killWindow = ConfigManager.RecallKillWindow;
            
            // Check for invalid values
            if (float.IsNaN(killWindow) || float.IsInfinity(killWindow) || killWindow < 0f)
            {
                result.IsValid = false;
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Recall Kill Window",
                    "Invalid or negative value detected",
                    "Reset to default value (3 seconds)",
                    killWindow,
                    3f
                ));
                return;
            }
            
            // Check for values outside reasonable bounds
            if (killWindow > 30f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Recall Kill Window",
                    "Very long kill window may cause confusion about recall-related deaths",
                    "Consider reducing to 5-10 seconds for clearer death attribution",
                    killWindow,
                    5f
                ));
            }
            
            // Zero kill window is valid but should be noted
            if (killWindow == 0f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Recall Kill Window",
                    "Kill window is disabled - no recall-related death tracking",
                    "This is valid if you don't want recall-related death attribution",
                    killWindow,
                    null
                ));
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "Recall Kill Window",
                $"Failed to validate: {ex.Message}",
                "Check configuration file integrity",
                null,
                3f
            ));
        }
    }
    
    /// <summary>
    /// Validates the recording interval configuration
    /// </summary>
    private static void ValidateRecordingInterval(ValidationResult result)
    {
        try
        {
            float interval = ConfigManager.RecordingInterval;
            
            // Check for invalid values
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval <= 0f)
            {
                result.IsValid = false;
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Recording Interval",
                    "Invalid or non-positive value detected",
                    "Reset to default value (0.1 seconds)",
                    interval,
                    0.1f
                ));
                return;
            }
            
            // Check for performance implications
            if (interval < 0.05f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Recording Interval",
                    "Very frequent recording may impact performance",
                    "Consider increasing to 0.1 seconds for better performance",
                    interval,
                    0.1f
                ));
            }
            else if (interval > 0.5f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Recording Interval",
                    "Infrequent recording may reduce recall precision",
                    "Consider reducing to 0.1-0.2 seconds for better precision",
                    interval,
                    0.1f
                ));
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "Recording Interval",
                $"Failed to validate: {ex.Message}",
                "Check configuration file integrity",
                null,
                0.1f
            ));
        }
    }
    
    /// <summary>
    /// Validates relationships between configuration values
    /// </summary>
    private static void ValidateConfigurationRelationships(ValidationResult result)
    {
        try
        {
            float cooldown = ConfigManager.RecallCooldown;
            float duration = ConfigManager.RewindDuration;
            float killWindow = ConfigManager.RecallKillWindow;
            
            // Kill window should not be longer than rewind duration
            if (killWindow > duration && killWindow > 0f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Configuration Relationship",
                    $"Kill window ({killWindow}s) is longer than rewind duration ({duration}s)",
                    "Consider making kill window equal to or shorter than rewind duration",
                    $"Kill Window: {killWindow}s, Rewind Duration: {duration}s",
                    $"Kill Window: {Math.Min(killWindow, duration)}s"
                ));
            }
            
            // Very short cooldown compared to rewind duration may allow chaining
            if (cooldown < duration * 2f && cooldown > 0f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Configuration Relationship",
                    $"Cooldown ({cooldown}s) is less than twice the rewind duration ({duration}s)",
                    "This allows for potential recall chaining - ensure this is intentional",
                    $"Cooldown: {cooldown}s, Rewind Duration: {duration}s",
                    null
                ));
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "Configuration Relationship",
                $"Failed to validate relationships: {ex.Message}",
                "Check individual configuration values",
                null,
                null
            ));
        }
    }
    
    /// <summary>
    /// Validates performance implications of current configuration
    /// </summary>
    private static void ValidatePerformanceImplications(ValidationResult result)
    {
        try
        {
            int maxHistorySize = ConfigManager.MaxHistorySize;
            float duration = ConfigManager.RewindDuration;
            float interval = ConfigManager.RecordingInterval;
            
            // Estimate memory usage
            int estimatedMemoryBytes = maxHistorySize * (sizeof(float) * 5); // 5 floats per snapshot
            
            if (estimatedMemoryBytes > 50000) // More than ~50KB per player
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Performance",
                    $"High memory usage estimated: ~{estimatedMemoryBytes / 1024}KB per player",
                    "Consider reducing rewind duration or increasing recording interval",
                    $"Duration: {duration}s, Interval: {interval}s",
                    "Reduce duration to 3-5s or increase interval to 0.15s"
                ));
            }
            
            // Check for excessive history size
            if (maxHistorySize > 1000)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Performance",
                    $"Very large history buffer: {maxHistorySize} snapshots",
                    "This may impact performance with many players",
                    maxHistorySize,
                    500
                ));
            }
            
            // Check for very frequent recording
            float recordingsPerSecond = 1f / interval;
            if (recordingsPerSecond > 20f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Performance",
                    $"Very frequent recording: {recordingsPerSecond:F1} times per second",
                    "This may impact game performance",
                    interval,
                    0.1f
                ));
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "Performance",
                $"Failed to validate performance implications: {ex.Message}",
                "Monitor game performance during play",
                null,
                null
            ));
        }
    }
    
    #endregion
    
    #region User Feedback Methods
    
    /// <summary>
    /// Generates a user-friendly validation summary
    /// </summary>
    private static void GenerateValidationSummary(ValidationResult result)
    {
        try
        {
            int errorCount = 0;
            int warningCount = 0;
            int infoCount = 0;
            
            foreach (var issue in result.Issues)
            {
                switch (issue.Severity)
                {
                    case ValidationSeverity.Critical:
                    case ValidationSeverity.Error:
                        errorCount++;
                        break;
                    case ValidationSeverity.Warning:
                        warningCount++;
                        break;
                    case ValidationSeverity.Info:
                        infoCount++;
                        break;
                }
            }
            
            if (errorCount > 0)
            {
                result.Summary = $"Configuration validation found {errorCount} error(s), {warningCount} warning(s), and {infoCount} info message(s). Please review and fix errors.";
            }
            else if (warningCount > 0)
            {
                result.Summary = $"Configuration validation found {warningCount} warning(s) and {infoCount} info message(s). Review warnings for optimal experience.";
            }
            else
            {
                result.Summary = $"Configuration validation passed successfully with {infoCount} info message(s).";
            }
        }
        catch (Exception ex)
        {
            result.Summary = $"Error generating validation summary: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Logs validation results with appropriate severity levels
    /// </summary>
    private static void LogValidationResults(ValidationResult result)
    {
        try
        {
            // Log summary
            if (result.IsValid)
            {
                ChronoParaPlugin.Logger?.LogInfo($"Configuration Validation: {result.Summary}");
            }
            else
            {
                ChronoParaPlugin.Logger?.LogWarning($"Configuration Validation: {result.Summary}");
            }
            
            // Log individual issues
            foreach (var issue in result.Issues)
            {
                string message = $"[{issue.ConfigurationName}] {issue.Issue}";
                if (!string.IsNullOrEmpty(issue.Recommendation))
                {
                    message += $" | Recommendation: {issue.Recommendation}";
                }
                
                if (issue.CurrentValue != null)
                {
                    message += $" | Current: {issue.CurrentValue}";
                }
                
                if (issue.RecommendedValue != null)
                {
                    message += $" | Suggested: {issue.RecommendedValue}";
                }
                
                switch (issue.Severity)
                {
                    case ValidationSeverity.Critical:
                    case ValidationSeverity.Error:
                        ChronoParaPlugin.Logger?.LogError(message);
                        break;
                    case ValidationSeverity.Warning:
                        ChronoParaPlugin.Logger?.LogWarning(message);
                        break;
                    case ValidationSeverity.Info:
                        if (ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogInfo(message);
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error logging validation results: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Generates a detailed user-friendly configuration report
    /// </summary>
    /// <returns>Formatted configuration report with recommendations</returns>
    public static string GenerateUserFriendlyReport()
    {
        try
        {
            var validation = ValidateAllConfiguration();
            var report = "=== Chronomancer's Paradox Configuration Report ===\n\n";
            
            // Current configuration
            report += "Current Configuration:\n";
            report += $"  Recall Cooldown: {ConfigManager.RecallCooldown}s\n";
            report += $"  Rewind Duration: {ConfigManager.RewindDuration}s\n";
            report += $"  Recall Kill Window: {ConfigManager.RecallKillWindow}s\n";
            report += $"  Recording Interval: {ConfigManager.RecordingInterval}s\n";
            report += $"  Max History Size: {ConfigManager.MaxHistorySize} snapshots\n\n";
            
            // Validation summary
            report += $"Validation Status: {validation.Summary}\n\n";
            
            // Issues and recommendations
            if (validation.Issues.Count > 0)
            {
                report += "Issues and Recommendations:\n";
                
                foreach (var issue in validation.Issues)
                {
                    string severityIcon = issue.Severity switch
                    {
                        ValidationSeverity.Critical => "❌",
                        ValidationSeverity.Error => "❌",
                        ValidationSeverity.Warning => "⚠️",
                        ValidationSeverity.Info => "ℹ️",
                        _ => "•"
                    };
                    
                    report += $"{severityIcon} [{issue.ConfigurationName}] {issue.Issue}\n";
                    if (!string.IsNullOrEmpty(issue.Recommendation))
                    {
                        report += $"   💡 {issue.Recommendation}\n";
                    }
                    report += "\n";
                }
            }
            
            // Performance summary
            int maxHistorySize = ConfigManager.MaxHistorySize;
            int estimatedMemoryBytes = maxHistorySize * (sizeof(float) * 5);
            report += "Performance Summary:\n";
            report += $"  Estimated Memory per Player: ~{estimatedMemoryBytes / 1024}KB\n";
            report += $"  Recording Frequency: {1f / ConfigManager.RecordingInterval:F1} times/second\n";
            report += $"  History Buffer Size: {maxHistorySize} snapshots\n\n";
            
            return report;
        }
        catch (Exception ex)
        {
            return $"Error generating configuration report: {ex.Message}";
        }
    }
    
    #endregion
}