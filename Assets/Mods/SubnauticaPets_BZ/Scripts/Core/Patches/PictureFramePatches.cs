using DaftAppleGames.SubnauticaPets.BaseParts;
using HarmonyLib;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(PictureFrame))]
    internal static class PictureFramePatches
    {
        [HarmonyPatch(nameof(PictureFrame.OnEnable))]
        [HarmonyPrefix]
        private static bool OnEnable_Prefix(PictureFrame __instance)
        {
            // If the component is on a PetConsole, then disable it
            var petConsole = __instance.GetComponentInChildren<PetConsole>();
            if (!petConsole) return true;

            Log.LogDebug("Disabling PictureFrame...");
            __instance.enabled = false;
            return false;
        }
    }
}