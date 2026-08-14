using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using HarmonyLib;
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
            // Add the new AutoPilot components
            ModDebugLog.LogDebug("Adding SeaTruckAutopilot components...");
            __instance.gameObject.EnsureComponent<SeaTruckNavMovement>();
            SeaTruckAutoPilot newAutoPilot = __instance.gameObject.EnsureComponent<SeaTruckAutoPilot>();
            __instance.gameObject.EnsureComponent<SeaTruckAutoPilotAudio>();
            AllSeaTruckAutoPilots.AddInstance(newAutoPilot);

            ModDebugLog.LogDebug($"Added SeaTruckAutopilot components to {__instance.gameObject.name}!");
        }

        /// <summary>
        /// Prevents the player from entering any segment of a SeaTruck during an active recall.
        /// </summary>
        [HarmonyPatch(nameof(SeaTruckSegment.EnterHatch))]
        [HarmonyPrefix]
        internal static bool EnterHatchPrefix(SeaTruckSegment __instance)
        {
            SeaTruckSegment headSegment = SeaTruckSegment.GetHead(__instance);
            SeaTruckAutoPilot autoPilot = headSegment ? headSegment.GetComponent<SeaTruckAutoPilot>() : null;
            if (!autoPilot || !autoPilot.IsRecalling)
            {
                return true;
            }

            ModDebugLog.LogDebug("Blocked player from entering a SeaTruck while recall is active.");
            ErrorMessage.AddMessage("This SeaTruck is currently being recalled.");
            return false;
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
