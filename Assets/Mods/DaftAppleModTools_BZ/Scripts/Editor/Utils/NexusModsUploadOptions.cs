using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DaftAppleGames.Editor
{
    [Serializable]
    public sealed class NexusModsUploadOptions
    {
        [FormerlySerializedAs("fileId")]
        [SerializeField] private string fileGroupId;
        [FormerlySerializedAs("modId")]
        [SerializeField] private string gameScopedModId;
        [SerializeField] private string gameDomain = "subnauticabelowzero";
        [SerializeField] private string displayName;
        [TextArea(3, 8)]
        [SerializeField] private string description;
        [TextArea(3, 8)]
        [SerializeField] private string changelog;
        [SerializeField] private string fileCategory = "main";
        [SerializeField] private bool archiveExistingVersion = true;
        [SerializeField] private bool updateModVersion = true;
        [SerializeField] private bool primaryModManagerDownload;
        [SerializeField] private bool allowModManagerDownload = true;
        [SerializeField] private bool showRequirementsPopup;

        public string FileGroupId => fileGroupId;
        public string GameScopedModId => gameScopedModId;
        public string GameDomain => gameDomain;
        public string DisplayName => displayName;
        public string Description => description;
        public string Changelog => changelog;
        public string FileCategory => fileCategory;
        public bool ArchiveExistingVersion => archiveExistingVersion;
        public bool UpdateModVersion => updateModVersion;
        public bool PrimaryModManagerDownload => primaryModManagerDownload;
        public bool AllowModManagerDownload => allowModManagerDownload;
        public bool ShowRequirementsPopup => showRequirementsPopup;
    }
}
