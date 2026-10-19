using DaftAppleGames.TameCreatures_BZ.TameCreatures;
using HarmonyLib;

namespace DaftAppleGames.TameCreatures_BZ.Patches
{
    /// <summary>
    /// Connects creature lifecycle events to the aggressive creature registry
    /// </summary>
    [HarmonyPatch(typeof(Creature))]
    internal static class CreaturePatches
    {
        /// <summary>
        /// Registers creatures after they start
        /// </summary>
        [HarmonyPatch(nameof(Creature.Start))]
        [HarmonyPostfix]
        private static void StartPostfix(Creature __instance)
        {
            AggressiveCreatures.Register(__instance);
        }

        /// <summary>
        /// Unregisters creatures before they are destroyed
        /// </summary>
        [HarmonyPatch(nameof(Creature.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroyPrefix(Creature __instance)
        {
            AggressiveCreatures.Unregister(__instance);
        }
    }
}
