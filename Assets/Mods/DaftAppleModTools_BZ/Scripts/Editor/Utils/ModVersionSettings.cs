using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    public sealed class ModVersionSettings : ScriptableObject
    {
        private const string SettingsAssetPath =
            "Assets/Mods/DaftAppleModTools_BZ/Scripts/Editor/Utils/ModVersionSettings.asset";

        private static ModVersionSettings instance;

        [SerializeField] private List<ModVersionEntry> mods = new List<ModVersionEntry>();

        public static ModVersionSettings Instance => instance == null ? LoadOrCreate() : instance;
        public IReadOnlyList<ModVersionEntry> Mods => mods;

        /// <summary>
        /// Saves the project-specific mod version configuration
        /// </summary>
        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private static ModVersionSettings LoadOrCreate()
        {
            instance = AssetDatabase.LoadAssetAtPath<ModVersionSettings>(SettingsAssetPath);
            if (instance != null)
            {
                return instance;
            }

            instance = CreateInstance<ModVersionSettings>();
            AssetDatabase.CreateAsset(instance, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return instance;
        }
    }
}
