using HarmonyLib;

namespace DaftAppleGames.VehicleEnhancements_BZ.Building
{
    [HarmonyPatch(typeof(Constructable), nameof(Constructable.CheckFlags))]
    internal static class ConstructablePatches
    {
        [HarmonyPostfix]
        private static void CheckFlagsPostfix(bool allowedInSub, ref bool __result)
        {
            if (!__result && allowedInSub &&
                SeaTruckBuildingContext.IsSubModuleFlagOverrideAllowed())
            {
                __result = true;
            }
        }
    }
}
