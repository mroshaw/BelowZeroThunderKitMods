using System.Collections.Generic;
using Nautilus.Handlers;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Tracks aquarium base cells across geometry rebuilds and saved games.
    /// </summary>
    internal static class BaseAquariumPersistence
    {
        private const float PositionToleranceSqr = 0.25f;
        private static readonly List<BaseAquariumLocation> Locations =
            new List<BaseAquariumLocation>();

        /// <summary>
        /// Registers the per-save aquarium location cache.
        /// </summary>
        internal static void Initialize()
        {
            BaseAquariumSaveData saveData =
                SaveDataHandler.RegisterSaveDataCache<BaseAquariumSaveData>();
            saveData.OnStartedSaving += (sender, eventArgs) =>
            {
                BaseAquariumSaveData data =
                    eventArgs.Instance as BaseAquariumSaveData;
                if (data != null)
                {
                    CaptureInventories();
                    data.Locations = new List<BaseAquariumLocation>(Locations);
                    ModDebugLog.LogInfo(
                        $"Saving {Locations.Count} Observatory Aquarium locations.");
                }
            };
            saveData.OnFinishedLoading += (sender, eventArgs) =>
            {
                BaseAquariumSaveData data =
                    eventArgs.Instance as BaseAquariumSaveData;
                Locations.Clear();
                if (data?.Locations != null)
                {
                    Locations.AddRange(data.Locations);
                }

                ModDebugLog.LogInfo(
                    $"Loaded {Locations.Count} Observatory Aquarium locations.");
            };
        }

        /// <summary>
        /// Records a newly constructed aquarium cell.
        /// </summary>
        internal static void AddLocation(Vector3 position)
        {
            if (ContainsLocation(position))
            {
                return;
            }

            Locations.Add(new BaseAquariumLocation(position));
            ModDebugLog.LogInfo(
                $"Recorded Observatory Aquarium location {position}.");
        }

        /// <summary>
        /// Reports whether a generated Observatory occupies a persisted aquarium location.
        /// </summary>
        internal static bool ContainsLocation(Vector3 position)
        {
            return GetLocation(position) != null;
        }

        /// <summary>
        /// Returns the persisted record matching a generated base piece.
        /// </summary>
        internal static BaseAquariumLocation GetLocation(Vector3 position)
        {
            foreach (BaseAquariumLocation location in Locations)
            {
                if ((location.ToVector3() - position).sqrMagnitude <= PositionToleranceSqr)
                {
                    return location;
                }
            }

            return null;
        }

        /// <summary>
        /// Copies live generated inventories into their persisted location records.
        /// </summary>
        private static void CaptureInventories()
        {
            BaseAquariumInventory[] inventories =
                Object.FindObjectsOfType<BaseAquariumInventory>();
            foreach (BaseAquariumInventory inventory in inventories)
            {
                BaseAquariumLocation location = GetLocation(inventory.transform.position);
                if (location != null)
                {
                    location.StoredItems = inventory.CaptureInventory();
                    ModDebugLog.LogInfo(
                        $"Captured {location.StoredItems.Count} Observatory Aquarium items " +
                        $"at {inventory.transform.position}.");
                }
            }
        }

        /// <summary>
        /// Removes the identity of a deconstructed aquarium cell.
        /// </summary>
        internal static void RemoveLocation(Vector3 position)
        {
            for (int locationIndex = Locations.Count - 1;
                 locationIndex >= 0; locationIndex--)
            {
                if ((Locations[locationIndex].ToVector3() - position).sqrMagnitude <=
                    PositionToleranceSqr)
                {
                    Locations.RemoveAt(locationIndex);
                    ModDebugLog.LogInfo(
                        $"Removed Observatory Aquarium location {position}.");
                }
            }
        }
    }
}
