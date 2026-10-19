using HarmonyLib;
using static DaftAppleGames.TameCreatures_BZ.TameCreaturesPlugin;

namespace DaftAppleGames.TameCreatures_BZ.Patches
{
    /// <summary>
    /// Prevents tamed Ice Worms from selecting player or vehicle noise as a target
    /// </summary>
    [HarmonyPatch(typeof(IceWorm))]
    internal static class IceWormPatches
    {
        /// <summary>
        /// Rejects player and vehicle noise while preserving other target validation
        /// </summary>
        [HarmonyPatch(nameof(IceWorm.IsTargetValid))]
        [HarmonyPrefix]
        private static bool IsTargetValid_Prefix(NoiseSource target, ref bool __result)
        {
            if (ConfigFile.TameIceWorms && target != null &&
                (target.gameObject == Utils.GetLocalPlayer() || target.GetComponent<Hoverbike>() != null ||
                 target.GetComponent<Vehicle>() != null))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
