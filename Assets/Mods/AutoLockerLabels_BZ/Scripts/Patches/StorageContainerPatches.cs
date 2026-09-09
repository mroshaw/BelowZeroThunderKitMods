using System;
using DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;
using Object = UnityEngine.Object;

namespace DaftAppleGames.AutoLockerLabels_BZ.Patches
{
    [HarmonyPatch(typeof(StorageContainer))] internal static class StorageContainerPatches
    {
        private const string FreestandingLockerLabelPrefabName = "FreestandingLockerLabel.prefab";
        private const string WallLockerTogglePrefabName = "AutoToggleSmallLocker";
        private const string FloatingLockerTogglePrefabName = "AutoToggleSmallStorage";

        /// <summary>
        /// Adds an AutoLockerLabelController to storage lockers
        /// </summary>
        [HarmonyPatch(nameof(StorageContainer.Awake))]
        [HarmonyPostfix]
        private static void AwakePostFix(StorageContainer __instance)
        {
            TechType techType = CraftData.GetTechType(__instance.gameObject);
            // Add the new Label prefab and a LockerController to the freestanding locker
            if (techType == TechType.Locker)
            {
                ModDebugLog.LogDebug($"Instantiating label prefab instance on: '{__instance.name}'");
                GameObject labelPrefab =
                    ModAssetUtils.GetObjectFromAssetBundle<GameObject>(FreestandingLockerLabelPrefabName, false) as
                        GameObject;
                GameObject label = labelPrefab == null ? null : Object.Instantiate(labelPrefab);
                if (label == null)
                {
                    ModDebugLog.LogError(
                        $"Unable to instantiate '{FreestandingLockerLabelPrefabName}' for '{__instance.name}'.");
                    return;
                }

                LabelConfig labelConfig = label.GetComponent<LabelConfig>();
                label.transform.SetParent(__instance.transform, false);
                label.transform.localPosition = labelConfig.LabelOffset;
                label.transform.localRotation = Quaternion.identity;
                label.transform.localScale = Vector3.one;
                ModDebugLog.LogDebug($"Activating freestanding label instance '{label.name}'.");
                label.SetActive(true);

                __instance.gameObject.EnsureComponent<LockerController>();
                ModDebugLog.LogDebug($"LockerController added to storage container on: '{__instance.name}'. ");

                return;
            }

            // Add the new AutoToggle prefab and a LockerController to the small locker
            if (techType == TechType.SmallLocker || techType == TechType.SmallStorage)
            {
                ModDebugLog.LogDebug($"Instantiating auto toggle prefab instance on: '{__instance.name}'");
                string prefabAsset = techType == TechType.SmallLocker
                    ? WallLockerTogglePrefabName
                    : FloatingLockerTogglePrefabName;

                GameObject toggleObject = ModAssetUtils.GetPrefabInstanceFromAssetBundle(prefabAsset, true);
                AutoToggle autoToggle = toggleObject.GetComponent<AutoToggle>();

                GameObject lockerRoot = __instance.prefabRoot
                    ? __instance.prefabRoot
                    : __instance.gameObject;

                // Parent the new toggle object
                ColoredLabel coloredLabel = lockerRoot.GetComponentInChildren<ColoredLabel>(true);
                
                RectTransform toggleTransform = toggleObject.GetComponent<RectTransform>();
                toggleTransform.SetParent(coloredLabel.signInput.transform, false);
                toggleTransform.anchorMin = new Vector2(0.5f, 0.5f);
                toggleTransform.anchorMax = new Vector2(0.5f, 0.5f);
                toggleTransform.pivot = new Vector2(0.5f, 0.5f);
                toggleTransform.anchoredPosition3D = autoToggle.AnchoredOffset;
                toggleTransform.sizeDelta = autoToggle.Size;
                toggleTransform.localRotation = Quaternion.identity;
                toggleTransform.localScale = Vector3.one;

                // Only show the toggle object when the input is active
                uGUI_SignInput signInput = lockerRoot.GetComponentInChildren<uGUI_SignInput>(true);
                int numObjects = signInput.editOnly.Length;
                Array.Resize(ref signInput.editOnly, numObjects + 1);
                signInput.editOnly[numObjects] = toggleObject;
                toggleObject.SetActive(false);

                __instance.gameObject.EnsureComponent<LockerController>();
                ModDebugLog.LogDebug($"LockerController added to storage container on: '{__instance.name}'. ");
            }
        }
    }
}
