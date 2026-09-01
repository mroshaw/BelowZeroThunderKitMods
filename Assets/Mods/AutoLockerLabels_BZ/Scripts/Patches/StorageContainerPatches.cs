using DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ.Patches
{
    [HarmonyPatch(typeof(StorageContainer))] internal static class StorageContainerPatches
    {
        private const string FreestandingLockerLabelPrefabName = "FreestandingLockerLabel.prefab";
        private const string WallLockerTogglePrefabName = "AutoToggle.prefab";

        /// <summary>
        /// Adds an AutoLockerLabelController to storage lockers
        /// </summary>
        [HarmonyPatch(nameof(StorageContainer.Awake))]
        [HarmonyPostfix]
        private static void AwakePostFix(StorageContainer __instance)
        {
            // Add the new Label prefab and a LockerController to the freestanding locker
            if (CraftData.GetTechType(__instance.gameObject) == TechType.Locker)
            {
                ModDebugLog.LogDebug($"Instantiating label prefab instance on: '{__instance.name}'");
                GameObject labelPrefab = ModAssetUtils.GetObjectFromAssetBundle<GameObject>(FreestandingLockerLabelPrefabName, false) as GameObject;
                LogColoredLabelDiagnostics("asset-bundle prefab", labelPrefab);
                GameObject label = labelPrefab == null ? null : Object.Instantiate(labelPrefab);
                LogColoredLabelDiagnostics("inactive prefab instance", label);
                if (label == null)
                {
                    ModDebugLog.LogError($"Unable to instantiate '{FreestandingLockerLabelPrefabName}' for '{__instance.name}'.");
                    return;
                }

                LabelConfig labelConfig = label.GetComponent<LabelConfig>();
                label.transform.SetParent(__instance.transform, false);
                label.transform.localPosition = labelConfig.LabelOffset;
                label.transform.localRotation = Quaternion.identity;
                label.transform.localScale = Vector3.one;
                LogColoredLabelDiagnostics("parented inactive prefab instance", label);
                ModDebugLog.LogDebug($"Activating freestanding label instance '{label.name}'.");
                label.SetActive(true);
                LogColoredLabelDiagnostics("active prefab instance", label);
                
                __instance.gameObject.EnsureComponent<LockerController>();
                ModDebugLog.LogDebug($"LockerController added to storage container on: '{__instance.name}'. ");
                
                return;
            }

            // Add the new AutoToggle prefab and a LockerController to the small locker
            if (CraftData.GetTechType(__instance.gameObject) == TechType.SmallLocker)
            {
                ModDebugLog.LogDebug($"Instantiating auto toggle prefab instance on: '{__instance.name}'");
                GameObject toggleObject = ModAssetUtils.GetPrefabInstanceFromAssetBundle(WallLockerTogglePrefabName, true);
                AutoToggle autoToggle = toggleObject.GetComponent<AutoToggle>();
                
                ColoredLabel coloredLabel = __instance.GetComponentInChildren<ColoredLabel>(true);
                RectTransform toggleTransform = toggleObject.GetComponent<RectTransform>();;
                toggleTransform.transform.SetParent(coloredLabel.signInput.transform, false);
                toggleTransform.anchorMin = new Vector2(0.5f, 0.5f);
                toggleTransform.anchorMax = new Vector2(0.5f, 0.5f);
                toggleTransform.pivot = new Vector2(0.5f, 0.5f);
                toggleTransform.anchoredPosition3D = autoToggle.AnchoredOffset;
                toggleTransform.sizeDelta = autoToggle.Size;
                toggleTransform.localRotation = Quaternion.identity;
                toggleTransform.localScale = Vector3.one;

                __instance.gameObject.EnsureComponent<LockerController>();
                ModDebugLog.LogDebug($"LockerController added to storage container on: '{__instance.name}'. ");
            }
        }

        private static void LogColoredLabelDiagnostics(string stage, GameObject labelObject)
        {
            if (labelObject == null)
            {
                ModDebugLog.LogError($"ColoredLabel diagnostics at '{stage}': label GameObject is null.");
                return;
            }

            ColoredLabel[] coloredLabels = labelObject.GetComponentsInChildren<ColoredLabel>(true);
            uGUI_SignInput[] signInputs = labelObject.GetComponentsInChildren<uGUI_SignInput>(true);
            ModDebugLog.LogDebug($"ColoredLabel diagnostics at '{stage}': object='{labelObject.name}', activeSelf={labelObject.activeSelf}, activeInHierarchy={labelObject.activeInHierarchy}, ColoredLabel count={coloredLabels.Length}, uGUI_SignInput count={signInputs.Length}.");

            foreach (ColoredLabel coloredLabel in coloredLabels)
            {
                string signInputName = coloredLabel.signInput == null ? "null" : coloredLabel.signInput.gameObject.name;
                bool matchesDiscoveredSignInput = signInputs.Length == 1 && coloredLabel.signInput == signInputs[0];
                ModDebugLog.LogDebug($"ColoredLabel diagnostics at '{stage}': componentObject='{coloredLabel.gameObject.name}', componentEnabled={coloredLabel.enabled}, componentActive={coloredLabel.gameObject.activeInHierarchy}, serializedSignInput='{signInputName}', matchesDiscoveredSignInput={matchesDiscoveredSignInput}.");
            }

            foreach (uGUI_SignInput signInput in signInputs)
            {
                ModDebugLog.LogDebug($"ColoredLabel diagnostics at '{stage}': discovered uGUI_SignInput object='{signInput.gameObject.name}', enabled={signInput.enabled}, activeSelf={signInput.gameObject.activeSelf}, activeInHierarchy={signInput.gameObject.activeInHierarchy}.");
            }
        }
    }
}
