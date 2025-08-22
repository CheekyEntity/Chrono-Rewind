using UnityEngine;
using System;

namespace ChronoPara.Modules;

/// <summary>
/// Represents a snapshot of a player's position and health at a specific point in time
/// Used for temporal recall functionality with comprehensive validation and error handling
/// </summary>
[System.Serializable]
public struct PositionSnapshot
{
    /// <summary>
    /// The player's position at the time of the snapshot
    /// </summary>
    public Vector3 position;
    
    /// <summary>
    /// The player's health at the time of the snapshot
    /// </summary>
    public float health;
    
    /// <summary>
    /// The timestamp when this snapshot was taken (using Time.time)
    /// </summary>
    public float timestamp;
    
    #region Constructors
    
    /// <summary>
    /// Creates a new position snapshot with validation
    /// </summary>
    /// <param name="pos">Player position</param>
    /// <param name="hp">Player health</param>
    /// <param name="time">Timestamp</param>
    public PositionSnapshot(Vector3 pos, float hp, float time)
    {
        // Validate and sanitize position
        position = ValidatePosition(pos);
        
        // Validate and sanitize health
        health = ValidateHealth(hp);
        
        // Validate and sanitize timestamp
        timestamp = ValidateTimestamp(time);
    }
    
    /// <summary>
    /// Creates a position snapshot with the current time and validation
    /// </summary>
    /// <param name="pos">Player position</param>
    /// <param name="hp">Player health</param>
    public PositionSnapshot(Vector3 pos, float hp) : this(pos, hp, Time.time)
    {
    }
    
    #endregion
    
    #region Validation Methods
    
    /// <summary>
    /// Validates and sanitizes a position vector
    /// </summary>
    /// <param name="pos">The position to validate</param>
    /// <returns>A valid position vector</returns>
    private static Vector3 ValidatePosition(Vector3 pos)
    {
        try
        {
            // Check for NaN or infinity values
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid position detected: {pos}, using origin");
                return Vector3.zero;
            }
            
            // Check for extremely large values that might indicate corruption
            const float MAX_COORDINATE = 10000f;
            if (Mathf.Abs(pos.x) > MAX_COORDINATE || Mathf.Abs(pos.y) > MAX_COORDINATE || Mathf.Abs(pos.z) > MAX_COORDINATE)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Position coordinates too large: {pos}, clamping to reasonable bounds");
                return new Vector3(
                    Mathf.Clamp(pos.x, -MAX_COORDINATE, MAX_COORDINATE),
                    Mathf.Clamp(pos.y, -MAX_COORDINATE, MAX_COORDINATE),
                    Mathf.Clamp(pos.z, -MAX_COORDINATE, MAX_COORDINATE)
                );
            }
            
            return pos;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating position: {ex.Message}");
            return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Validates and sanitizes a health value
    /// </summary>
    /// <param name="hp">The health value to validate</param>
    /// <returns>A valid health value</returns>
    private static float ValidateHealth(float hp)
    {
        try
        {
            // Check for NaN or infinity
            if (float.IsNaN(hp) || float.IsInfinity(hp))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid health value detected: {hp}, using default");
                return 100f; // Default health
            }
            
            // Clamp to reasonable bounds
            const float MIN_HEALTH = 0f;
            const float MAX_HEALTH = 10000f; // Allow for modded high health values
            
            float clampedHealth = Mathf.Clamp(hp, MIN_HEALTH, MAX_HEALTH);
            
            if (clampedHealth != hp)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Health value {hp} was outside reasonable bounds, clamped to {clampedHealth}");
            }
            
            return clampedHealth;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating health: {ex.Message}");
            return 100f; // Default health
        }
    }
    
    /// <summary>
    /// Validates and sanitizes a timestamp value
    /// </summary>
    /// <param name="time">The timestamp to validate</param>
    /// <returns>A valid timestamp</returns>
    private static float ValidateTimestamp(float time)
    {
        try
        {
            // Check for NaN or infinity
            if (float.IsNaN(time) || float.IsInfinity(time))
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid timestamp detected: {time}, using current time");
                return Time.time;
            }
            
            // Check for negative timestamps (shouldn't happen with Time.time)
            if (time < 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Negative timestamp detected: {time}, using current time");
                return Time.time;
            }
            
            // Check for timestamps that are too far in the future (might indicate corruption)
            float currentTime = Time.time;
            if (time > currentTime + 1f) // Allow 1 second tolerance for timing variations
            {
                ChronoParaPlugin.Logger?.LogWarning($"Future timestamp detected: {time} (current: {currentTime}), using current time");
                return currentTime;
            }
            
            return time;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating timestamp: {ex.Message}");
            return Time.time;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Checks if this snapshot is older than the specified duration with error handling
    /// </summary>
    /// <param name="maxAge">Maximum age in seconds</param>
    /// <returns>True if the snapshot is too old</returns>
    public bool IsExpired(float maxAge)
    {
        try
        {
            if (float.IsNaN(maxAge) || maxAge < 0f)
            {
                ChronoParaPlugin.Logger?.LogWarning($"Invalid maxAge value: {maxAge}");
                return false; // Assume not expired on invalid input
            }
            
            float age = Time.time - timestamp;
            return age > maxAge;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error checking if snapshot is expired: {ex.Message}");
            return false; // Assume not expired on error
        }
    }
    
    /// <summary>
    /// Gets the age of this snapshot in seconds with error handling
    /// </summary>
    public float Age
    {
        get
        {
            try
            {
                float age = Time.time - timestamp;
                
                // Sanity check - age shouldn't be negative
                if (age < 0f)
                {
                    ChronoParaPlugin.Logger?.LogWarning($"Negative snapshot age calculated: {age}");
                    return 0f;
                }
                
                return age;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error calculating snapshot age: {ex.Message}");
                return 0f;
            }
        }
    }
    
    /// <summary>
    /// Checks if this snapshot contains valid data
    /// </summary>
    /// <returns>True if all snapshot data is valid</returns>
    public bool IsValid()
    {
        try
        {
            // Check position validity
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
            {
                return false;
            }
            
            // Check health validity
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0f)
            {
                return false;
            }
            
            // Check timestamp validity
            if (float.IsNaN(timestamp) || float.IsInfinity(timestamp) || timestamp < 0f)
            {
                return false;
            }
            
            // Check if timestamp is reasonable (not too far in the past or future)
            float age = Time.time - timestamp;
            if (age < -1f || age > 3600f) // Allow 1 hour max age, 1 second future tolerance
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error validating snapshot: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Creates a sanitized copy of this snapshot with validated values
    /// </summary>
    /// <returns>A new snapshot with validated data</returns>
    public PositionSnapshot Sanitize()
    {
        return new PositionSnapshot(
            ValidatePosition(position),
            ValidateHealth(health),
            ValidateTimestamp(timestamp)
        );
    }
    
    /// <summary>
    /// Returns a string representation of this snapshot with error handling
    /// </summary>
    public override string ToString()
    {
        try
        {
            return $"PositionSnapshot(pos: {position}, health: {health:F1}, age: {Age:F2}s, valid: {IsValid()})";
        }
        catch (Exception ex)
        {
            return $"PositionSnapshot(error: {ex.Message})";
        }
    }
    
    /// <summary>
    /// Returns a detailed string representation for debugging
    /// </summary>
    /// <returns>Detailed snapshot information</returns>
    public string ToDetailedString()
    {
        try
        {
            return $"PositionSnapshot {{\n" +
                   $"  Position: {position}\n" +
                   $"  Health: {health:F2}\n" +
                   $"  Timestamp: {timestamp:F3}\n" +
                   $"  Age: {Age:F3}s\n" +
                   $"  Valid: {IsValid()}\n" +
                   $"}}";
        }
        catch (Exception ex)
        {
            return $"PositionSnapshot {{ Error: {ex.Message} }}";
        }
    }
    
    #endregion
}