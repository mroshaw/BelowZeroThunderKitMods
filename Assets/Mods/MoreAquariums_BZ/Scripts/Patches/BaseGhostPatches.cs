using HarmonyLib;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Patches for the BaseGhost class.
    /// </summary>
    [HarmonyPatch(typeof(BaseGhost))]
    internal class BaseGhostPatches
    {
        private static Base pendingBase;
        private static Int3 pendingCell;
        private static bool aquariumPlacementPending;

        /// <summary>
        /// Records the target base and cell before vanilla converts the custom ghost.
        /// </summary>
        [HarmonyPatch(nameof(BaseGhost.OnPlace))]
        [HarmonyPrefix]
        private static void OnPlace_Prefix(BaseGhost __instance)
        {
            BaseAquariumGhost aquariumGhost =
                __instance.GetComponent<BaseAquariumGhost>();
            if (!aquariumGhost)
            {
                return;
            }

            pendingBase = __instance.targetBase;
            pendingCell = __instance.targetOffset;
            aquariumPlacementPending = pendingBase;
            ModDebugLog.LogInfo(
                $"Base aquarium ghost placement starting. Ghost: {__instance.name}, " +
                $"target base: {GetBaseName(pendingBase)}, target cell: {pendingCell}, " +
                $"world position: {__instance.transform.position}.");
        }

        /// <summary>
        /// Matches a generated Observatory deconstructable to the pending custom placement.
        /// </summary>
        internal static bool TryConsumeAquariumPlacement(Base generatedBase,
            TechType recipe, Int3.Bounds bounds)
        {
            ModDebugLog.LogDebug(
                $"Generated base cell deconstructable. Recipe: {recipe}, " +
                $"base: {GetBaseName(generatedBase)}, bounds: {bounds}, " +
                $"aquarium pending: {aquariumPlacementPending}, " +
                $"pending base: {GetBaseName(pendingBase)}, pending cell: {pendingCell}.");

            if (!aquariumPlacementPending ||
                recipe != TechType.BaseObservatory ||
                generatedBase != pendingBase)
            {
                return false;
            }

            aquariumPlacementPending = false;
            pendingBase = null;
            ModDebugLog.LogInfo(
                $"Matched generated Observatory cell {bounds} to the pending " +
                $"Observatory Aquarium placement at {pendingCell}.");
            return true;
        }

        /// <summary>
        /// Returns a useful base identifier for construction diagnostics.
        /// </summary>
        private static string GetBaseName(Base targetBase)
        {
            return targetBase ?
                $"{targetBase.name} ({targetBase.GetInstanceID()})" :
                "none";
        }
    }
}
