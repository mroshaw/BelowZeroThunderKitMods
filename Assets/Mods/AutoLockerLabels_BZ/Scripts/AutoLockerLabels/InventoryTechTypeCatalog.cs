using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Resolves and caches which registered TechTypes represent inventory pickupables.
    /// </summary>
    internal sealed class InventoryTechTypeCatalog
    {
        private readonly Dictionary<TechType, bool> pickupableByTechType =
            new Dictionary<TechType, bool>();

        internal IEnumerator Filter(
            IReadOnlyList<TechType> candidates,
            Action<TechType> accepted,
            Action<TechType> rejected,
            Action<int, int> progress,
            Action completed)
        {
            int total = candidates.Count;
            int processed = 0;
            foreach (TechType techType in candidates)
            {
                bool isPickupable;
                if (!pickupableByTechType.TryGetValue(techType, out isPickupable))
                {
                    CoroutineTask<GameObject> prefabTask = null;
                    try
                    {
                        prefabTask = CraftData.GetPrefabForTechTypeAsync(techType, false);
                    }
                    catch (Exception exception)
                    {
                        AutoLockerLabelsPlugin.ModDebugLog.LogDebug(
                            "Could not request prefab for " + techType + ": " + exception.Message);
                    }

                    if (prefabTask != null)
                    {
                        yield return prefabTask;
                        GameObject prefab = null;
                        try
                        {
                            prefab = prefabTask.GetResult();
                            isPickupable = prefab != null && prefab.GetComponent<Pickupable>() != null;
                        }
                        catch (Exception exception)
                        {
                            AutoLockerLabelsPlugin.ModDebugLog.LogDebug(
                                "Could not inspect prefab for " + techType + ": " + exception.Message);
                        }

                        prefab = null;
                    }

                    pickupableByTechType[techType] = isPickupable;
                }

                if (isPickupable)
                {
                    accepted(techType);
                }
                else
                {
                    rejected(techType);
                }

                processed++;
                progress(processed, total);
            }

            completed();
        }
    }
}
