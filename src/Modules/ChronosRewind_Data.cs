using UnityEngine;
using BlackMagicAPI.Modules.Spells;

namespace ChronoPara.Modules
{
    /// <summary>
    /// Spell data definition for the Chronos Rewind spell
    /// Defines static properties and discovery mechanics for the spell
    /// </summary>
    internal class ChronosRewind_Data : SpellData
    {
        /// <summary>
        /// The display name of the spell
        /// </summary>
        public override string Name => "Chronos Rewind";
        
        /// <summary>
        /// Sub-names for the spell (used for search/filtering)
        /// </summary>
        public override string[] SubNames => ["Chronos", "Rewind"];
        
        /// <summary>
        /// The cooldown time in seconds, sourced from configuration for runtime adjustability
        /// </summary>
        public override float Cooldown => ConfigManager.RecallCooldown;
        
        /// <summary>
        /// The glow color for the spell - cyan-blue to suggest temporal/magical nature
        /// </summary>
        public override Color GlowColor => new Color(0.4f, 0.8f, 1.0f, 1.0f);
        
        /// <summary>
        /// Indicates whether this spell can spawn in team chests (configurable, disabled by default)
        /// </summary>
        public override bool CanSpawnInTeamChest => ConfigManager.CanSpawnInChests;

        /// <summary>
        /// Override to load main texture from AssetBundle
        /// </summary>
        public override Texture2D GetMainTexture()
        {
            // Load from our AssetBundle
            return AssetManager.GetSpellMainTexture() ?? base.GetMainTexture();
        }

        /// <summary>
        /// Override to load emission texture from AssetBundle
        /// </summary>
        public override Texture2D GetEmissionTexture()
        {
            // Load from our AssetBundle
            return AssetManager.GetSpellEmissionTexture() ?? base.GetEmissionTexture();
        }
    }
}