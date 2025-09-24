using HarmonyLib;
using DaftAppleGames.SeaTruckRecall_BZ.Navigation;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Patches
{
    [HarmonyPatch(typeof(Dockable))]
    internal class DockablePatches
    {
        [HarmonyPatch(nameof(Dockable.OnDockingStart))]
        [HarmonyPrefix]
        public static bool OnDockingStart_PreFix(Dockable __instance, bool disableCollisionInProcess, bool shouldDetachTail)
        {
            SeaTruckAutoPilot autoPilot = __instance.GetComponent<SeaTruckAutoPilot>();
            if (autoPilot)
            {
                ModDebugLog.LogDebug($"Docking Start for SeaTruckAutopilot {__instance.gameObject.name}!");
                autoPilot.BeginDocking();
            }
            return true;
        }
       
        [HarmonyPatch(nameof(Dockable.OnUndockingComplete))]
        [HarmonyPostfix]
        public static void OnUndockingComplete_PostFix(Dockable __instance, Player player)
        {
            SeaTruckAutoPilot autoPilot = __instance.GetComponent<SeaTruckAutoPilot>();
            if (autoPilot)
            {
                ModDebugLog.LogDebug($"Docking Complete for SeaTruckAutopilot {__instance.gameObject.name}!");
                autoPilot.DockingComplete();
            }
        }
    }
}