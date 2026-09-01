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
        
        internal bool IsAutomatic(string lockerId)
        {
            return !string.IsNullOrWhiteSpace(lockerId) &&
                   AutomaticLockerIds.Contains(lockerId);
        }

        internal void EnableAutomatic(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutomaticLockerIds.Add(lockerId);
        }

        internal void DisableAutomatic(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutomaticLockerIds.Remove(lockerId);
        }

        internal bool TryGetCustomLabel(string lockerId, out string label)
        {
            label = string.Empty;
            return !string.IsNullOrWhiteSpace(lockerId) && CustomLabels.TryGetValue(lockerId, out label);
        }

        internal void SetCustomLabel(string lockerId, string label)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            CustomLabels[lockerId] = label ?? string.Empty;
        }

        internal void RemoveLocker(string lockerId)
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutomaticLockerIds.Remove(lockerId);
            CustomLabels.Remove(lockerId);
        }
    }
}
