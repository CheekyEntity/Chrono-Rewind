using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using System.Diagnostics;

namespace ChronoPara.Modules;

/// <summary>
/// Performance optimization utilities for the Chronomancer's Paradox mod
/// Provides profiling, memory management, and performance monitoring capabilities
/// </summary>
public static class PerformanceOptimizer
{
    #region Performance Monitoring
    
    private static readonly Dictionary<string, PerformanceMetric> _metrics = new Dictionary<string, PerformanceMetric>();
    private static readonly object _metricsLock = new object();
    
    /// <summary>
    /// Performance metric tracking structure
    /// </summary>
    private struct PerformanceMetric
    {
        public long TotalExecutionTime; // In ticks
        public int CallCount;
        public long MinExecutionTime;
        public long MaxExecutionTime;
        public DateTime LastCall;
        
        public double AverageExecutionTimeMs => CallCount > 0 ? (TotalExecutionTime / (double)CallCount) / TimeSpan.TicksPerMillisecond : 0;
        public double MinExecutionTimeMs => MinExecutionTime / (double)TimeSpan.TicksPerMillisecond;
        public double MaxExecutionTimeMs => MaxExecutionTime / (double)TimeSpan.TicksPerMillisecond;
    }
    
    /// <summary>
    /// Profiles the execution time of an action and records metrics
    /// </summary>
    /// <param name="actionName">Name of the action being profiled</param>
    /// <param name="action">The action to profile</param>
    public static void ProfileAction(string actionName, Action action)
    {
        if (action == null) return;
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            action();
        }
        finally
        {
            stopwatch.Stop();
            RecordMetric(actionName, stopwatch.ElapsedTicks);
        }
    }
    
    /// <summary>
    /// Profiles the execution time of a function and records metrics
    /// </summary>
    /// <typeparam name="T">Return type of the function</typeparam>
    /// <param name="actionName">Name of the action being profiled</param>
    /// <param name="func">The function to profile</param>
    /// <returns>The result of the function</returns>
    public static T ProfileFunction<T>(string actionName, Func<T> func)
    {
        if (func == null) return default(T);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            return func();
        }
        finally
        {
            stopwatch.Stop();
            RecordMetric(actionName, stopwatch.ElapsedTicks);
        }
    }
    
    /// <summary>
    /// Records a performance metric
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <param name="elapsedTicks">Execution time in ticks</param>
    private static void RecordMetric(string actionName, long elapsedTicks)
    {
        lock (_metricsLock)
        {
            if (_metrics.TryGetValue(actionName, out var metric))
            {
                metric.TotalExecutionTime += elapsedTicks;
                metric.CallCount++;
                metric.MinExecutionTime = Math.Min(metric.MinExecutionTime, elapsedTicks);
                metric.MaxExecutionTime = Math.Max(metric.MaxExecutionTime, elapsedTicks);
                metric.LastCall = DateTime.Now;
                _metrics[actionName] = metric;
            }
            else
            {
                _metrics[actionName] = new PerformanceMetric
                {
                    TotalExecutionTime = elapsedTicks,
                    CallCount = 1,
                    MinExecutionTime = elapsedTicks,
                    MaxExecutionTime = elapsedTicks,
                    LastCall = DateTime.Now
                };
            }
        }
    }
    
    /// <summary>
    /// Gets a performance report for all tracked metrics
    /// </summary>
    /// <returns>Formatted performance report</returns>
    public static string GetPerformanceReport()
    {
        lock (_metricsLock)
        {
            if (_metrics.Count == 0)
            {
                return "No performance metrics recorded.";
            }
            
            var report = "=== Performance Metrics Report ===\n";
            
            foreach (var kvp in _metrics)
            {
                var metric = kvp.Value;
                report += $"{kvp.Key}:\n";
                report += $"  Calls: {metric.CallCount}\n";
                report += $"  Avg: {metric.AverageExecutionTimeMs:F3}ms\n";
                report += $"  Min: {metric.MinExecutionTimeMs:F3}ms\n";
                report += $"  Max: {metric.MaxExecutionTimeMs:F3}ms\n";
                report += $"  Last: {metric.LastCall:HH:mm:ss}\n\n";
            }
            
            return report;
        }
    }
    
    /// <summary>
    /// Clears all performance metrics
    /// </summary>
    public static void ClearMetrics()
    {
        lock (_metricsLock)
        {
            _metrics.Clear();
        }
    }
    
    #endregion
    
    #region Memory Management
    
    /// <summary>
    /// Optimizes a position history queue by removing redundant snapshots
    /// </summary>
    /// <param name="history">The history queue to optimize</param>
    /// <param name="tolerance">Position tolerance for considering snapshots redundant</param>
    /// <returns>Number of snapshots removed</returns>
    public static int OptimizeHistoryQueue(Queue<PositionSnapshot> history, float tolerance = 0.1f)
    {
        if (history == null || history.Count <= 2)
            return 0;
        
        return ProfileFunction("OptimizeHistoryQueue", () =>
        {
            try
            {
                var snapshots = history.ToArray();
                var optimizedSnapshots = new List<PositionSnapshot>();
                
                // Always keep the first snapshot
                if (snapshots.Length > 0)
                    optimizedSnapshots.Add(snapshots[0]);
                
                // Keep snapshots that represent significant movement or health changes
                for (int i = 1; i < snapshots.Length - 1; i++)
                {
                    var current = snapshots[i];
                    var previous = snapshots[i - 1];
                    var next = snapshots[i + 1];
                    
                    // Keep if position changed significantly
                    bool significantMovement = Vector3.Distance(current.position, previous.position) > tolerance;
                    
                    // Keep if health changed significantly
                    bool significantHealthChange = Mathf.Abs(current.health - previous.health) > 1f;
                    
                    // Keep if this snapshot represents a direction change
                    bool directionChange = IsDirectionChange(previous, current, next, tolerance);
                    
                    if (significantMovement || significantHealthChange || directionChange)
                    {
                        optimizedSnapshots.Add(current);
                    }
                }
                
                // Always keep the last snapshot
                if (snapshots.Length > 1)
                    optimizedSnapshots.Add(snapshots[snapshots.Length - 1]);
                
                // Rebuild the queue with optimized snapshots
                history.Clear();
                foreach (var snapshot in optimizedSnapshots)
                {
                    history.Enqueue(snapshot);
                }
                
                int removedCount = snapshots.Length - optimizedSnapshots.Count;
                
                if (removedCount > 0 && ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"History optimization removed {removedCount} redundant snapshots");
                }
                
                return removedCount;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error optimizing history queue: {ex.Message}");
                return 0;
            }
        });
    }
    
    /// <summary>
    /// Determines if a snapshot represents a direction change
    /// </summary>
    private static bool IsDirectionChange(PositionSnapshot prev, PositionSnapshot current, PositionSnapshot next, float tolerance)
    {
        try
        {
            var dir1 = (current.position - prev.position).normalized;
            var dir2 = (next.position - current.position).normalized;
            
            // If either direction is too small, don't consider it a direction change
            if (dir1.magnitude < tolerance || dir2.magnitude < tolerance)
                return false;
            
            // Calculate the angle between directions
            float dot = Vector3.Dot(dir1, dir2);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            
            // Consider it a direction change if the angle is greater than 30 degrees
            return angle > 30f;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Forces garbage collection and logs memory usage
    /// </summary>
    public static void ForceGarbageCollection()
    {
        ProfileAction("ForceGarbageCollection", () =>
        {
            try
            {
                long memoryBefore = GC.GetTotalMemory(false);
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                long memoryAfter = GC.GetTotalMemory(false);
                long freedMemory = memoryBefore - memoryAfter;
                
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Garbage collection freed {freedMemory / 1024}KB of memory");
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error during garbage collection: {ex.Message}");
            }
        });
    }
    
    /// <summary>
    /// Gets current memory usage information
    /// </summary>
    /// <returns>Memory usage report</returns>
    public static string GetMemoryReport()
    {
        try
        {
            long totalMemory = GC.GetTotalMemory(false);
            int gen0Collections = GC.CollectionCount(0);
            int gen1Collections = GC.CollectionCount(1);
            int gen2Collections = GC.CollectionCount(2);
            
            return $"Memory Usage Report:\n" +
                   $"  Total Memory: {totalMemory / 1024}KB\n" +
                   $"  Gen 0 Collections: {gen0Collections}\n" +
                   $"  Gen 1 Collections: {gen1Collections}\n" +
                   $"  Gen 2 Collections: {gen2Collections}";
        }
        catch (Exception ex)
        {
            return $"Error generating memory report: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Configuration Optimization
    
    private static float _cachedRecallCooldown = -1f;
    private static float _cachedRewindDuration = -1f;
    private static float _cachedRecallKillWindow = -1f;
    private static float _lastConfigRefresh = 0f;
    private static readonly float CONFIG_REFRESH_INTERVAL = 5f; // Refresh every 5 seconds
    
    /// <summary>
    /// Gets cached recall cooldown with periodic refresh
    /// </summary>
    public static float GetCachedRecallCooldown()
    {
        RefreshConfigurationCache();
        return _cachedRecallCooldown > 0 ? _cachedRecallCooldown : ConfigManager.RecallCooldown;
    }
    
    /// <summary>
    /// Gets cached rewind duration with periodic refresh
    /// </summary>
    public static float GetCachedRewindDuration()
    {
        RefreshConfigurationCache();
        return _cachedRewindDuration > 0 ? _cachedRewindDuration : ConfigManager.RewindDuration;
    }
    
    /// <summary>
    /// Gets cached recall kill window with periodic refresh
    /// </summary>
    public static float GetCachedRecallKillWindow()
    {
        RefreshConfigurationCache();
        return _cachedRecallKillWindow > 0 ? _cachedRecallKillWindow : ConfigManager.RecallKillWindow;
    }
    
    /// <summary>
    /// Refreshes the configuration cache if needed
    /// </summary>
    private static void RefreshConfigurationCache()
    {
        float currentTime = Time.time;
        
        if (currentTime - _lastConfigRefresh > CONFIG_REFRESH_INTERVAL)
        {
            ProfileAction("RefreshConfigurationCache", () =>
            {
                try
                {
                    _cachedRecallCooldown = ConfigManager.RecallCooldown;
                    _cachedRewindDuration = ConfigManager.RewindDuration;
                    _cachedRecallKillWindow = ConfigManager.RecallKillWindow;
                    _lastConfigRefresh = currentTime;
                }
                catch (Exception ex)
                {
                    ChronoParaPlugin.Logger?.LogError($"Error refreshing configuration cache: {ex.Message}");
                }
            });
        }
    }
    
    /// <summary>
    /// Forces a configuration cache refresh
    /// </summary>
    public static void ForceConfigurationRefresh()
    {
        _lastConfigRefresh = 0f;
        RefreshConfigurationCache();
    }
    
    #endregion
    
    #region Adaptive Performance
    
    private static int _frameSkipMultiplier = 1;
    private static float _lastPerformanceCheck = 0f;
    private static readonly float PERFORMANCE_CHECK_INTERVAL = 10f; // Check every 10 seconds
    
    // Enhanced performance tracking
    private static float _averageFrameTime = 0f;
    private static int _frameTimesSampled = 0;
    private static float _lastFrameTimeUpdate = 0f;
    private static readonly float FRAME_TIME_UPDATE_INTERVAL = 1f; // Update every second
    
    /// <summary>
    /// Gets the current adaptive frame skip interval based on performance
    /// </summary>
    public static int GetAdaptiveFrameSkip()
    {
        float currentTime = Time.time;
        
        if (currentTime - _lastPerformanceCheck > PERFORMANCE_CHECK_INTERVAL)
        {
            UpdateAdaptivePerformance();
            _lastPerformanceCheck = currentTime;
        }
        
        return Math.Max(1, 5 * _frameSkipMultiplier); // Base 5 frames, multiplied by performance factor
    }
    
    /// <summary>
    /// Updates adaptive performance settings based on current performance metrics
    /// </summary>
    private static void UpdateAdaptivePerformance()
    {
        try
        {
            // Update frame time tracking
            UpdateFrameTimeTracking();
            
            // Check if position tracking is taking too long
            lock (_metricsLock)
            {
                if (_metrics.TryGetValue("RecordCurrentPosition", out var metric))
                {
                    double avgTime = metric.AverageExecutionTimeMs;
                    
                    if (avgTime > 2.0) // If taking more than 2ms on average
                    {
                        _frameSkipMultiplier = Math.Min(_frameSkipMultiplier + 1, 4); // Max 4x skip
                        
                        if (ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"Increased frame skip multiplier to {_frameSkipMultiplier} due to high position tracking time ({avgTime:F2}ms)");
                        }
                    }
                    else if (avgTime < 0.5 && _frameSkipMultiplier > 1) // If performing well
                    {
                        _frameSkipMultiplier = Math.Max(_frameSkipMultiplier - 1, 1); // Min 1x skip
                        
                        if (ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"Decreased frame skip multiplier to {_frameSkipMultiplier} due to good performance ({avgTime:F2}ms)");
                        }
                    }
                }
                
                // Enhanced memory pressure detection
                long currentMemory = GC.GetTotalMemory(false);
                long memoryMB = currentMemory / (1024 * 1024);
                
                if (memoryMB > 100) // More than 100MB
                {
                    // Increase frame skip to reduce memory pressure
                    _frameSkipMultiplier = Math.Min(_frameSkipMultiplier + 1, 6); // Allow higher skip for memory pressure
                    
                    if (ChronoParaPlugin.DebugMode?.Value == true)
                    {
                        ChronoParaPlugin.Logger?.LogDebug($"Increased frame skip due to memory pressure: {memoryMB}MB");
                    }
                }
                
                // Additional optimization: Check memory usage and adjust history optimization frequency
                if (_metrics.TryGetValue("OptimizeHistoryQueue", out var optimizeMetric))
                {
                    double optimizeTime = optimizeMetric.AverageExecutionTimeMs;
                    
                    // If history optimization is taking too long, reduce frequency
                    if (optimizeTime > 10.0) // More than 10ms
                    {
                        if (ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"History optimization taking {optimizeTime:F2}ms - consider reducing optimization frequency");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error updating adaptive performance: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Updates frame time tracking for performance analysis
    /// </summary>
    private static void UpdateFrameTimeTracking()
    {
        try
        {
            float currentTime = Time.time;
            
            if (currentTime - _lastFrameTimeUpdate >= FRAME_TIME_UPDATE_INTERVAL)
            {
                float deltaTime = Time.deltaTime;
                
                // Update running average
                _averageFrameTime = (_averageFrameTime * _frameTimesSampled + deltaTime) / (_frameTimesSampled + 1);
                _frameTimesSampled = Math.Min(_frameTimesSampled + 1, 100); // Cap at 100 samples
                
                _lastFrameTimeUpdate = currentTime;
                
                // Log performance warnings if frame time is consistently high
                if (_averageFrameTime > 0.033f && _frameTimesSampled > 10) // More than 30 FPS
                {
                    if (ChronoParaPlugin.DebugMode?.Value == true)
                    {
                        ChronoParaPlugin.Logger?.LogDebug($"Average frame time high: {_averageFrameTime * 1000:F1}ms ({1f / _averageFrameTime:F1} FPS)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ChronoParaPlugin.Logger?.LogError($"Error updating frame time tracking: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the current average frame time in milliseconds
    /// </summary>
    public static float GetAverageFrameTimeMs()
    {
        return _averageFrameTime * 1000f;
    }
    
    /// <summary>
    /// Gets the current estimated FPS
    /// </summary>
    public static float GetEstimatedFPS()
    {
        return _averageFrameTime > 0f ? 1f / _averageFrameTime : 0f;
    }
    
    #endregion
    
    #region Diagnostic Methods
    
    /// <summary>
    /// Advanced memory optimization for position history queues
    /// </summary>
    /// <param name="history">The history queue to optimize</param>
    /// <param name="aggressiveMode">Whether to use aggressive optimization</param>
    /// <returns>Number of snapshots removed</returns>
    public static int OptimizeHistoryQueueAdvanced(Queue<PositionSnapshot> history, bool aggressiveMode = false)
    {
        if (history == null || history.Count <= 2)
            return 0;
        
        return ProfileFunction("OptimizeHistoryQueueAdvanced", () =>
        {
            try
            {
                var snapshots = history.ToArray();
                var optimizedSnapshots = new List<PositionSnapshot>();
                
                float tolerance = aggressiveMode ? 0.2f : 0.1f; // More aggressive tolerance
                float healthTolerance = aggressiveMode ? 2f : 1f; // More aggressive health tolerance
                
                // Always keep the first snapshot
                if (snapshots.Length > 0)
                    optimizedSnapshots.Add(snapshots[0]);
                
                // Enhanced optimization logic
                for (int i = 1; i < snapshots.Length - 1; i++)
                {
                    var current = snapshots[i];
                    var previous = snapshots[i - 1];
                    var next = snapshots[i + 1];
                    
                    bool shouldKeep = false;
                    
                    // Keep if position changed significantly
                    if (Vector3.Distance(current.position, previous.position) > tolerance)
                    {
                        shouldKeep = true;
                    }
                    
                    // Keep if health changed significantly
                    if (Mathf.Abs(current.health - previous.health) > healthTolerance)
                    {
                        shouldKeep = true;
                    }
                    
                    // Keep if this snapshot represents a direction change
                    if (IsDirectionChange(previous, current, next, tolerance))
                    {
                        shouldKeep = true;
                    }
                    
                    // Keep snapshots at regular intervals to maintain temporal accuracy
                    if (!aggressiveMode && i % 5 == 0) // Every 5th snapshot
                    {
                        shouldKeep = true;
                    }
                    
                    // In aggressive mode, also check for velocity changes
                    if (aggressiveMode && i > 1 && i < snapshots.Length - 2)
                    {
                        var prevVelocity = (current.position - previous.position) / (current.timestamp - previous.timestamp);
                        var nextVelocity = (next.position - current.position) / (next.timestamp - current.timestamp);
                        
                        if (Vector3.Distance(prevVelocity, nextVelocity) > tolerance * 2f)
                        {
                            shouldKeep = true;
                        }
                    }
                    
                    if (shouldKeep)
                    {
                        optimizedSnapshots.Add(current);
                    }
                }
                
                // Always keep the last snapshot
                if (snapshots.Length > 1)
                    optimizedSnapshots.Add(snapshots[snapshots.Length - 1]);
                
                // Rebuild the queue with optimized snapshots
                history.Clear();
                foreach (var snapshot in optimizedSnapshots)
                {
                    history.Enqueue(snapshot);
                }
                
                int removedCount = snapshots.Length - optimizedSnapshots.Count;
                
                if (removedCount > 0 && ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Advanced history optimization removed {removedCount} snapshots (aggressive: {aggressiveMode})");
                }
                
                return removedCount;
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error in advanced history optimization: {ex.Message}");
                return 0;
            }
        });
    }
    
    /// <summary>
    /// Performs comprehensive memory cleanup
    /// </summary>
    public static void PerformComprehensiveMemoryCleanup()
    {
        ProfileAction("PerformComprehensiveMemoryCleanup", () =>
        {
            try
            {
                long memoryBefore = GC.GetTotalMemory(false);
                
                // Clear performance metrics if they're taking up too much space
                lock (_metricsLock)
                {
                    if (_metrics.Count > 50) // If we have too many metrics
                    {
                        var oldMetrics = _metrics.Where(kvp => 
                            (DateTime.Now - kvp.Value.LastCall).TotalMinutes > 5).ToList();
                        
                        foreach (var oldMetric in oldMetrics)
                        {
                            _metrics.Remove(oldMetric.Key);
                        }
                        
                        if (ChronoParaPlugin.DebugMode?.Value == true)
                        {
                            ChronoParaPlugin.Logger?.LogDebug($"Cleaned up {oldMetrics.Count} old performance metrics");
                        }
                    }
                }
                
                // Force garbage collection with full cleanup
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
                
                // Compact the large object heap if available (.NET 4.5.1+)
                try
                {
                    // Try to compact LOH if available
                    var gcSettingsType = typeof(GC).Assembly.GetType("System.Runtime.GCSettings");
                    if (gcSettingsType != null)
                    {
                        var property = gcSettingsType.GetProperty("LargeObjectHeapCompactionMode");
                        if (property != null)
                        {
                            property.SetValue(null, 1); // CompactOnce
                            GC.Collect();
                        }
                    }
                }
                catch (Exception)
                {
                    // Not available in this .NET version, ignore
                }
                
                long memoryAfter = GC.GetTotalMemory(false);
                long freedMemory = memoryBefore - memoryAfter;
                
                if (ChronoParaPlugin.DebugMode?.Value == true)
                {
                    ChronoParaPlugin.Logger?.LogDebug($"Comprehensive memory cleanup freed {freedMemory / 1024}KB");
                }
            }
            catch (Exception ex)
            {
                ChronoParaPlugin.Logger?.LogError($"Error during comprehensive memory cleanup: {ex.Message}");
            }
        });
    }
    
    /// <summary>
    /// Generates a comprehensive performance diagnostic report
    /// </summary>
    /// <returns>Detailed performance diagnostic report</returns>
    public static string GeneratePerformanceDiagnostic()
    {
        try
        {
            var report = "=== Performance Diagnostic Report ===\n\n";
            
            // Performance metrics
            report += GetPerformanceReport() + "\n";
            
            // Memory information
            report += GetMemoryReport() + "\n\n";
            
            // Frame time information
            report += "Frame Time Analysis:\n";
            report += $"  Average Frame Time: {GetAverageFrameTimeMs():F2}ms\n";
            report += $"  Estimated FPS: {GetEstimatedFPS():F1}\n";
            report += $"  Frame Samples: {_frameTimesSampled}\n\n";
            
            // Configuration cache status
            report += "Configuration Cache Status:\n";
            report += $"  Last Refresh: {Time.time - _lastConfigRefresh:F2}s ago\n";
            report += $"  Cached Cooldown: {_cachedRecallCooldown}s\n";
            report += $"  Cached Duration: {_cachedRewindDuration}s\n";
            report += $"  Cached Kill Window: {_cachedRecallKillWindow}s\n\n";
            
            // Adaptive performance status
            report += "Adaptive Performance Status:\n";
            report += $"  Frame Skip Multiplier: {_frameSkipMultiplier}x\n";
            report += $"  Current Frame Skip: {GetAdaptiveFrameSkip()}\n";
            report += $"  Last Performance Check: {Time.time - _lastPerformanceCheck:F2}s ago\n\n";
            
            // Performance recommendations
            report += "Performance Recommendations:\n";
            if (GetAverageFrameTimeMs() > 33f) // Less than 30 FPS
            {
                report += "  ⚠️ Frame rate is low - consider reducing rewind duration or increasing recording interval\n";
            }
            
            long memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            if (memoryMB > 100)
            {
                report += $"  ⚠️ High memory usage ({memoryMB}MB) - consider running memory cleanup\n";
            }
            
            if (_frameSkipMultiplier > 2)
            {
                report += "  ⚠️ High frame skip multiplier indicates performance issues\n";
            }
            
            if (report.EndsWith("Performance Recommendations:\n"))
            {
                report += "  ✅ Performance is good - no recommendations\n";
            }
            
            return report;
        }
        catch (Exception ex)
        {
            return $"Error generating performance diagnostic: {ex.Message}";
        }
    }
    
    #endregion
}