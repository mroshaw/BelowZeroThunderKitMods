/*
using HarmonyLib;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Patch in the Player methods
    /// </summary>
    [HarmonyPatch(typeof(Player))] internal class PlayerPatches
    {
        [HarmonyPatch(nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Player __instance)
        {
            // Add the BuilderHelper
            __instance.gameObject.AddComponent<BuilderHelper>();
        }
    }
}
*/