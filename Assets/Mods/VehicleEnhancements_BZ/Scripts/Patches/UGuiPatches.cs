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
        internal const string EnhancedIndicatorsPrefabName = "EnhancedIndicators";

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

        internal static bool ConfigureEnhancedIndicators(GameObject indicators, Transform hudRoot, EnhancedVehicle vehicle)
        {
            SpeedometerController speedometer = indicators.GetComponent<SpeedometerController>();
            HsiController hsi = indicators.GetComponent<HsiController>();
            TimeAndWeatherController timeAndWeather = indicators.GetComponent<TimeAndWeatherController>();
            ReversingAudioController reversingAudio = indicators.GetComponent<ReversingAudioController>();
            ReversingCameraController reversingCamera = indicators.GetComponent<ReversingCameraController>();
            if (!speedometer || !hsi || !timeAndWeather || !reversingAudio ||
                (vehicle == EnhancedVehicle.Seatruck && !reversingCamera))
            {
                ModDebugLog.LogError("Enhanced indicator prefab is missing a required controller.");
                return false;
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
                    return false;
                }

                VehicleHudLayout layout = indicators.GetComponent<VehicleHudLayout>();
                if (!layout)
                {
                    layout = indicators.AddComponent<VehicleHudLayout>();
                }

                layout.Configure(hudContent);

                if (reversingCamera)
                {
                    reversingCamera.enabled = false;
                }
            }

            return true;
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

            enhancedIndicators.SetActive(false);
            enhancedIndicators.name = EnhancedIndicatorsPrefabName;
            enhancedIndicators.transform.SetParent(hudRoot, false);

            if (!ConfigureEnhancedIndicators(enhancedIndicators, hudRoot, vehicle))
            {
                Object.Destroy(enhancedIndicators);
                return;
            }

            enhancedIndicators.SetActive(true);
            ModDebugLog.LogDebug($"Added enhanced indicators to the {vehicle} HUD.");
        }
    }
}
