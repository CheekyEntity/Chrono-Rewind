using UnityEngine;
using System.IO;
using BepInEx.Logging;

namespace ChronoPara.Modules
{
    /// <summary>
    /// Manages loading and caching of custom assets from the chronomancer AssetBundle
    /// </summary>
    public static class AssetManager
    {
        private static AssetBundle _chronoBundle;
        private static bool _bundleLoaded = false;
        private static ManualLogSource Logger => ChronoParaPlugin.Logger;

        // Cached assets
        private static GameObject _recallEffectPrefab;
        private static AudioClip _recallSound;
        private static Texture2D _recallKillIcon;
        private static Texture2D _spellIcon;
        private static Texture2D _spellMainTexture;
        private static Texture2D _spellEmissionTexture;

        /// <summary>
        /// Initialize the asset loading system and load the chronomancer bundle
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadAssetBundle();
                CacheAssets();
                Logger.LogInfo("AssetManager initialized successfully");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to initialize AssetManager: {ex.Message}");
                Logger.LogWarning("Mod will continue with fallback assets");
            }
        }

        /// <summary>
        /// Load the chronomancer AssetBundle from the Resources directory
        /// </summary>
        private static void LoadAssetBundle()
        {
            string bundlePath = Path.Combine(
                Path.GetDirectoryName(ChronoParaPlugin.Instance.Info.Location),
                "chronomancer.bundle"
            );

            if (!File.Exists(bundlePath))
            {
                Logger.LogInfo("AssetBundle not found - using fallback assets");
                return;
            }

            try
            {
                _chronoBundle = AssetBundle.LoadFromFile(bundlePath);
                
                if (_chronoBundle == null)
                {
                    Logger.LogError("Failed to load AssetBundle - bundle is null");
                    return;
                }

                _bundleLoaded = true;
                Logger.LogInfo("Successfully loaded chronomancer AssetBundle");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Exception loading AssetBundle: {ex.Message}");
                _chronoBundle = null;
            }
        }

        /// <summary>
        /// Cache commonly used assets for performance
        /// </summary>
        private static void CacheAssets()
        {
            if (!_bundleLoaded || _chronoBundle == null)
            {
                return;
            }

            try
            {
                // Cache RecallEffect prefab
                _recallEffectPrefab = _chronoBundle.LoadAsset<GameObject>("RecallEffect");

                // Cache RecallSound audio clip
                _recallSound = _chronoBundle.LoadAsset<AudioClip>("RecallSound");

                // Cache RecallKill icon
                _recallKillIcon = _chronoBundle.LoadAsset<Texture2D>("RecallKill_Icon");

                // Cache Spell icon from ChronoAssets/Sprites
                _spellIcon = _chronoBundle.LoadAsset<Texture2D>("Assets/ChronoAssets/Sprites/ChronosRewind_Ui.png");

                // Cache Spell main texture from ChronoAssets/Sprites
                _spellMainTexture = _chronoBundle.LoadAsset<Texture2D>("Assets/ChronoAssets/Sprites/ChronosRewind_Main.png");

                // Cache Spell emission texture from ChronoAssets/Sprites
                _spellEmissionTexture = _chronoBundle.LoadAsset<Texture2D>("Assets/ChronoAssets/Sprites/ChronosRewind_Emission.png");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Exception caching assets: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the RecallEffect prefab with fallback handling
        /// </summary>
        public static GameObject GetRecallEffectPrefab()
        {
            if (_recallEffectPrefab != null)
            {
                return _recallEffectPrefab;
            }


            return CreateFallbackEffectPrefab();
        }

        /// <summary>
        /// Get the RecallSound audio clip with fallback handling
        /// </summary>
        public static AudioClip GetRecallSound()
        {
            if (_recallSound != null)
            {
                return _recallSound;
            }

            Logger.LogDebug("RecallSound not available, using fallback");
            return null; // Game will handle missing audio gracefully
        }

        /// <summary>
        /// Get the RecallKill icon with fallback handling
        /// </summary>
        public static Texture2D GetRecallKillIcon()
        {
            if (_recallKillIcon != null)
            {
                return _recallKillIcon;
            }


            return CreateFallbackIcon();
        }

        /// <summary>
        /// Get the spell icon with fallback handling
        /// </summary>
        public static Texture2D GetSpellIcon()
        {
            if (_spellIcon != null)
            {
                return _spellIcon;
            }

            return CreateFallbackSpellIcon();
        }

        /// <summary>
        /// Get the spell main texture from AssetBundle
        /// </summary>
        public static Texture2D GetSpellMainTexture()
        {
            if (_spellMainTexture != null)
            {
                return _spellMainTexture;
            }

            return null; // Let BlackMagicAPI handle fallback
        }

        /// <summary>
        /// Get the spell emission texture from AssetBundle
        /// </summary>
        public static Texture2D GetSpellEmissionTexture()
        {
            if (_spellEmissionTexture != null)
            {
                return _spellEmissionTexture;
            }

            return null; // Let BlackMagicAPI handle fallback
        }

        /// <summary>
        /// Load a specific asset from the bundle with type safety
        /// </summary>
        public static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (!_bundleLoaded || _chronoBundle == null)
            {
                return null;
            }

            try
            {
                return _chronoBundle.LoadAsset<T>(assetName);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Exception loading asset '{assetName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a fallback effect prefab using built-in Unity components
        /// </summary>
        private static GameObject CreateFallbackEffectPrefab()
        {
            try
            {
                GameObject fallbackPrefab = new GameObject("FallbackRecallEffect");
                
                // Add a simple particle system for visual feedback
                var particleSystem = fallbackPrefab.AddComponent<ParticleSystem>();
                var main = particleSystem.main;
                main.startColor = new Color(0.4f, 0.8f, 1.0f, 0.8f); // Cyan-blue
                main.startLifetime = 2.0f;
                main.startSpeed = 5.0f;
                main.maxParticles = 50;
                
                var emission = particleSystem.emission;
                emission.rateOverTime = 25;
                
                var shape = particleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1.0f;
                
                return fallbackPrefab;
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to create fallback effect prefab: {ex.Message}");
                return new GameObject("EmptyRecallEffect");
            }
        }

        /// <summary>
        /// Create a fallback spell icon using a simple colored texture
        /// </summary>
        private static Texture2D CreateFallbackSpellIcon()
        {
            try
            {
                Texture2D fallbackIcon = new Texture2D(128, 128);
                Color[] pixels = new Color[128 * 128];
                
                // Create a temporal/clock-like icon with cyan colors
                Vector2 center = new Vector2(64, 64);
                float outerRadius = 58;
                float innerRadius = 45;
                
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        Vector2 pos = new Vector2(x, y);
                        float distance = Vector2.Distance(pos, center);
                        
                        if (distance <= outerRadius && distance >= innerRadius)
                        {
                            // Ring - bright cyan
                            pixels[y * 128 + x] = new Color(0.4f, 0.8f, 1.0f, 1.0f);
                        }
                        else if (distance < innerRadius)
                        {
                            // Inner circle - darker cyan with clock hands
                            float angle = Mathf.Atan2(y - 64, x - 64) * Mathf.Rad2Deg;
                            
                            // Create clock hand effect
                            if ((Mathf.Abs(angle) < 5 && distance < 40) || // Hour hand
                                (Mathf.Abs(angle - 90) < 3 && distance < 50)) // Minute hand
                            {
                                pixels[y * 128 + x] = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White hands
                            }
                            else
                            {
                                pixels[y * 128 + x] = new Color(0.2f, 0.4f, 0.6f, 0.8f); // Dark cyan background
                            }
                        }
                        else
                        {
                            // Outside - transparent
                            pixels[y * 128 + x] = Color.clear;
                        }
                    }
                }
                
                fallbackIcon.SetPixels(pixels);
                fallbackIcon.Apply();
                
                return fallbackIcon;
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to create fallback spell icon: {ex.Message}");
                return Texture2D.whiteTexture;
            }
        }

        /// <summary>
        /// Create a fallback icon using a simple colored texture
        /// </summary>
        private static Texture2D CreateFallbackIcon()
        {
            try
            {
                Texture2D fallbackIcon = new Texture2D(64, 64);
                Color[] pixels = new Color[64 * 64];
                
                // Create a simple cyan circle icon
                Vector2 center = new Vector2(32, 32);
                float radius = 28;
                
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        Vector2 pos = new Vector2(x, y);
                        float distance = Vector2.Distance(pos, center);
                        
                        if (distance <= radius)
                        {
                            // Inside circle - cyan color with alpha based on distance
                            float alpha = 1.0f - (distance / radius) * 0.5f;
                            pixels[y * 64 + x] = new Color(0.4f, 0.8f, 1.0f, alpha);
                        }
                        else
                        {
                            // Outside circle - transparent
                            pixels[y * 64 + x] = Color.clear;
                        }
                    }
                }
                
                fallbackIcon.SetPixels(pixels);
                fallbackIcon.Apply();
                
                return fallbackIcon;
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to create fallback icon: {ex.Message}");
                return Texture2D.whiteTexture;
            }
        }

        /// <summary>
        /// Check if the AssetBundle is loaded and available
        /// </summary>
        public static bool IsBundleLoaded => _bundleLoaded && _chronoBundle != null;

        /// <summary>
        /// Get information about loaded assets for debugging
        /// </summary>
        public static string GetAssetInfo()
        {
            if (!_bundleLoaded || _chronoBundle == null)
            {
                return "AssetBundle not loaded";
            }

            var assetNames = _chronoBundle.GetAllAssetNames();
            return $"AssetBundle loaded with {assetNames.Length} assets: {string.Join(", ", assetNames)}";
        }



        /// <summary>
        /// Clean up resources when the plugin is unloaded
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                if (_chronoBundle != null)
                {
                    _chronoBundle.Unload(false);
                    _chronoBundle = null;
                }
                
                _bundleLoaded = false;
                _recallEffectPrefab = null;
                _recallSound = null;
                _recallKillIcon = null;
                _spellIcon = null;
                _spellMainTexture = null;
                _spellEmissionTexture = null;
                
                Logger.LogDebug("AssetManager cleanup completed");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Exception during AssetManager cleanup: {ex.Message}");
            }
        }
    }
}