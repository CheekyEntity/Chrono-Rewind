using System;
using UnityEngine;

namespace ChronoPara.Modules;

/// <summary>
/// Utility class for validating Unity components and providing graceful degradation
/// Ensures robust operation when components are missing or in invalid states
/// </summary>
public static class ComponentValidator
{
    #region Component Validation
    
    /// <summary>
    /// Validates that a component exists and is in a usable state
    /// </summary>
    /// <typeparam name="T">The component type to validate</typeparam>
    /// <param name="component">The component to validate</param>
    /// <param name="componentName">Name of the component for logging</param>
    /// <param name="isRequired">Whether this component is required for operation</param>
    /// <returns>True if the component is valid and usable</returns>
    public static bool ValidateComponent<T>(T component, string componentName, bool isRequired = true) where T : Component
    {
        try
        {
            if (component == null)
            {
                LogComponentError($"{componentName} component is null", isRequired);
                return false;
            }
            
            if (component.gameObject == null)
            {
                LogComponentError($"{componentName} component has null GameObject", isRequired);
                return false;
            }
            
            if (!component.gameObject.activeInHierarchy)
            {
                LogComponentError($"{componentName} component's GameObject is not active", isRequired);
                return false;
            }
            
            // Component-specific validation
            if (!ValidateSpecificComponent(component, componentName))
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogComponentError($"Error validating {componentName} component: {ex.Message}", isRequired);
            return false;
        }
    }
    
    /// <summary>
    /// Validates multiple components at once
    /// </summary>
    /// <param name="validations">Array of component validation tuples</param>
    /// <returns>True if all validations pass</returns>
    public static bool ValidateComponents(params (Component component, string name, bool required)[] validations)
    {
        try
        {
            bool allValid = true;
            
            foreach (var (component, name, required) in validations)
            {
                if (!ValidateComponent(component, name, required))
                {
                    allValid = false;
                    
                    // If a required component fails, we can stop early
                    if (required)
                    {
                        LogComponentError($"Required component {name} validation failed - stopping validation", true);
                        return false;
                    }
                }
            }
            
            return allValid;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error during multi-component validation: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to get a component with error handling and fallback options
    /// </summary>
    /// <typeparam name="T">The component type to get</typeparam>
    /// <param name="gameObject">The GameObject to search</param>
    /// <param name="componentName">Name of the component for logging</param>
    /// <param name="isRequired">Whether this component is required</param>
    /// <param name="searchChildren">Whether to search child objects if not found on parent</param>
    /// <returns>The component if found, null otherwise</returns>
    public static T SafeGetComponent<T>(GameObject gameObject, string componentName, bool isRequired = true, bool searchChildren = false) where T : Component
    {
        try
        {
            if (gameObject == null)
            {
                LogComponentError($"Cannot get {componentName} - GameObject is null", isRequired);
                return null;
            }
            
            // Try to get component from the GameObject
            T component = gameObject.GetComponent<T>();
            
            // If not found and searchChildren is enabled, try children
            if (component == null && searchChildren)
            {
                component = gameObject.GetComponentInChildren<T>();
                
                if (component != null)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"{componentName} found in child object: {component.gameObject.name}");
                }
            }
            
            if (component == null)
            {
                LogComponentError($"{componentName} component not found on {gameObject.name}", isRequired);
                
                if (isRequired)
                {
                    ProvideComponentRecoveryGuidance<T>(componentName, gameObject);
                }
                
                return null;
            }
            
            // Validate the found component
            if (!ValidateComponent(component, componentName, isRequired))
            {
                return null;
            }
            
            return component;
        }
        catch (Exception ex)
        {
            LogComponentError($"Error getting {componentName} component: {ex.Message}", isRequired);
            return null;
        }
    }
    
    /// <summary>
    /// Attempts to add a component if it doesn't exist, with error handling
    /// </summary>
    /// <typeparam name="T">The component type to add</typeparam>
    /// <param name="gameObject">The GameObject to add the component to</param>
    /// <param name="componentName">Name of the component for logging</param>
    /// <returns>The existing or newly added component, null if failed</returns>
    public static T SafeAddComponent<T>(GameObject gameObject, string componentName) where T : Component
    {
        try
        {
            if (gameObject == null)
            {
                LogComponentError($"Cannot add {componentName} - GameObject is null", true);
                return null;
            }
            
            // Check if component already exists
            T existingComponent = gameObject.GetComponent<T>();
            if (existingComponent != null)
            {
                ChronoParaPlugin.Logger?.LogDebug($"{componentName} already exists on {gameObject.name}");
                return existingComponent;
            }
            
            // Try to add the component
            T newComponent = gameObject.AddComponent<T>();
            
            if (newComponent == null)
            {
                LogComponentError($"Failed to add {componentName} component to {gameObject.name}", true);
                return null;
            }
            
            ChronoParaPlugin.Logger?.LogInfo($"Successfully added {componentName} component to {gameObject.name}");
            return newComponent;
        }
        catch (Exception ex)
        {
            LogComponentError($"Error adding {componentName} component: {ex.Message}", true);
            return null;
        }
    }
    
    #endregion
    
    #region Health Validation
    
    /// <summary>
    /// Validates that a health value is within acceptable bounds
    /// </summary>
    /// <param name="health">The health value to validate</param>
    /// <param name="context">Context for logging purposes</param>
    /// <returns>True if the health value is valid</returns>
    public static bool IsValidHealth(float health, string context = "health check")
    {
        try
        {
            if (float.IsNaN(health))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid health value (NaN) detected in {context}");
                return false;
            }
            
            if (float.IsInfinity(health))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid health value (Infinity) detected in {context}");
                return false;
            }
            
            if (health < 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Negative health value ({health}) detected in {context}");
                return false;
            }
            
            if (health > 10000f) // Reasonable upper bound for modded games
            {
                ChronoParaPlugin.Logger?.LogWarning($"Extremely high health value ({health}) detected in {context}");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating health in {context}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Sanitizes a health value to ensure it's within acceptable bounds
    /// </summary>
    /// <param name="health">The health value to sanitize</param>
    /// <param name="defaultHealth">Default health to use if sanitization fails</param>
    /// <param name="context">Context for logging purposes</param>
    /// <returns>A sanitized health value</returns>
    public static float SanitizeHealth(float health, float defaultHealth = 100f, string context = "health sanitization")
    {
        try
        {
            if (IsValidHealth(health, context))
            {
                return health;
            }
            
            // Handle specific invalid cases
            if (float.IsNaN(health) || float.IsInfinity(health))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Replacing invalid health value with default ({defaultHealth}) in {context}");
                return defaultHealth;
            }
            
            if (health < 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Clamping negative health value to 0 in {context}");
                return 0f;
            }
            
            if (health > 10000f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Clamping excessive health value to 10000 in {context}");
                return 10000f;
            }
            
            return health;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error sanitizing health in {context}: {ex.Message}");
            return defaultHealth;
        }
    }
    
    #endregion
    
    #region Position Validation
    
    /// <summary>
    /// Validates that a position vector is within reasonable bounds
    /// </summary>
    /// <param name="position">The position to validate</param>
    /// <param name="context">Context for logging purposes</param>
    /// <returns>True if the position is valid</returns>
    public static bool IsValidPosition(Vector3 position, string context = "position check")
    {
        try
        {
            // Check for NaN values
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid position (NaN) detected in {context}: {position}");
                return false;
            }
            
            // Check for infinity values
            if (float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid position (Infinity) detected in {context}: {position}");
                return false;
            }
            
            // Check for extremely large values that might indicate corruption
            const float MAX_COORDINATE = 50000f; // Reasonable bound for most game worlds
            if (Mathf.Abs(position.x) > MAX_COORDINATE || 
                Mathf.Abs(position.y) > MAX_COORDINATE || 
                Mathf.Abs(position.z) > MAX_COORDINATE)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Position coordinates too large in {context}: {position}");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating position in {context}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Sanitizes a position vector to ensure it's within acceptable bounds
    /// </summary>
    /// <param name="position">The position to sanitize</param>
    /// <param name="defaultPosition">Default position to use if sanitization fails</param>
    /// <param name="context">Context for logging purposes</param>
    /// <returns>A sanitized position vector</returns>
    public static Vector3 SanitizePosition(Vector3 position, Vector3 defaultPosition = default, string context = "position sanitization")
    {
        try
        {
            if (IsValidPosition(position, context))
            {
                return position;
            }
            
            Vector3 sanitized = position;
            
            // Handle NaN or infinity values
            if (float.IsNaN(position.x) || float.IsInfinity(position.x))
            {
                sanitized.x = defaultPosition.x;
            }
            if (float.IsNaN(position.y) || float.IsInfinity(position.y))
            {
                sanitized.y = defaultPosition.y;
            }
            if (float.IsNaN(position.z) || float.IsInfinity(position.z))
            {
                sanitized.z = defaultPosition.z;
            }
            
            // Clamp extremely large values
            const float MAX_COORDINATE = 50000f;
            sanitized.x = Mathf.Clamp(sanitized.x, -MAX_COORDINATE, MAX_COORDINATE);
            sanitized.y = Mathf.Clamp(sanitized.y, -MAX_COORDINATE, MAX_COORDINATE);
            sanitized.z = Mathf.Clamp(sanitized.z, -MAX_COORDINATE, MAX_COORDINATE);
            
            if (sanitized != position)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Position sanitized in {context}: {position} -> {sanitized}");
            }
            
            return sanitized;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error sanitizing position in {context}: {ex.Message}");
            return defaultPosition;
        }
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Performs component-specific validation based on the component type
    /// </summary>
    /// <param name="component">The component to validate</param>
    /// <param name="componentName">Name of the component for logging</param>
    /// <returns>True if the component passes specific validation</returns>
    private static bool ValidateSpecificComponent(Component component, string componentName)
    {
        try
        {
            // PlayerMovement specific validation
            if (component is PlayerMovement playerMovement)
            {
                // Add any PlayerMovement-specific validation here
                return true;
            }
            
            // AudioSource specific validation
            if (component is AudioSource audioSource)
            {
                if (audioSource.clip == null)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"AudioSource {componentName} has no clip assigned (this may be normal)");
                }
                return true;
            }
            
            // NetworkObject specific validation
            if (component.GetType().Name.Contains("NetworkObject"))
            {
                // Add network object specific validation if needed
                return true;
            }
            
            // Default validation passed
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error in specific validation for {componentName}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Logs component-related errors with appropriate severity
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="isRequired">Whether the component is required</param>
    private static void LogComponentError(string message, bool isRequired)
    {
        try
        {
            if (isRequired)
            {
                ChronoParaPlugin.Logger?.LogError($"[COMPONENT ERROR] {message}");
            }
            else
            {
                ChronoParaPlugin.Logger?.LogWarning($"[COMPONENT WARNING] {message}");
            }
        }
        catch (Exception ex)
        {
            // Fallback logging if formatted logging fails
            ChronoParaPlugin.Logger?.LogError($"Component error: {message} (Logging error: {ex.Message})");
        }
    }
    
    /// <summary>
    /// Provides recovery guidance for missing components
    /// </summary>
    /// <typeparam name="T">The component type that's missing</typeparam>
    /// <param name="componentName">Name of the component</param>
    /// <param name="gameObject">The GameObject that's missing the component</param>
    private static void ProvideComponentRecoveryGuidance<T>(string componentName, GameObject gameObject) where T : Component
    {
        try
        {
            string guidance = typeof(T).Name switch
            {
                "PlayerMovement" => "PlayerMovement component is required for Temporal Recall. This may indicate a mod conflict or game update.",
                "AudioSource" => "AudioSource component missing - recall sounds may not play. This is not critical for functionality.",
                "NetworkObject" => "NetworkObject component missing - network synchronization may fail. Check for mod conflicts.",
                _ => $"{componentName} component is missing. This may affect mod functionality."
            };
            
            ChronoParaPlugin.Logger?.LogInfo($"Recovery guidance: {guidance}");
            
            // Provide object-specific guidance
            if (gameObject != null)
            {
                ChronoParaPlugin.Logger?.LogInfo($"Affected object: {gameObject.name} (Active: {gameObject.activeInHierarchy})");
                
                // List other components for debugging
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    var components = gameObject.GetComponents<Component>();
                    string componentList = string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name));
                    ChronoParaPlugin.Logger?.LogDebug($"Available components on {gameObject.name}: {componentList}");
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error providing component recovery guidance: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Diagnostic Methods
    
    /// <summary>
    /// Gets a comprehensive component status report for a GameObject
    /// </summary>
    /// <param name="gameObject">The GameObject to analyze</param>
    /// <returns>Formatted component status report</returns>
    public static string GetComponentStatusReport(GameObject gameObject)
    {
        try
        {
            if (gameObject == null)
            {
                return "Component Status: GameObject is null";
            }
            
            var report = $"=== Component Status Report for {gameObject.name} ===\n";
            report += $"Active: {gameObject.activeInHierarchy}\n";
            report += $"Layer: {gameObject.layer}\n";
            report += $"Tag: {gameObject.tag}\n";
            
            var components = gameObject.GetComponents<Component>();
            report += $"\nComponents ({components.Length}):\n";
            
            foreach (var component in components)
            {
                if (component != null)
                {
                    // Check if component has enabled property (Behaviour components)
                    string status = "Active";
                    if (component is Behaviour behaviour)
                    {
                        status = behaviour.enabled ? "Enabled" : "Disabled";
                    }
                    report += $"  - {component.GetType().Name}: {status}\n";
                }
                else
                {
                    report += $"  - [NULL COMPONENT]\n";
                }
            }
            
            return report;
        }
        catch (Exception ex)
        {
            return $"Error generating component status report: {ex.Message}";
        }
    }
    
    #endregion
}