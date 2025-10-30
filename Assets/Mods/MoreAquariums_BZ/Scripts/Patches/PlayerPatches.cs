/*
using HarmonyLib;

namespace DaftAppleGames.MoreAquariums.Patches
{
    internal class PlayerPatches
    {
        /// <summary>
        /// Patch in the Player methods
        /// </summary>
        [HarmonyPatch(typeof(Player))]
        internal class PlayerPatch
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
}
*/