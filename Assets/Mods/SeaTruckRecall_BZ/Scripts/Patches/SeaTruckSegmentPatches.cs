using HarmonyLib;
using DaftAppleGames.SeaTruckRecall_BZ.Navigation;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Patches
{
    /// <summary>
    /// Harmony patches for the SeaTruck
    /// </summary>
    [HarmonyPatch(typeof(SeaTruckSegment))]
    internal class SeaTruckSegmentPatches
    {
        /// <summary>
        /// Patch the Start method, to add the instance
        /// to the static global list
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(SeaTruckSegment.Start))]
        [HarmonyPostfix]
        internal static void StartPostfix(SeaTruckSegment __instance)
        {
            if (!__instance.isMainCab)
            {
                return;
            }
            // Add the new AutoPilot component
            ModDebugLog.LogDebug("Adding SeaTruckAutopilot components...");

            // If Instant Movement is selected, add the component
            if (ConfigFile.RecallMoveMethod == RecallMoveMethod.Instant)
            {
                __instance.gameObject.EnsureComponent<InstantNavigation>();
            }
            else
            {
                __instance.gameObject.EnsureComponent<SeaTruckNavMovement>();
            }

            SeaTruckAutoPilot newAutoPilot = __instance.gameObject.EnsureComponent<SeaTruckAutoPilot>();
            AllSeaTruckAutoPilots.AddInstance(newAutoPilot);

            ModDebugLog.LogDebug($"Added SeaTruckAutopilot components to {__instance.gameObject.name}!");
        }

        /// <summary>
        /// Patch the OnDestroy method, to remove
        /// the instance from the global list
        /// </summary>
        [HarmonyPatch(nameof(SeaTruckSegment.OnDestroy))]
        [HarmonyPostfix]
        internal static void OnDestroyPostfix(SeaTruckSegment __instance)
        {
            SeaTruckAutoPilot autoPilot = __instance.GetComponent<SeaTruckAutoPilot>();
            if (autoPilot)
            {
                AllSeaTruckAutoPilots.RemoveInstance(autoPilot);
            }
        }
    }
}