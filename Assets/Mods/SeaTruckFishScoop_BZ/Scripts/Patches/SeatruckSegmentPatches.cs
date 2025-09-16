using HarmonyLib;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patches for the SeaTruck Fish Scoop Mod
    /// SeaTruckSegment class patches
    /// </summary>
    ///
    [HarmonyPatch(typeof(SeaTruckSegment))]
    public class SeatruckSegmentPatches
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
                SeaTruckFishScoopPluginBz.Log.LogDebug("Adding SeaTruckFishScoop components...");
                __instance.gameObject.AddComponent<FishScoop>();
                SeaTruckFishScoopPluginBz.Log.LogDebug("SeaTruckFishScoop components added.");
            }
        }
    }
}