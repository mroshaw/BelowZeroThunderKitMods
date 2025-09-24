using HarmonyLib;
using static DaftAppleGames.SeaTruckFishScoop_BZ.SeaTruckFishScoopPluginBz;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patches for the SeaTruck Fish Scoop Mod
    /// SeaTruckSegment class patches
    /// </summary>
    ///
    [HarmonyPatch(typeof(SeaTruckSegment))]
    public class SeaTruckSegmentPatches
    {
        /// <summary>
        /// Add a FishScoop to every spawned SeaTruck
        /// </summary>
        [HarmonyPatch(nameof(SeaTruckSegment.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(SeaTruckSegment __instance)
        {
            if (__instance.isMainCab)
            {
                ModDebugLog.LogDebug("Adding SeaTruckFishScoop components...");
                __instance.gameObject.AddComponent<FishScoop>();
                ModDebugLog.LogDebug("SeaTruckFishScoop components added.");
            }
        }
        
        /// <summary>
        /// Update FishScoop state when detaching an aquarium
        /// </summary>
        [HarmonyPatch(nameof(SeaTruckSegment.Detach))]
        [HarmonyPostfix]
        public static void Detach_Postfix(SeaTruckSegment __instance)
        {
            // Only interested in Aquariums
            if (!__instance.GetComponent<SeaTruckAquarium>())
            {
                return;
            }

            FishScoop fishScoop = __instance.motor.GetComponent<FishScoop>();
            if (fishScoop)
            {
                ModDebugLog.LogDebug("Aquarium has been detached. Reevaluating fish scoop state...");
                fishScoop.EvaluateScoopState();
            }
        }
    }
}