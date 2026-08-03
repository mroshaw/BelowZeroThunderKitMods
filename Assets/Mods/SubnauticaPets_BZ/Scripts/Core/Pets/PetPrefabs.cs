using System.Collections;
using Nautilus.Assets;
using UWE;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    // Class IDs can be found here: https://github.com/SubnauticaModding/Nautilus/blob/master/Nautilus/Documentation/resources/BZ-PrefabPaths.json
    internal static class PetPrefabs
    {
        /// <summary>
        ///     Sets up all pet prefabs.
        /// </summary>
        internal static void RegisterAll()
        {
            PenglingBabyPrefab.Register();
            PengwingAdultPrefab.Register();
            PinnacaridPrefab.Register();
            SnowstalkerBabyPrefab.Register();
            RockPuncherPrefab.Register();
            TrivalveBluePrefab.Register();
            TrivalveYellowPrefab.Register();
        }

        /// <summary>
        ///     Creates an authored pet prefab and grafts a model from a vanilla prefab onto it.
        /// </summary>
        internal static IEnumerator CreateWithVanillaModel(IOut<GameObject> result, PrefabInfo info,
            string prefabAssetName, string vanillaClassId)
        {
            GameObject prefabAsset =
                ModAssetUtils.GetObjectFromAssetBundle<GameObject>(prefabAssetName) as GameObject;
            if (!prefabAsset)
            {
                ModDebugLog.LogError($"Could not load pet prefab asset '{prefabAssetName}'.");
                yield break;
            }

            GameObject petPrefab = Object.Instantiate(prefabAsset);
            petPrefab.SetActive(false);

            PetConfigurator configurator = petPrefab.GetComponent<PetConfigurator>();
            if (!TryValidateConfigurator(configurator, prefabAssetName))
            {
                Object.Destroy(petPrefab);
                yield break;
            }

            IPrefabRequest vanillaPrefabRequest = PrefabDatabase.GetPrefabAsync(vanillaClassId);
            yield return vanillaPrefabRequest;

            if (!vanillaPrefabRequest.TryGetPrefab(out var vanillaPrefab))
            {
                ModDebugLog.LogError($"Could not load vanilla prefab with class ID '{vanillaClassId}'.");
                Object.Destroy(petPrefab);
                yield break;
            }

            GameObject vanillaInstance = UWE.Utils.InstantiateDeactivated(vanillaPrefab);
            Transform modelTransform = FindChild(vanillaInstance.transform, configurator.ModelGameObjectName);
            if (!modelTransform)
            {
                ModDebugLog.LogError(
                    $"Could not find model '{configurator.ModelGameObjectName}' in vanilla prefab '{vanillaClassId}'.");
                Object.Destroy(vanillaInstance);
                Object.Destroy(petPrefab);
                yield break;
            }

            modelTransform.SetParent(configurator.ModelParent.transform, false);
            Object.Destroy(vanillaInstance);

            PetPrefabConfigUtils.ConfigurePrefabIdentifier(petPrefab, info.ClassID, info.TechType);
            PetPrefabConfigUtils.ConfigureLandOnlyCreature(petPrefab);
            petPrefab.name = info.ClassID;
            result.Set(petPrefab);
            ModDebugLog.LogDebug($"Attached vanilla model '{configurator.ModelGameObjectName}' to {info.TechType}.");
        }

        private static bool TryValidateConfigurator(PetConfigurator configurator, string prefabAssetName)
        {
            if (!configurator)
            {
                ModDebugLog.LogError($"Pet prefab '{prefabAssetName}' has no PetConfigurator component.");
                return false;
            }

            if (!configurator.ModelParent)
            {
                ModDebugLog.LogError($"Pet prefab '{prefabAssetName}' has no model parent configured.");
                return false;
            }

            if (string.IsNullOrEmpty(configurator.ModelGameObjectName))
            {
                ModDebugLog.LogError($"Pet prefab '{prefabAssetName}' has no model GameObject name configured.");
                return false;
            }

            return true;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
