# Asset Creation Guide for Chronomancer's Paradox

This guide explains how to create the custom Unity assets required for the Chronomancer's Paradox mod.

## Prerequisites

- Unity Editor (version 2022.3 LTS recommended to match Mage Arena)
- Basic knowledge of Unity asset creation
- Audio editing software (optional, for custom sounds)

## Step 1: Create Unity Project

1. Open Unity Hub and create a new 3D project
2. Name it "ChronoAssets" or similar
3. Ensure you're using Unity 2022.3 LTS

## Step 2: Create Asset Structure

Create the following folder structure in your Assets folder:

```
Assets/
├── ChronoAssets/
│   ├── Prefabs/
│   ├── Audio/
│   ├── Icons/
│   └── Editor/
```

## Step 3: Create Required Assets

### RecallEffect.prefab

1. **Create Empty GameObject**: Right-click in Prefabs folder → Create → Empty GameObject
2. **Name it**: "RecallEffect"
3. **Add Particle System**: Add Component → Effects → Particle System
4. **Configure Particle System**:
   - **Main Module**:
     - Start Lifetime: 2.0
     - Start Speed: 5.0
     - Start Color: Cyan-blue (R: 0.4, G: 0.8, B: 1.0, A: 0.8)
     - Max Particles: 100
   - **Emission Module**:
     - Rate over Time: 50
     - Bursts: Add burst at time 0 with count 30
   - **Shape Module**:
     - Shape: Sphere
     - Radius: 1.0
   - **Velocity over Lifetime**:
     - Linear: (0, 2, 0) - particles move upward
   - **Color over Lifetime**:
     - Gradient from full cyan to transparent
   - **Size over Lifetime**:
     - Curve that starts at 0.5, peaks at 1.0, then fades to 0
5. **Add Audio Source** (optional): For integrated sound effects
6. **Save as Prefab**: Drag to Prefabs folder

### RecallSound.wav

**Option A: Create Custom Audio**
1. Use audio editing software (Audacity, etc.)
2. Create a 3-5 second mystical/temporal sound effect
3. Suggested elements:
   - Whoosh sound (wind/air movement)
   - Subtle magical chime or bell
   - Brief echo/reverb effect
   - Frequency sweep (high to low pitch)
4. Export as WAV format, 44.1kHz, 16-bit

**Option B: Use Placeholder**
1. Record a simple "whoosh" sound
2. Apply reverb and pitch modulation
3. Keep it under 5 seconds

**Import to Unity**:
1. Drag WAV file to Audio folder
2. Select the audio clip
3. In Inspector, set:
   - Load Type: Compressed in Memory
   - Compression Format: Vorbis
   - Quality: 70% (balance between quality and size)

### RecallKill_Icon.png

1. **Create 64x64 pixel image** in image editor (Photoshop, GIMP, etc.)
2. **Design suggestions**:
   - Circular icon with cyan-blue color scheme
   - Clock or hourglass symbol
   - Swirl or spiral pattern
   - Temporal/time-related imagery
3. **Technical requirements**:
   - Size: 64x64 pixels
   - Format: PNG with transparency
   - Background: Transparent
4. **Import to Unity**:
   - Drag PNG to Icons folder
   - Set Texture Type to "Sprite (2D and UI)"
   - Set Max Size to 64
   - Apply changes

## Step 4: Create AssetBundle Build Script

Create a script to build the AssetBundle:

1. **Create Editor Script**: In Editor folder, create "AssetBundleBuilder.cs"
2. **Copy this code**:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetBundleBuilder : EditorWindow
{
    [MenuItem("Tools/Build Chronomancer AssetBundle")]
    public static void BuildAssetBundle()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        // Set AssetBundle names
        SetAssetBundleNames();

        // Build the AssetBundle
        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        Debug.Log("AssetBundle built successfully!");
        
        // Copy to mod directory (adjust path as needed)
        string sourcePath = Path.Combine(assetBundleDirectory, "chronomancer");
        string destPath = "../../ChronoPara/src/Resources/chronomancer.bundle";
        
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, destPath, true);
            Debug.Log($"AssetBundle copied to: {destPath}");
        }
    }

    private static void SetAssetBundleNames()
    {
        // Set AssetBundle name for RecallEffect prefab
        string prefabPath = "Assets/ChronoAssets/Prefabs/RecallEffect.prefab";
        AssetImporter.GetAtPath(prefabPath).assetBundleName = "chronomancer";

        // Set AssetBundle name for RecallSound
        string audioPath = "Assets/ChronoAssets/Audio/RecallSound.wav";
        AssetImporter.GetAtPath(audioPath).assetBundleName = "chronomancer";

        // Set AssetBundle name for RecallKill_Icon
        string iconPath = "Assets/ChronoAssets/Icons/RecallKill_Icon.png";
        AssetImporter.GetAtPath(iconPath).assetBundleName = "chronomancer";
    }
}
```

## Step 5: Build the AssetBundle

1. **Set AssetBundle Names**:
   - Select RecallEffect.prefab → Inspector → AssetBundle → "chronomancer"
   - Select RecallSound.wav → Inspector → AssetBundle → "chronomancer"
   - Select RecallKill_Icon.png → Inspector → AssetBundle → "chronomancer"

2. **Build AssetBundle**:
   - Go to Tools → Build Chronomancer AssetBundle
   - Or manually: Window → AssetBundle Browser → Build → Build

3. **Copy to Mod Directory**:
   - Find the generated "chronomancer" file in Assets/AssetBundles/
   - Copy it to `ChronoPara/src/Resources/chronomancer.bundle`

## Step 6: Test Integration

1. **Build the mod** using the existing build tools
2. **Install in Mage Arena** and test
3. **Check logs** for asset loading messages
4. **Verify effects** appear when casting Temporal Recall

## Troubleshooting

### AssetBundle Not Loading
- Check file path: `ChronoPara/src/Resources/chronomancer.bundle`
- Verify file permissions
- Check Unity console for build errors

### Assets Not Found in Bundle
- Verify AssetBundle names are set correctly
- Ensure all assets are included in build
- Check asset names match exactly in code

### Effects Not Appearing
- Check particle system settings
- Verify prefab has required components
- Test with fallback effects first

### Audio Not Playing
- Check audio import settings
- Verify AudioSource component configuration
- Test with simple audio clip first

## Asset Specifications Summary

| Asset | Type | Size | Requirements |
|-------|------|------|-------------|
| RecallEffect.prefab | Prefab | N/A | Particle system, cyan-blue theme |
| RecallSound.wav | Audio | <1MB | 3-5 seconds, mystical/temporal theme |
| RecallKill_Icon.png | Texture | 64x64 | PNG with transparency, time/clock theme |

## Next Steps

After creating the assets:
1. Test the mod with custom assets
2. Adjust particle effects based on gameplay feedback
3. Fine-tune audio levels and timing
4. Consider additional visual effects for enhanced experience

The AssetManager in the mod provides fallback handling, so the mod will work even if custom assets are missing, but the experience will be enhanced with proper assets.