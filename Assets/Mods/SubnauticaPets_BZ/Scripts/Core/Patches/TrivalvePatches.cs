using HarmonyLib;
using DaftAppleGames.SubnauticaPets.Pets;
using UWE;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    /// <summary>
    ///     Patches for the pain that is the Trivalve.
    /// </summary>
    [HarmonyPatch(typeof(Trivalve))] internal class TrivalvePatches
    {
        /// <summary>
        ///     Prevents the Trivalve re-parenting out of the base
        /// </summary>
        [HarmonyPatch(nameof(Trivalve.followingPlayer), MethodType.Setter)]
        [HarmonyPrefix]
        public static bool FollowingPlayer_Prefix(Trivalve __instance, bool value)
        {
            if (!__instance.GetComponent<Pet>())
            {
                return true;
            }

            Log.LogDebug("In Trivalve.followingPlayer");
            __instance.creatureFollowPlayer.enabled = value;
            __instance._followingPlayer = value;
            __instance.largeWorldEntity.cellLevel = value
                ? LargeWorldEntity.CellLevel.Global
                : LargeWorldEntity.CellLevel.Medium;
            if (LargeWorldStreamer.main && LargeWorldStreamer.main.cellManager != null)
            {
                LargeWorldStreamer.main.cellManager.RegisterEntity(__instance.largeWorldEntity);
            }

            __instance.Subscribe(value);
            return false;
        }
    }
}
