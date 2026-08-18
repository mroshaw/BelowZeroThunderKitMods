using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Captures and restores inventory for a dynamically generated base aquarium.
    /// </summary>
    public class BaseAquariumInventory : MonoBehaviour
    {
        private StorageContainer storageContainer;

        /// <summary>
        /// Assigns the generated storage and restores its persisted contents.
        /// </summary>
        internal void Initialize(StorageContainer targetStorageContainer)
        {
            storageContainer = targetStorageContainer;
            StartCoroutine(RestoreInventory());
        }

        /// <summary>
        /// Returns item records for the current storage contents.
        /// </summary>
        internal List<BaseAquariumStoredItem> CaptureInventory()
        {
            List<BaseAquariumStoredItem> storedItems =
                new List<BaseAquariumStoredItem>();
            if (!storageContainer || storageContainer.container == null)
            {
                return storedItems;
            }

            foreach (InventoryItem inventoryItem in storageContainer.container)
            {
                if (inventoryItem == null || !inventoryItem.item)
                {
                    continue;
                }

                TechType techType = inventoryItem.item.GetTechType();
                string classId = CraftData.GetClassIdForTechType(techType);
                if (!string.IsNullOrEmpty(classId))
                {
                    storedItems.Add(new BaseAquariumStoredItem(classId));
                }
            }

            return storedItems;
        }

        /// <summary>
        /// Recreates persisted items once their prefabs are available.
        /// </summary>
        private IEnumerator RestoreInventory()
        {
            BaseAquariumLocation location =
                BaseAquariumPersistence.GetLocation(transform.position);
            if (location == null || location.StoredItems == null ||
                location.StoredItems.Count == 0)
            {
                yield break;
            }

            int restoredCount = 0;
            foreach (BaseAquariumStoredItem storedItem in location.StoredItems)
            {
                if (storedItem == null || string.IsNullOrEmpty(storedItem.ClassId))
                {
                    continue;
                }

                IPrefabRequest request = PrefabDatabase.GetPrefabAsync(storedItem.ClassId);
                yield return request;
                GameObject prefab;
                if (!request.TryGetPrefab(out prefab) || !prefab)
                {
                    ModDebugLog.LogError(
                        $"Could not restore aquarium item '{storedItem.ClassId}'.");
                    continue;
                }

                GameObject itemGameObject = Instantiate(prefab);
                Pickupable pickupable = itemGameObject.GetComponent<Pickupable>();
                if (!pickupable || storageContainer.container.AddItem(pickupable) == null)
                {
                    ModDebugLog.LogError(
                        $"Could not add restored aquarium item '{storedItem.ClassId}'.");
                    Destroy(itemGameObject);
                    continue;
                }

                restoredCount++;
            }

            ModDebugLog.LogInfo(
                $"Restored {restoredCount} Observatory Aquarium items at {transform.position}.");
        }
    }
}
