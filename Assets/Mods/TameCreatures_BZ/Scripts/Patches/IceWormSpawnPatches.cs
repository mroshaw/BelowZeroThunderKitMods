using HarmonyLib;
using static DaftAppleGames.TameCreatures_BZ.TameCreaturesPlugin;

namespace DaftAppleGames.TameCreatures_BZ.Patches
{
    /// <summary>
    /// Prevents tamed Ice Worms from spawning in response to player or vehicle noise
    /// </summary>
    [HarmonyPatch(typeof(IceWormSpawn))]
    internal static class IceWormSpawnPatches
    {
        /// <summary>
        /// Rejects player and vehicle noise while preserving other target validation
        /// </summary>
        [HarmonyPatch(nameof(IceWormSpawn.IsValidTarget))]
        [HarmonyPrefix]
        private static bool IsValidTarget_Prefix(NoiseSource target, ref bool __result)
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
