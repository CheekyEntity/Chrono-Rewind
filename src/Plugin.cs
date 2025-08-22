using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ChronoPara.Modules;
using BlackMagicAPI.Managers;
using static ChronoPara.Modules.DiagnosticLogger;

namespace ChronoPara;

[BepInProcess("MageArena")]
[BepInDependency("com.magearena.modsync", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.d1gq.black.magic.api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.d1gq.fish.utilities", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.d1gq.mage.configuration.api", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(MyGUID, PluginName, VersionString)]
public class ChronoParaPlugin : BaseUnityPlugin
{
    internal static ChronoParaPlugin Instance { get; private set; }

    private const string MyGUID = "com.cheekyentity.ChronosRewind.dev";
    internal const string PluginName = "Chronos Rewind";
    private const string VersionString = "1.0.0";

    private static Harmony Harmony;
    internal static new ManualLogSource Logger;

    public static string modsync = "all";

    // Configuration entries
    public static ConfigEntry<float> RecallCooldown;
    public static ConfigEntry<float> RewindDuration;
    public static ConfigEntry<float> RecallKillWindow;
    public static ConfigEntry<bool> CanSpawnInChests;
    
    // Simple debug flag (disabled for production)
    public static ConfigEntry<bool> DebugMode;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        
        // Initialize configuration system
        InitializeConfiguration();
        
        // Initialize Harmony patches
        Harmony = new(MyGUID);
        
        try
        {
            // Apply Harmony patches
            Harmony.PatchAll();
            Logger.LogInfo("Harmony patches applied successfully");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Failed to apply Harmony patches: {ex.Message}");
            return;
        }

        // Initialize spell system
        InitializeSpellSystem();
        
        // Initialize performance management system
        PerformanceManager.Initialize();

        Logger.LogInfo($"Plugin {MyGUID} v{VersionString} is loaded!");
    }
    
    private void Update()
    {
        try
        {
            // Update performance monitoring
            PerformanceManager.Update();
        }
        catch (System.Exception ex)
        {
            // Log error but don't let it break the game
            if (DebugMode?.Value == true)
            {
                Logger.LogError($"Error in plugin update: {ex.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            DiagnosticLogger.LogInfo("Starting plugin cleanup...", LogCategory.System);
            
            // Shutdown performance management system first
            PerformanceManager.Shutdown();
            
            // Generate final diagnostic and performance reports before cleanup
            if (DebugMode?.Value == true)
            {
                string diagnosticReport = DiagnosticLogger.GenerateDiagnosticReport();
                Logger.LogDebug("Final diagnostic report:\n" + diagnosticReport);
                
                string performanceReport = PerformanceOptimizer.GeneratePerformanceDiagnostic();
                Logger.LogDebug("Final performance report:\n" + performanceReport);
                
                // Force final garbage collection and memory cleanup
                PerformanceOptimizer.ForceGarbageCollection();
            }
            
            // Clean up DeathTracker resources with error handling
            try
            {
                DeathTracker.Cleanup();
                DiagnosticLogger.LogInfo("DeathTracker cleanup completed", LogCategory.System);
            }
            catch (System.Exception ex)
            {
                DiagnosticLogger.LogError($"Error during DeathTracker cleanup: {ex.Message}", LogCategory.System, ex);
            }
            
            // Clean up AssetManager resources with error handling
            try
            {
                AssetManager.Cleanup();
                DiagnosticLogger.LogInfo("AssetManager cleanup completed", LogCategory.System);
            }
            catch (System.Exception ex)
            {
                DiagnosticLogger.LogError($"Error during AssetManager cleanup: {ex.Message}", LogCategory.System, ex);
            }
            
            // Clean up Harmony patches with error handling
            try
            {
                if (Harmony != null)
                {
                    Harmony.UnpatchSelf();
                    DiagnosticLogger.LogInfo("Harmony patches removed", LogCategory.System);
                }
                else
                {
                    DiagnosticLogger.LogWarning("Harmony instance was null during cleanup", LogCategory.System);
                }
            }
            catch (System.Exception ex)
            {
                DiagnosticLogger.LogError($"Error during Harmony cleanup: {ex.Message}", LogCategory.System, ex);
            }
            
            // Log cleanup summary
            int totalErrors = DiagnosticLogger.GetTotalErrorCount();
            if (totalErrors > 0)
            {
                Logger.LogWarning($"Plugin cleanup completed with {totalErrors} total errors logged during session");
                Logger.LogInfo("Check the diagnostic logs above for details on any issues encountered");
            }
            else
            {
                DiagnosticLogger.LogInfo("Plugin cleanup completed successfully with no errors", LogCategory.System);
            }
        }
        catch (System.Exception ex)
        {
            // Final fallback error handling
            Logger.LogError($"Critical error during plugin cleanup: {ex.Message}");
            
            // Try to log stack trace if possible
            try
            {
                if (DebugMode?.Value == true)
                {
                    Logger.LogDebug($"Cleanup error stack trace: {ex.StackTrace}");
                }
            }
            catch
            {
                // Ignore errors in error handling
            }
        }
    }

    private void InitializeConfiguration()
    {
        try
        {
            // Chronos Rewind Cooldown configuration
            var cooldownDesc = new ConfigDescription(
                "Cooldown time in seconds for Chronos Rewind spell",
                new AcceptableValueRange<float>(10f, 120f));
            RecallCooldown = Config.Bind("Chronos Rewind", "Cooldown", 45f, cooldownDesc);

            // Rewind Duration configuration
            var rewindDurationDesc = new ConfigDescription(
                "How many seconds back to rewind when casting Chronos Rewind",
                new AcceptableValueRange<float>(1f, 10f));
            RewindDuration = Config.Bind("Chronos Rewind", "Rewind Duration", 3f, rewindDurationDesc);

            // Recall Kill Window configuration
            var killWindowDesc = new ConfigDescription(
                "Time window after recall for kills to count as recall-related (in seconds)",
                new AcceptableValueRange<float>(1f, 10f));
            RecallKillWindow = Config.Bind("Chronos Rewind", "Kill Window", 3f, killWindowDesc);

            // Chest Spawning configuration
            var chestSpawnDesc = new ConfigDescription(
                "Allow Chronos Rewind spell to spawn in team chests for natural discovery");
            CanSpawnInChests = Config.Bind("Chronos Rewind", "Can Spawn In Chests", false, chestSpawnDesc);

            // Debug Mode configuration (disabled by default for production)
            DebugMode = Config.Bind("Debug", "Enable Debug Mode", false, "Enable detailed debug logging");

            Logger.LogInfo("Configuration system initialized successfully");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Failed to initialize configuration: {ex.Message}");
            // Set default values if configuration fails
            Logger.LogWarning("Using default configuration values");
        }
    }


    
    private void InitializeSpellSystem()
    {
        try
        {
            DiagnosticLogger.LogInfo("Initializing spell system...", LogCategory.System);
            
            // Perform comprehensive configuration validation with user feedback
            var configValidation = ConfigurationValidator.ValidateAllConfiguration();
            if (!configValidation.IsValid)
            {
                DiagnosticLogger.LogWarning("Configuration validation found issues, but continuing with initialization", LogCategory.Configuration);
                
                // Log user-friendly configuration report
                if (DebugMode?.Value == true)
                {
                    string configReport = ConfigurationValidator.GenerateUserFriendlyReport();
                    Logger.LogInfo("Configuration Report:\n" + configReport);
                }
            }
            
            // Run integration tests if debug mode is enabled
            if (DebugMode?.Value == true)
            {
                DiagnosticLogger.LogInfo("Running integration tests...", LogCategory.System);
                var testResults = IntegrationTester.RunIntegrationTests();
                
                if (!testResults.AllPassed)
                {
                    DiagnosticLogger.LogWarning($"Integration tests found issues: {testResults.FailedCount} failed tests", LogCategory.System);
                }
                
                // Run performance tests as part of final integration testing
                DiagnosticLogger.LogInfo("Running performance tests...", LogCategory.System);
                PerformanceManager.RunPerformanceTests();
            }
            
            // Load and register asset bundle with error handling
            bool assetBundleLoaded = LoadAssetBundle();
            
            // Register the Chronos Rewind spell with error handling
            bool spellRegistered = RegisterTemporalRecallSpell();
            
            // Register custom death icon with error handling
            bool deathIconRegistered = RegisterCustomDeathIcon();
            
            // Evaluate overall initialization success
            if (spellRegistered)
            {
                DiagnosticLogger.LogInfo("Spell system initialized successfully", LogCategory.System);
                
                // Log initialization summary
                var summary = $"Initialization Summary - AssetBundle: {(assetBundleLoaded ? "Loaded" : "Fallback")}, " +
                             $"Spell: {(spellRegistered ? "Registered" : "Failed")}, " +
                             $"DeathIcon: {(deathIconRegistered ? "Registered" : "Failed")}";
                DiagnosticLogger.LogInfo(summary, LogCategory.System);
                
                // Provide comprehensive user feedback
                UserFeedbackSystem.ProvideComprehensiveFeedback();
            }
            else
            {
                DiagnosticLogger.LogError("Critical failure in spell system initialization - spell registration failed", LogCategory.System);
                
                // Provide recovery guidance through user feedback system
                UserFeedbackSystem.ProvideTroubleshootingFeedback("spell_not_found");
            }
        }
        catch (System.Exception ex)
        {
            DiagnosticLogger.LogError($"Failed to initialize spell system: {ex.Message}", LogCategory.System, ex);
            
            // Provide detailed recovery guidance
            Logger.LogError("Critical error during spell system initialization. Please check the following:");
            Logger.LogError("1. Ensure all mod dependencies are installed and up to date");
            Logger.LogError("2. Check for conflicts with other mods");
            Logger.LogError("3. Verify game files are not corrupted");
            Logger.LogError("4. Try reinstalling the Chronos Rewind mod");
        }
    }
    
    private bool LoadAssetBundle()
    {
        try
        {
            DiagnosticLogger.LogInfo("Loading asset bundle...", LogCategory.System);
            
            // Initialize the AssetManager to load custom assets
            AssetManager.Initialize();
            
            if (AssetManager.IsBundleLoaded)
            {
                DiagnosticLogger.LogInfo("Custom assets loaded successfully", LogCategory.System);
                
                // Log asset information in debug mode
                if (DebugMode?.Value == true)
                {
                    string assetInfo = AssetManager.GetAssetInfo();
                    DiagnosticLogger.LogDebug($"Asset bundle details: {assetInfo}", LogCategory.System);
                }
                
                return true;
            }
            else
            {
                DiagnosticLogger.LogWarning("Custom AssetBundle not found, using fallback assets", LogCategory.System);
                Logger.LogInfo("Note: Some visual and audio effects may not be available without the custom AssetBundle");
                Logger.LogInfo("To get the full experience, ensure 'chronomancer.bundle' is in the mod directory");
                
                return false; // Fallback mode
            }
        }
        catch (System.Exception ex)
        {
            DiagnosticLogger.LogError($"Failed to load asset bundle: {ex.Message}", LogCategory.System, ex);
            
            // Provide detailed recovery guidance
            Logger.LogWarning("Asset bundle loading failed - continuing with fallback assets");
            Logger.LogInfo("Recovery guidance:");
            Logger.LogInfo("1. Check that 'chronomancer.bundle' exists in the mod directory");
            Logger.LogInfo("2. Verify the file is not corrupted (try re-downloading the mod)");
            Logger.LogInfo("3. Ensure you have sufficient disk space and permissions");
            
            return false; // Fallback mode
        }
    }
    
    private bool RegisterTemporalRecallSpell()
    {
        try
        {
            DiagnosticLogger.LogInfo("Registering Chronos Rewind spell...", LogCategory.System);
            
            // Validate that required types exist
            if (typeof(ChronosRewind_Data) == null)
            {
                DiagnosticLogger.LogError("ChronosRewind_Data type not found", LogCategory.System);
                return false;
            }
            
            if (typeof(ChronosRewind_Logic) == null)
            {
                DiagnosticLogger.LogError("ChronosRewind_Logic type not found", LogCategory.System);
                return false;
            }
            
            // Register the Chronos Rewind spell using the BlackMagicAPI pattern
            BlackMagicManager.RegisterSpell(this, typeof(ChronosRewind_Data), typeof(ChronosRewind_Logic));
            
            DiagnosticLogger.LogInfo("Successfully registered Chronos Rewind spell", LogCategory.System);
            
            // Log spell configuration in debug mode
            if (DebugMode?.Value == true)
            {
                DiagnosticLogger.LogDebug($"Spell registered with cooldown: {ConfigManager.RecallCooldown}s", LogCategory.System);
            }
            
            return true;
        }
        catch (System.Exception ex)
        {
            DiagnosticLogger.LogError($"Failed to register Chronos Rewind spell: {ex.Message}", LogCategory.System, ex);
            
            // Provide detailed recovery guidance
            Logger.LogError("Spell registration failed - the mod will not function correctly");
            Logger.LogInfo("Recovery guidance:");
            Logger.LogInfo("1. Ensure BlackMagicAPI is installed and up to date");
            Logger.LogInfo("2. Check for conflicts with other spell mods");
            Logger.LogInfo("3. Verify the mod files are not corrupted");
            Logger.LogInfo("4. Try restarting the game");
            
            return false;
        }
    }
    

    
    private bool RegisterCustomDeathIcon()
    {
        try
        {
            DiagnosticLogger.LogInfo("Initializing death tracking system...", LogCategory.System);
            
            // Initialize the death tracking system which handles death icon registration
            DeathTracker.Initialize();
            
            if (DeathTracker.IsInitialized)
            {
                DiagnosticLogger.LogInfo("Death tracking system initialized successfully", LogCategory.System);
                return true;
            }
            else
            {
                DiagnosticLogger.LogWarning("Death tracking system initialization failed - recall-related death attribution may not work", LogCategory.System);
                
                // Provide guidance but don't fail completely
                Logger.LogInfo("Note: The core Chronos Rewind functionality will still work");
                Logger.LogInfo("Only custom death messages and statistics will be affected");
                
                return false; // Non-critical failure
            }
        }
        catch (System.Exception ex)
        {
            DiagnosticLogger.LogError($"Failed to initialize death tracking system: {ex.Message}", LogCategory.System, ex);
            
            // Provide recovery guidance
            Logger.LogWarning("Death tracking initialization failed - this is not critical for core functionality");
            Logger.LogInfo("Recovery guidance:");
            Logger.LogInfo("1. Check that the death icon asset is available");
            Logger.LogInfo("2. Verify BlackMagicAPI death icon registration is working");
            Logger.LogInfo("3. This may be caused by mod conflicts with other death-related mods");
            
            return false; // Non-critical failure
        }
    }
    

}