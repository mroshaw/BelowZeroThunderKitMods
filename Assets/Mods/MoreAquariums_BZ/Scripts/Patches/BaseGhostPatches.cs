using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Patches for the BaseGhost class.
    /// </summary>
    [HarmonyPatch(typeof(BaseGhost))]
    internal class BaseGhostPatches
    {
        private const float PlacementPositionToleranceSqr = 0.25f;

        private static Base pendingBase;
        private static Int3 pendingCell;
        private static Vector3 pendingPosition;
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
            pendingPosition = __instance.transform.position;
            aquariumPlacementPending = pendingBase;
            ModDebugLog.LogInfo(
                $"Base aquarium ghost placement starting. Ghost: {__instance.name}, " +
                $"target base: {GetBaseName(pendingBase)}, target cell: {pendingCell}, " +
                $"world position: {pendingPosition}.");
        }

        /// <summary>
        /// Matches a generated Observatory deconstructable to the pending custom placement.
        /// </summary>
        internal static bool TryConsumeAquariumPlacement(Base generatedBase,
            TechType recipe, Int3.Bounds bounds, Vector3 generatedPosition)
        {
            ModDebugLog.LogDebug(
                $"Generated base cell deconstructable. Recipe: {recipe}, " +
                $"base: {GetBaseName(generatedBase)}, bounds: {bounds}, " +
                $"world position: {generatedPosition}, " +
                $"aquarium pending: {aquariumPlacementPending}, " +
                $"pending base: {GetBaseName(pendingBase)}, pending cell: {pendingCell}, " +
                $"pending world position: {pendingPosition}.");

            if (!aquariumPlacementPending ||
                recipe != TechType.BaseObservatory ||
                generatedBase != pendingBase)
            {
                return false;
            }

            bool cellMatches = ContainsCell(bounds, pendingCell);
            bool positionMatches =
                (generatedPosition - pendingPosition).sqrMagnitude <=
                PlacementPositionToleranceSqr;
            if (!cellMatches && !positionMatches)
            {
                return false;
            }

            aquariumPlacementPending = false;
            pendingBase = null;
            ModDebugLog.LogInfo(
                $"Matched generated Observatory cell {bounds} to the pending " +
                $"Observatory Aquarium placement at {pendingCell} using " +
                $"{(cellMatches ? "cell" : "world position")} matching.");
            return true;
        }

        private static bool ContainsCell(Int3.Bounds bounds, Int3 cell)
        {
            return cell.x >= bounds.mins.x && cell.x <= bounds.maxs.x &&
                   cell.y >= bounds.mins.y && cell.y <= bounds.maxs.y &&
                   cell.z >= bounds.mins.z && cell.z <= bounds.maxs.z;
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
