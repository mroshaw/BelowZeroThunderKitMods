using System.Collections.Generic;
using Nautilus.Json;
using Nautilus.Json.Attributes;

namespace DaftAppleGames.AutoLockerLabels_BZ
{
    /// <summary>
    /// Stores the lockers that have automatic labelling enabled in the
    /// current save slot.
    /// </summary>
    [FileName("auto_locker_labels")]
    internal sealed class SaveData : SaveDataCache
    {
        public HashSet<string> AutomaticLockerIds { get; set; } =
            new HashSet<string>();

        public Dictionary<string, string> CustomLabels { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, int> CustomLabelColors { get; set; } = new Dictionary<string, int>();
        
        internal bool IsAutomatic(string lockerId)
        {
            return !string.IsNullOrWhiteSpace(lockerId) &&
                   AutomaticLockerIds != null &&
                   AutomaticLockerIds.Contains(lockerId);
        }

        internal void EnableAutomatic(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            if (AutomaticLockerIds == null)
            {
                AutomaticLockerIds = new HashSet<string>();
            }

            AutomaticLockerIds.Add(lockerId);
        }

        internal void DisableAutomatic(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutomaticLockerIds?.Remove(lockerId);
        }

        internal bool TryGetCustomLabel(string lockerId, out string label)
        {
            label = string.Empty;
            return !string.IsNullOrWhiteSpace(lockerId) &&
                   CustomLabels != null &&
                   CustomLabels.TryGetValue(lockerId, out label);
        }

        internal void SetCustomLabel(string lockerId, string label)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            if (CustomLabels == null)
            {
                CustomLabels = new Dictionary<string, string>();
            }

            CustomLabels[lockerId] = label ?? string.Empty;
        }

        internal bool TryGetCustomLabelColor(string lockerId, out int colorIndex)
        {
            colorIndex = 0;
            return !string.IsNullOrWhiteSpace(lockerId) &&
                   CustomLabelColors != null &&
                   CustomLabelColors.TryGetValue(lockerId, out colorIndex);
        }

        internal void SetCustomLabelColor(string lockerId, int colorIndex)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            if (CustomLabelColors == null)
            {
                CustomLabelColors = new Dictionary<string, int>();
            }

            CustomLabelColors[lockerId] = colorIndex;
        }

        internal void RemoveLocker(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutomaticLockerIds?.Remove(lockerId);
            CustomLabels?.Remove(lockerId);
            CustomLabelColors?.Remove(lockerId);
        }
    }
}
