using System;
using ThunderKit.Core.Manifests;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    [Serializable]
    public class ModVersionEntry
    {
        [SerializeField] private string name;
        [SerializeField] private MonoScript pluginScript;
        [SerializeField] private Manifest manifest;
        [SerializeField] private ModVersion version = new ModVersion();
        [SerializeField] private NexusModsUploadOptions nexusMods = new NexusModsUploadOptions();

        public string Name => name;
        public MonoScript PluginScript => pluginScript;
        public Manifest Manifest => manifest;
        public ModVersion Version => version;
        public NexusModsUploadOptions NexusMods => nexusMods;
    }
}
