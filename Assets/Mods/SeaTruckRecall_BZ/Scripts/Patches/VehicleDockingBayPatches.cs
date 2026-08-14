using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Patches
{
    /// <summary>
    /// Hands control to vanilla docking as soon as a recalled SeaTruck is captured by the docking bay.
    /// </summary>
    [HarmonyPatch(typeof(VehicleDockingBay))]
    internal class VehicleDockingBayPatches
    {
        [HarmonyPatch(nameof(VehicleDockingBay.OnTriggerEnter))]
        [HarmonyPostfix]
        internal static void OnTriggerEnterPostfix(VehicleDockingBay __instance, Collider other)
        {
            Dockable capturedDockable = __instance.interpolatingDockable;
            if (!capturedDockable)
            {
                return;
            }

            SeaTruckAutoPilot autoPilot = capturedDockable.GetComponent<SeaTruckAutoPilot>();
            if (!autoPilot || !autoPilot.IsNavigating)
            {
                return;
            }

            ModDebugLog.LogDebug($"VehicleDockingBay captured recalled SeaTruck via collider " +
                                 $"'{other.name}'. Stopping navigation and handing control to vanilla docking.");
            autoPilot.BeginDocking();
        }
    }
}
