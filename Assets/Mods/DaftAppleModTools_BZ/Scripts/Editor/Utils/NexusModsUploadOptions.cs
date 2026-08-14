using System;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    [Serializable]
    public sealed class NexusModsUploadOptions
    {
        [SerializeField] private string fileId;
        [SerializeField] private string modId;
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

        public string FileId => fileId;
        public string ModId => modId;
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
