using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using Nautilus.Handlers;
using UnityEngine;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Captures item presentation data for one opening of the category picker.
    /// </summary>
    internal sealed class TechTypeDisplayData
    {
        internal TechType TechType { get; }
        internal string DisplayName { get; }
        internal string AssignedDisplayName { get; }
        internal string SourceName { get; }
        internal bool IsModded { get; }
        internal Sprite Icon { get; }
        internal Color SourceColor => IsModded
            ? new Color(1f, 0.68f, 0.05f, 1f)
            : new Color(0.42f, 0.84f, 1f, 1f);

        internal TechTypeDisplayData(TechType techType)
        {
            TechType = techType;
            string techTypeName = techType.ToString();
            string localizedName = Language.main == null ? string.Empty : Language.main.Get(techType);
            AssignedDisplayName = string.IsNullOrWhiteSpace(localizedName)
                ? techTypeName
                : localizedName + "  (" + techTypeName + ")";
            DisplayName = localizedName == techTypeName ? techTypeName : AssignedDisplayName;
            Icon = SpriteManager.Get(techType);

            IsModded = EnumHandler.TryGetOwnerAssembly(techType, out Assembly ownerAssembly);
            SourceName = IsModded ? GetModName(ownerAssembly) : "Below Zero";
        }

        private static string GetModName(Assembly ownerAssembly)
        {
            foreach (KeyValuePair<string, BepInEx.PluginInfo> plugin in Chainloader.PluginInfos)
            {
                if (plugin.Value.Instance != null &&
                    plugin.Value.Instance.GetType().Assembly == ownerAssembly)
                {
                    return plugin.Value.Metadata.Name;
                }
            }

            return ownerAssembly.GetName().Name;
        }
    }
}
