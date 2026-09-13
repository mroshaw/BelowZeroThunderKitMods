using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ.Patches
{
    [HarmonyPatch(typeof(uGUI))]
    internal static class UGuiPatches
    {
        private const string IndicatorsObjectName = "Indicators";
        private const string EnhancedIndicatorsPrefabName = "SeatruckEnhancedIndicators";

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AwakePostfix(uGUI __instance)
        {
            if (!__instance)
            {
                return;
            }

            uGUI_SeaTruckHUD seaTruckHud = __instance.GetComponentInChildren<uGUI_SeaTruckHUD>(true);
            if (!seaTruckHud || !seaTruckHud.root)
            {
                ModDebugLog.LogError("Could not find the SeaTruck HUD.");
                return;
            }

            Transform indicators = seaTruckHud.root.transform.Find(IndicatorsObjectName);
            if (!indicators)
            {
                ModDebugLog.LogError("Could not find the SeaTruck HUD Indicators object.");
                return;
            }

            if (indicators.Find(EnhancedIndicatorsPrefabName))
            {
                ModDebugLog.LogDebug("SeaTruck enhanced indicators are already present.");
                return;
            }

            GameObject enhancedIndicators = ModAssetUtils.GetPrefabInstanceFromAssetBundle(
                EnhancedIndicatorsPrefabName,
                false);
            if (!enhancedIndicators)
            {
                ModDebugLog.LogError($"Could not instantiate '{EnhancedIndicatorsPrefabName}'.");
                return;
            }

            enhancedIndicators.name = EnhancedIndicatorsPrefabName;
            enhancedIndicators.transform.SetParent(indicators, false);
            enhancedIndicators.SetActive(true);
            ModDebugLog.LogDebug("Added enhanced indicators to the SeaTruck HUD.");
        }
    }
}
