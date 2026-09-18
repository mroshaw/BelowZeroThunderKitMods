using HarmonyLib;
using UnityEngine;
using DaftAppleGames.VehicleEnhancements_BZ.Hsi;
using DaftAppleGames.VehicleEnhancements_BZ.Reversing;
using DaftAppleGames.VehicleEnhancements_BZ.Speedometer;
using DaftAppleGames.VehicleEnhancements_BZ.TimeAndWeather;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Patches
{
    [HarmonyPatch(typeof(uGUI))]
    internal static class UGuiPatches
    {
        private const string EnhancedIndicatorsPrefabName = "EnhancedIndicators";

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AwakePostfix(uGUI __instance)
        {
            if (!__instance)
            {
                return;
            }

            uGUI_SeaTruckHUD seaTruckHud = __instance.GetComponentInChildren<uGUI_SeaTruckHUD>(true);
            if (seaTruckHud && seaTruckHud.root)
            {
                AddEnhancedIndicators(seaTruckHud.root.transform, EnhancedVehicle.Seatruck);
            }

            uGUI_ExosuitHUD exosuitHud = __instance.GetComponentInChildren<uGUI_ExosuitHUD>(true);
            if (exosuitHud && exosuitHud.root)
            {
                AddEnhancedIndicators(exosuitHud.root.transform, EnhancedVehicle.PrawnSuit);
            }

            uGUI_HoverbikeHUD hoverbikeHud = __instance.GetComponentInChildren<uGUI_HoverbikeHUD>(true);
            if (hoverbikeHud && hoverbikeHud.root)
            {
                AddEnhancedIndicators(hoverbikeHud.root.transform, EnhancedVehicle.Snowfox);
            }
        }

        private static void AddEnhancedIndicators(Transform hudRoot, EnhancedVehicle vehicle)
        {
            if (hudRoot.Find(EnhancedIndicatorsPrefabName))
            {
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
            enhancedIndicators.transform.SetParent(hudRoot, false);

            SpeedometerController speedometer = enhancedIndicators.GetComponent<SpeedometerController>();
            HsiController hsi = enhancedIndicators.GetComponent<HsiController>();
            TimeAndWeatherController timeAndWeather = enhancedIndicators.GetComponent<TimeAndWeatherController>();
            ReversingAudioController reversingAudio = enhancedIndicators.GetComponent<ReversingAudioController>();
            ReversingCameraController reversingCamera = enhancedIndicators.GetComponent<ReversingCameraController>();
            if (!speedometer || !hsi || !timeAndWeather || !reversingAudio ||
                (vehicle == EnhancedVehicle.Seatruck && !reversingCamera))
            {
                ModDebugLog.LogError("Enhanced indicator prefab is missing a required controller.");
                Object.Destroy(enhancedIndicators);
                return;
            }

            speedometer.Configure(vehicle);
            hsi.Configure(vehicle);
            timeAndWeather.Configure(vehicle);
            reversingAudio.Configure(vehicle);

            if (vehicle != EnhancedVehicle.Seatruck)
            {
                RectTransform hudContent = hudRoot.parent as RectTransform;
                if (!hudContent)
                {
                    ModDebugLog.LogError($"Could not align enhanced indicators with the {vehicle} HUD.");
                    Object.Destroy(enhancedIndicators);
                    return;
                }

                enhancedIndicators.AddComponent<VehicleHudLayout>().Configure(hudContent);

                if (reversingCamera)
                {
                    reversingCamera.enabled = false;
                }
            }

            enhancedIndicators.SetActive(true);
            ModDebugLog.LogDebug($"Added enhanced indicators to the {vehicle} HUD.");
        }
    }
}
