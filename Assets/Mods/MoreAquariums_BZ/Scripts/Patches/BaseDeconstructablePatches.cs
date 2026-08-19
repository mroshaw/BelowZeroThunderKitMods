using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Patches for the BaseDeconstructable class.
    /// </summary>
    [HarmonyPatch(typeof(BaseDeconstructable))]
    internal class BaseDeconstructablePatches
    {
        /// <summary>
        /// Adds aquarium functionality after the base system creates the completed module.
        /// </summary>
        [HarmonyPatch(nameof(BaseDeconstructable.MakeCellDeconstructable))]
        [HarmonyPostfix]
        private static void MakeCellDeconstructable_Postfix(
            Transform geometry, Int3.Bounds bounds, TechType recipe,
            BaseDeconstructable __result)
        {
            if (!__result)
            {
                ModDebugLog.LogError(
                    $"MakeCellDeconstructable returned no component for recipe {recipe}, " +
                    $"bounds {bounds}, geometry {geometry}.");
                return;
            }

            Base generatedBase = __result.GetComponentInParent<Base>();
            bool newAquariumPlacement =
                BaseGhostPatches.TryConsumeAquariumPlacement(
                    generatedBase, recipe, bounds);
            bool persistedAquarium = recipe == TechType.BaseObservatory &&
                BaseAquariumPersistence.ContainsLocation(__result.transform.position);
            if (!newAquariumPlacement && !persistedAquarium)
            {
                return;
            }

            if (newAquariumPlacement)
            {
                BaseAquariumPersistence.AddLocation(__result.transform.position);
            }

            __result.recipe = ObservatoryAquariumPrefab.PrefabInfo.TechType;
            ModDebugLog.LogInfo(
                $"Applying Observatory Aquarium functionality to generated object " +
                $"'{__result.name}' at {__result.transform.position}. Tooltip recipe is now " +
                $"{__result.recipe}.");
            ObservatoryAquariumPrefab.ConfigureCompletedBasePiece(
                __result.gameObject);
        }

        /// <summary>
        /// Removes persisted aquarium identity when its generated base cell is deconstructed.
        /// </summary>
        [HarmonyPatch(nameof(BaseDeconstructable.Deconstruct))]
        [HarmonyPrefix]
        private static void Deconstruct_Prefix(BaseDeconstructable __instance)
        {
            if (__instance.recipe == ObservatoryAquariumPrefab.PrefabInfo.TechType)
            {
                BaseAquariumPersistence.RemoveLocation(__instance.transform.position);
            }
        }
    }
}
