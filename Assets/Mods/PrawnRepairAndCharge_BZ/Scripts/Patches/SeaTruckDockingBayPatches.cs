using HarmonyLib;

namespace DaftAppleGames.PrawnSuitRepairAndCharge_BZ
{
    [HarmonyPatch(typeof(SeaTruckDockingBay), nameof(SeaTruckDockingBay.OnProtoDeserialize))]
    internal class SeaTruckDockingBayPatches
    {
        [HarmonyPrefix]
        private static void OnProtoDeserialize_Prefix()
        {
            ExosuitPatches.IsRestoringSeaTruckDock = true;
        }

        [HarmonyFinalizer]
        private static void OnProtoDeserialize_Finalizer()
        {
            ExosuitPatches.IsRestoringSeaTruckDock = false;
        }
    }
}